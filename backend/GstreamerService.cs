using System;
using System.IO;
using System.IO.Pipelines;
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
    
    GLib.MainLoop _mainLoop;
    Gst.Pipeline _pipeline;
    readonly ILogger _logger;
    bool isRecording;
    bool isStreaming;
    
    
    public GstreamerService(ILogger<GstreamerService> logger)
    {
        _logger = logger;
        _mainLoop = new GLib.MainLoop();

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

    public async Task<bool> Start()
    {
        var config = await DeckyStreamConfig.LoadConfig();
        if (isRecording || isStreaming) return false;
        isRecording = true;

        string videoDir = "/home/deck/Videos/DeckyStream/" + System.DateTime.Now.ToString("yyyy-M-dd");
        Directory.CreateDirectory(videoDir);

        var outFile = videoDir + "/" + System.DateTime.Now.ToString("HH-mm-ss") + ".mp4";
        _logger.LogInformation("Writing to: " + outFile);

    //     Pipeline = Parse.Launch(@$"pipewiresrc do-timestamp=true
    // ! vaapipostproc
    // ! queue
    // ! vaapih264enc
    // ! h264parse
    // ! mp4mux name=sink
    // ! filesink location=""{outFile}""
    // audiomixer name=mix ! audioconvert lamemp3enc target=bitrate bitrate=128 cbr=true ! sink.audio_0
    // pulsesrc device=""{audioSrcSink}"" ! mix.
    // {(config.MicEnabled ? @$"pulsesrc device=""{micSrcSink}"" ! mix." : string.Empty)}
    // ");
        _pipeline = new Pipeline();
        
        var sink = Gst.ElementFactory.Make("filesink");
        sink.SetProperty("location", new Value(outFile));
        var videosrc = Gst.ElementFactory.Make("pipewiresrc");
        videosrc.SetProperty("do-timestamp", new Value(true));
        
        
        var videopostproc = Gst.ElementFactory.Make("vaapipostproc");
        var queue1 = Gst.ElementFactory.Make("queue");

        var videoenc = Gst.ElementFactory.Make("vaapih264enc");
        var h264parse = Gst.ElementFactory.Make("h264parse");
        var mux = Gst.ElementFactory.Make("mp4mux", "mux");

        _pipeline.Add(videosrc, queue1, videopostproc, videoenc, h264parse, mux, sink);
        // Pipeline.Link(videosrc, videopostproc, queue1, videoenc, h264parse, mux, sink);
        _pipeline.Link(videosrc);
        videosrc.Link(videopostproc);
        videopostproc.Link(queue1);
        queue1.Link(videoenc);
        videoenc.Link(h264parse);
        h264parse.Link( mux);
        mux.Link(sink);

        var audioMixer = Gst.ElementFactory.Make("audiomixer", "audiomixer");
 
        var desktopAudio = Gst.ElementFactory.Make("pulsesrc", "audio1");
        desktopAudio.SetProperty("device", new Value(micSrcSink));
        
        var micAudio = Gst.ElementFactory.Make("pulsesrc", "audio2");
        micAudio.SetProperty("device", new Value(audioSrcSink));

        // var buzzer1 = Gst.ElementFactory.Make("audiotestsrc", "buzzer1");
        // buzzer1.SetProperty("freq", new Value(1000));
        // buzzer1.SetProperty("volume", new Value(0.5));
        //
        // var buzzer2 = Gst.ElementFactory.Make("audiotestsrc", "buzzer2");
        // buzzer2.SetProperty("wave", new Value("ticks"));
        // buzzer2.SetProperty("freq", new Value(10000));
        // buzzer2.SetProperty("apply-tick-ramp", new Value(true));
        // buzzer2.SetProperty("volume", new Value(1));
        // buzzer2.SetProperty("marker-tick-period", new Value(10));
        // buzzer2.SetProperty("sine-periods-per-tick", new Value(20));


        var audioconv = Gst.ElementFactory.Make("audioconvert");
        
        var audioEnc = ElementFactory.Make("lamemp3enc");
        audioEnc.SetProperty("target", new Value("bitrate"));
        audioEnc.SetProperty("bitrate", new Value("128"));
        audioEnc.SetProperty("cbr", new Value(true));

        _pipeline.Add(audioMixer);
        _pipeline.Add(desktopAudio);
        _pipeline.Add(micAudio);

        _pipeline.Add(audioEnc);
        _pipeline.Add(audioconv);
        
        desktopAudio.Link(audioMixer);

        // if (config.MicEnabled)
        {
            micAudio.Link(audioMixer);
        }

        audioMixer.Link(audioconv);

        audioconv.Link(audioEnc);

        audioEnc.Link(mux);
        
        _pipeline.Bus.AddSignalWatch();
        _pipeline.Bus.EnableSyncMessageEmission();
        _pipeline.Bus.Message += OnMessage;
        

        var ret = _pipeline.SetState(State.Playing);

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
        ThreadPool.QueueUserWorkItem(x => _mainLoop.Run());

    }

    public async Task<bool> StartStream()
    {
        if (isRecording || isStreaming) return false;
        isStreaming = true;

        var config = await DeckyStreamConfig.LoadConfig();

        if (config.StreamingMode == deckystream.StreamType.ndi)
        {
            _pipeline = Parse.Launch(@$"pipewiresrc do-timestamp=true
        ! vaapipostproc
        ! queue 
        ! ndisinkcombiner name=combiner 
        ! ndisink ndi-name=""{Dns.GetHostName()}"" audiomixer name=mix 
         pulsesrc device=""{audioSrcSink}"" ! mix.
        {(config.MicEnabled ? @$"pulsesrc device=""{micSrcSink}"" ! mix." : string.Empty)}
        ! combiner.audio") as Pipeline;
        }
        else
        {
            if (!config.RtmpEndpoint.StartsWith("rtmp://")) return false;
            
            _pipeline = Parse.Launch(@$"pipewiresrc do-timestamp=true
    ! vaapipostproc
    ! queue
    ! vaapih264enc
    ! h264parse
    ! flvmux streamable=true name=sink 
    ! rtmpsink location=""{config.RtmpEndpoint}""
     audiomixer name=mix ! audioconvert lamemp3enc target=bitrate bitrate=128 cbr=true ! sink.audio_0
     pulsesrc device=""{audioSrcSink}"" ! mix.
    {(config.MicEnabled ? @$"pulsesrc device=""{micSrcSink}"" ! mix." : string.Empty)}") as Pipeline;
        }
        
        _pipeline.Bus.AddSignalWatch();
        _pipeline.Bus.EnableSyncMessageEmission();
        _pipeline.Bus.Message += OnMessage;
        var ret = _pipeline.SetState(State.Playing);

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
        
        if (_pipeline != null)
        {
            var evt = _pipeline.SendEvent(Event.NewEos());
            if (evt)
            {
                isRecording = false;
                isStreaming = false;
            }
            return evt;
        }
        
        return false;
    }


    public string GetDotDebug()
    {
       return Gst.Debug.BinToDotData(_pipeline, DebugGraphDetails.All);

    }
    
    public void Dispose()
    {
        _pipeline.SendEvent(Event.NewEos());
        Thread.Sleep(1000);
        
        _pipeline.Dispose();
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
                _pipeline.QueryDuration(Format.Time, out duration);
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
                _mainLoop.Quit();
                break;
            case MessageType.Warning:
                IntPtr warningPtr;
                string warning;
                args.Message.ParseWarning(out warningPtr, out warning);
                _logger.LogInformation($"[Warning] {warning}.");
                break;
            case MessageType.Eos:
                _logger.LogInformation("[Eos] Playback has ended. Exiting!");
                isRecording = false;
                isStreaming = false;
                _pipeline.SetState(State.Null);
                _pipeline.Unref();
                _mainLoop.Quit();
                break;
            default:
                _logger.LogInformation($"[Recv] {args.Message.Type} {args.Message}");
                break;
        }
    }

}