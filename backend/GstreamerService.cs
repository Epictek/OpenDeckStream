using System;
using System.IO;
using Gst;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using deckystream;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StreamType = Gst.StreamType;
using Value = GLib.Value;

public class GstreamerService : IDisposable
{
    const string micSrcSink = @"alsa_input.pci-0000_04_00.5-platform-acp5x_mach.0.HiFi__hw_acp5x_0__source";
    const string audioSrcSink = @"alsa_output.pci-0000_04_00.5-platform-acp5x_mach.0.HiFi__hw_acp5x_1__sink.monitor";
    
    GLib.MainLoop MainLoop;
    Gst.Element Pipeline;
    readonly ILogger _logger;
    bool isRecording;
    bool isStreaming;
    
    
    public GstreamerService(ILogger<GstreamerService> logger)
    {
        _logger = logger;
        MainLoop = new GLib.MainLoop();

        try
        {
            Application.Init();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "error");
        }
    }

    public bool GetIsRecording()
    {
        return isRecording;
    }

    public bool GetIsStreaming()
    {
        return isStreaming ;
    }

    public bool Start()
    {
        if (isRecording || isStreaming) return false;
        isRecording = true;

        string videoDir = "/home/deck/Videos/DeckyStream/" + System.DateTime.Now.ToString("yyyy-M-dd");
        Directory.CreateDirectory(videoDir);

        var outFile = videoDir + "/" + System.DateTime.Now.ToString("HH-mm-ss") + ".mp4";
        _logger.LogInformation("Writing to: " + outFile);

        Pipeline = Parse.Launch(@$"pipewiresrc do-timestamp=true
    ! vaapipostproc
    ! queue
    ! vaapih264enc
    ! h264parse
    ! mp4mux name=sink
    ! filesink location=""{outFile}""
    audiomixer name=mix ! audioconvert lamemp3enc target=bitrate bitrate=128 cbr=true ! sink.audio_0
    pulsesrc device=""{audioSrcSink}"" ! mix.
    pulsesrc device=""{micSrcSink}"" ! mix.

    ");
            
        Pipeline.Bus.AddSignalWatch();
        Pipeline.Bus.EnableSyncMessageEmission();
        Pipeline.Bus.Message += OnMessage;
        var ret = Pipeline.SetState(State.Playing);

        if (ret == StateChangeReturn.Failure)
        {
            _logger.LogCritical("Unable to set the pipeline to the playing state.");
            isRecording = false;
            isStreaming = false;

            return false;
        }

        StartMainLoop();
        return true;
    }

    void StartMainLoop()
    {
        ThreadPool.QueueUserWorkItem(x => MainLoop.Run());

    }

    public async Task<bool> StartStream()
    {
        if (isRecording || isStreaming) return false;
        isStreaming = true;

        var config = await DeckyStreamConfig.LoadConfig();

        if (config.StreamingMode == deckystream.StreamType.ndi)
        {
            Pipeline = Parse.Launch(@$"pipewiresrc do-timestamp=true
        ! vaapipostproc
        ! queue 
        ! ndisinkcombiner name=combiner 
        ! ndisink ndi-name=""{Dns.GetHostName()}"" audiomixer name=mix 
         pulsesrc device=""{audioSrcSink}"" ! mix.
         pulsesrc device=""{micSrcSink}"" ! mix.
        ! combiner.audio");
        }
        else
        {
            if (!config.RtmpEndpoint.StartsWith("rtmp://")) return false;
            
            Pipeline = Parse.Launch(@$"pipewiresrc do-timestamp=true
    ! vaapipostproc
    ! queue
    ! vaapih264enc
    ! h264parse
    ! flvmux streamable=true name=sink 
    ! rtmpsink location=""{config.RtmpEndpoint}""
     audiomixer name=mix ! audioconvert lamemp3enc target=bitrate bitrate=128 cbr=true ! sink.audio_0
     pulsesrc device=""{audioSrcSink}"" ! mix.
     pulsesrc device=""{micSrcSink}"" ! mix.");
        }
        
        Pipeline.Bus.AddSignalWatch();
        Pipeline.Bus.EnableSyncMessageEmission();
        Pipeline.Bus.Message += OnMessage;
        var ret = Pipeline.SetState(State.Playing);

        if (ret == StateChangeReturn.Failure)
        {
            _logger.LogCritical("Unable to set the pipeline to the playing state.");
            isRecording = false;
            isStreaming = false;

            return false;
        }

        StartMainLoop();
        return true;
    }

    public bool Stop()
    {
        _logger.LogInformation("Stopping pipeline");
        if (!isRecording && !isStreaming) return true;
        
        if (Pipeline != null)
        {
            var evt = Pipeline.SendEvent(Event.NewEos());
            if (evt)
            {
                isRecording = false;
                isStreaming = false;
            }
            return evt;
        }
        
        return false;
    }

    public void Dispose()
    {
        Pipeline.SendEvent(Event.NewEos());
        Thread.Sleep(1000);
        
        Pipeline.Dispose();
    }


    void OnMessage(object e, MessageArgs args)
    {
        switch (args.Message.Type)
        {
            case MessageType.StateChanged:
                State oldstate, newstate, pendingstate;
                args.Message.ParseStateChanged(out oldstate, out newstate, out pendingstate);
                _logger.LogInformation($"[StateChange] From {oldstate} to {newstate} pending at {pendingstate}");
                break;
            case MessageType.StreamStatus:
                Element owner;
                StreamStatusType type;
                args.Message.ParseStreamStatus(out type, out owner);
                _logger.LogInformation($"[StreamStatus] Type {type} from {owner}");
                break;
            case MessageType.DurationChanged:
                long duration;
                Pipeline.QueryDuration(Format.Time, out duration);
                _logger.LogInformation($"[DurationChanged] New duration is {(duration / Gst.Constants.SECOND)} seconds");
                break;
            case MessageType.ResetTime:
                ulong runningtime = args.Message.ParseResetTime();
                _logger.LogInformation($"[ResetTime] Running time is {runningtime}");
                break;
            case MessageType.AsyncDone:
                ulong desiredrunningtime = args.Message.ParseAsyncDone();
                _logger.LogInformation($"[AsyncDone] Running time is {desiredrunningtime}");
                break;
            case MessageType.NewClock:
                Clock clock = args.Message.ParseNewClock();
                _logger.LogInformation($"[NewClock] {clock}");
                break;
            case MessageType.Buffering:
                int percent = args.Message.ParseBuffering();
                _logger.LogInformation($"[Buffering] {percent}% done");
                break;
            case MessageType.Tag:
                TagList list = args.Message.ParseTag();
                _logger.LogInformation($"[Tag] Information in scope {list.Scope} is {list.ToString()}");
                break;
            case MessageType.Error:
                GLib.GException gerror;
                string debug;
                args.Message.ParseError(out gerror, out debug);
                _logger.LogError($"[Error] {gerror.Message}, debug information {debug}.");
                MainLoop.Quit();
                break;
            case MessageType.Warning:
                IntPtr warningPtr;
                string warning;
                args.Message.ParseWarning(out warningPtr, out warning);
                _logger.LogTrace($"[Warning] {warning}.");
                break;
            case MessageType.Eos:
                _logger.LogInformation("[Eos] Playback has ended. Exiting!");
                isRecording = false;
                isStreaming = false;
                Pipeline.SetState(State.Null);
                Pipeline.Unref();
                MainLoop.Quit();
                break;
            default:
                _logger.LogInformation($"[Recv] {args.Message.Type} {args.Message}");
                break;
        }
    }

}