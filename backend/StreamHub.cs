using Microsoft.AspNetCore.SignalR;

namespace deckystream;

public class StreamHub : Hub<IStreamClient>
{
    private readonly GstreamerService _gstreamerService;
    private readonly GstreamerServiceShadow _gstreamerServiceShadow;

    public StreamHub(GstreamerService gstreamerService, GstreamerServiceShadow gstreamerServiceShadow)
    {
        _gstreamerService = gstreamerService;
        _gstreamerServiceShadow = gstreamerServiceShadow;
    }
    
    public async Task<bool> StartRecord()
    {
        return await _gstreamerService.Start();
    }
    public async Task<bool> StopRecord()
    {
        return _gstreamerService.Stop();
    }
    
    public async Task<bool> StartStream()
    {
        return await _gstreamerService.StartStream();
    }
    
    public async Task<bool> StopStream()
    {
        return _gstreamerService.Stop();
    }
    
    public async Task<bool> GetRecordingStatus()
    {
        return _gstreamerService.GetIsRecording();
    }
    
    public async Task<bool> GetStreamingStatus()
    {
        return _gstreamerService.GetIsStreaming();
    }
    
    public Task StartShadow()
    {
        _ = _gstreamerServiceShadow.StartPipeline();
        return Task.CompletedTask;
    }

    public Task StopShadow()
    {
        _ = _gstreamerServiceShadow.StopPipeline();
        return Task.CompletedTask;
    }

    public Task SaveShadow()
    {
        _ = _gstreamerServiceShadow.StartRecording();
        return Task.CompletedTask;
    }
    
    public async Task SetConfig(DeckyStreamConfig config)
    {
        await DeckyStreamConfig.SaveConfig(config);
    }
    
    public async Task<DeckyStreamConfig> GetConfig()
    {
        return await DeckyStreamConfig.LoadConfig();
    }

    public async Task<bool> ToggleMic(bool enabled)
    {
        if (enabled)
        {
             _gstreamerService.AddMic();
             return true;
        }

        return false;
    }

}

public enum GstreamerState
{
    Starting,
    StartedRecording,
    StartedStreaming,
    StoppedError,
    Stopped
}



public interface IStreamClient
{
    Task StreamingStatusChange(bool streaming);

    Task RecordingStatusChange(bool recording);

    Task GstreamerStateChange(GstreamerState state, string reason = "");

}