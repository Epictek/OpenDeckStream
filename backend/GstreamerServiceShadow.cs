using System;
using System.IO;
using System.IO.Pipelines;
using Gst;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using deckystream;
using GLib;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Application = Gst.Application;
using DateTime = Gst.DateTime;
using StreamType = Gst.StreamType;
using Task = System.Threading.Tasks.Task;
using Value = GLib.Value;

public class GstreamerServiceShadow: IDisposable 
{
    const string micSrcSink = @"alsa_input.pci-0000_04_00.5-platform-acp5x_mach.0.HiFi__hw_acp5x_0__source";
    const string audioSrcSink = @"alsa_output.pci-0000_04_00.5-platform-acp5x_mach.0.HiFi__hw_acp5x_1__sink.monitor";
    int buffer_count = 0;

    GLib.MainLoop _mainLoop;

    Gst.Pipeline? pipeline;

    readonly ILogger _logger;
    
    private readonly IHubContext<StreamHub, IStreamClient> _streamHubContext;
    private System.DateTime PipelineStartTime;
    public GstreamerServiceShadow(ILogger<GstreamerServiceShadow> logger, IHubContext<StreamHub, IStreamClient> streamHubContext)
    {
        _logger = logger;
        _streamHubContext = streamHubContext;
        try
        {
            Application.Init();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "error");
        }

        _mainLoop = new GLib.MainLoop();
        // _ndiMicLoop = new GLib.MainLoop();
        
        // _pipeline = new Pipeline();
    }

    public string GetDotDebug()
    {
     return Gst.Debug.BinToDotData(pipeline, DebugGraphDetails.All);
    }
    
    private Element muxer;
    private Element vrecq;
    private Element filesink;
    private Pad vrecq_src;
    private ulong vrecq_src_probe_id;

    public async Task StopPipeline()
    {
     _mainLoop.Quit();
     pipeline.SetState(State.Null);

     pipeline.Unref();
    }

    
    public async Task StartPipeline()
    {
     PipelineStartTime = System.DateTime.UtcNow;
     try
     {
      var config = await DeckyStreamConfig.LoadConfig();

      var videoSrc = "pipewiresrc do-timestamp=true ! vaapipostproc ! queue ! vaapih264enc ! h264parse ";
      //var videoSrc = "videotestsrc ! video/x-raw,width=1920,height=1080,format=I420 ! clockoverlay ! x264enc tune=zerolatency bitrate=8000";

      pipeline = (Gst.Parse.Launch($@"{videoSrc} ! queue name=vrecq ! mp4mux name=muxer ! filesink async=false name=filesink
pulsesrc device=""{audioSrcSink}"" ! audiorate ! audioconvert ! lamemp3enc target=bitrate bitrate=320 cbr=false ! muxer.audio_0") as Pipeline)!;

      var buffer = config.ReplayBuffer;
      //todo: workout sane values for the buffer
      if (buffer is < 5 or > 600)
      {
       _logger.LogError("Invalid buffer length: {buffer}", buffer);
       return;
      }

      vrecq = pipeline.GetByName("vrecq");
      vrecq.SetProperty("max-size-time", new Value(buffer * Constants.SECOND));
      vrecq.SetProperty("max-size-bytes", new Value(0));
      vrecq.SetProperty("max-size-buffers", new Value(0));

      //sets the queue to dispose of old buffer frames
      vrecq.SetProperty("leaky", new Value(2));

      vrecq_src = vrecq.GetStaticPad("src");
      vrecq_src_probe_id = vrecq_src.AddProbe(PadProbeType.Block | PadProbeType.Buffer, block_probe_cb);

      filesink = pipeline.GetByName("filesink");
      UpdateFileSinkLocation();


      muxer = pipeline.GetByName("muxer");

      pipeline.SetState(State.Playing);

      pipeline.Bus.AddSignalWatch();
      pipeline.Bus.EnableSyncMessageEmission();
      pipeline.Bus.Message += OnMessage;

      _mainLoop.Run();

      pipeline.SetState(State.Null);

      pipeline.Unref();
     }
     catch (Exception ex)
     {
      _logger.LogError(ex, "Error starting pipeline");
     }
    }
    
    
    
    void push_eos_thread ()
    {
     try
     {
      Pad peer = vrecq_src.Peer;
      _logger.LogInformation($"pushing EOS event on pad  ({peer.Name})");

      /* tell pipeline to forward EOS message from filesink immediately and not
       * hold it back until it also got an EOS message from the video sink */
      pipeline.MessageForward = true;
      peer.SendEvent(Event.NewEos());
      peer.Unref();
     }
     catch (Exception ex)
     {
      _logger.LogError(ex, "Error sending EOS");
     }
     isSaving = false;
    }

    
void UpdateFileSinkLocation()
{
 var outFile = Path.Join("/tmp" ,System.DateTime.Now.ToString("HH-mm-ss") + ".mp4");
 _logger.LogInformation("Changing file to location" + outFile);
 filesink.SetProperty("location", new Value(outFile));
}


PadProbeReturn probe_drop_one_cb(Pad pad, PadProbeInfo info)
{
 if (buffer_count == 0)
 {
  buffer_count++;
  _logger.LogInformation("Drop one buffer with ts " + info.Buffer.Dts);
  return PadProbeReturn.Drop;
 } else {
  bool is_keyframe;
  is_keyframe = !info.Buffer.Flags.HasFlag(BufferFlags.DeltaUnit);

  
  if (is_keyframe) {
   _logger.LogInformation("Letting buffer through and removing drop probe");
   return PadProbeReturn.Remove;
  } else {
   _logger.LogInformation ("Dropping buffer, wait for a keyframe.");
   return PadProbeReturn.Drop;
  }
  
 }
 }


private bool isSaving;
public async Task StartRecording()
{
 if (isSaving) return;
 isSaving = true;
 try
 {
  _logger.LogInformation("timeout, unblocking pad to start recording");

  /* need to hook up another probe to drop the initial old buffer stuck
   * in the blocking pad probe */
  vrecq_src.AddProbe(PadProbeType.Buffer, probe_drop_one_cb);


  /* now remove the blocking probe to unblock the pad */
  if (vrecq_src_probe_id != 0)
  {
   vrecq_src.RemoveProbe(vrecq_src_probe_id);
  }

  vrecq_src_probe_id = 0;

  await Task.Delay(5000);
  StopRecording();
 }
 catch (Exception ex)
 {
  _logger.LogError("StartRecording fail");
 }
}

PadProbeReturn block_probe_cb(Pad pad, PadProbeInfo info)
{
 return PadProbeReturn.Ok;
}



void StopRecording()
{
 try
 {
  _logger.LogInformation("stop recording");
  vrecq_src_probe_id = vrecq_src.AddProbe(PadProbeType.Block | PadProbeType.Buffer, block_probe_cb);
  Task.Run(push_eos_thread);
 } catch (Exception ex)
 {
  _logger.LogError("StopRecording fail");
 }

}


async void OnMessage(object e, MessageArgs args)
{
 try
 {
  _logger.LogInformation($"[{args.Message.Type}] {args.Message}");

  switch (args.Message.Type)
  {
   case MessageType.StateChanged:
    State oldstate, newstate, pendingstate;
    args.Message.ParseStateChanged(out oldstate, out newstate, out pendingstate);
    _logger.LogInformation($"[StateChange] From {oldstate} to {newstate} pending at {pendingstate}");
    Directory.CreateDirectory(Path.Join(DirectoryHelper.LOG_DIR, PipelineStartTime.ToString("yyyy-M-d-hh-mm-ss")));
    var file = File.CreateText(Path.Join(DirectoryHelper.LOG_DIR, PipelineStartTime.ToString("yyyy-M-d-hh-mm-ss"), $"{System.DateTime.UtcNow.ToString("HHms")}-{oldstate}-{newstate}-{pendingstate}")); 
    await file.WriteAsync(GetDotDebug());
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
    _logger.LogTrace($"[Warning] {warning}.");
    break;
   case MessageType.Element:

    if (args.Message.Structure.HasName("GstBinForwarded"))
    {

     _logger.LogInformation("Forwarded EOS from " + args.Message.Structure.Type + ":" + args.Message.Src.Name + ": " + args.Message.Src.GetType());

     filesink.SetState(State.Null);
     muxer.SetState(State.Null);

     UpdateFileSinkLocation();

     filesink.SetState(State.Playing);
     muxer.SetState(State.Playing);
     args.Message.Dispose();

    }

    break;
   // case MessageType.Eos:
   //  if (args.Message.Structure == null || !args.Message.Structure.HasName("GstBinForwarded"))
   //  {
   //   _logger.LogInformation("EOS from " + args.Message.Src.Name + ": " + args.Message.Src.GetType());
   //   // _logger.LogInformation("Stopping pipeline");
   //   // _mainLoop.Quit();
   //   //todo: how to tell forwarded EOS and real EOS apart?
   //  }
   //  break;
   case MessageType.StreamStatus:
    Element owner;
    StreamStatusType type;
    args.Message.ParseStreamStatus(out type, out owner);
    _logger.LogInformation($"[StreamStatus] Type {type} from {owner}");
    break;

   default:
    _logger.LogInformation($"[Recv] {args.Message.Type} {args.Message}");
    break;

  }
 }
 catch (Exception ex)
 {
  _logger.LogError(ex, "OnMessage fail");
 }
}


public void Dispose()
{
 pipeline?.Dispose();
 muxer.Dispose();
 vrecq.Dispose();
 filesink.Dispose();
 vrecq_src.Dispose();
}
}

