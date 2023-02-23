using Microsoft.AspNetCore.SignalR;

namespace deckystream;

public class StreamHub : Hub<IStreamClient>
{
    private readonly GstreamerService _gstreamerService;
    private readonly GstreamerServiceShadow _gstreamerServiceShadow;
    private readonly SettingsService _settingsService;

    public StreamHub(GstreamerService gstreamerService, GstreamerServiceShadow gstreamerServiceShadow, SettingsService settingsService)
    {
        _gstreamerService = gstreamerService;
        _gstreamerServiceShadow = gstreamerServiceShadow;
        _settingsService = settingsService;
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
    
    public Task SetConfig(DeckyStreamConfig config)
    {
        return _settingsService.Save(config);
    }
    
    public async Task<DeckyStreamConfig> GetConfig()
    {
        return _settingsService.Current;
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
    
    public async Task ResumeSuspend()
    {
        if (_settingsService.Current.ShadowEnabled)
        {
            await _gstreamerServiceShadow.StartPipeline();
        }
    }

    
    public async Task Suspend()
    {
        if (_settingsService.Current.ShadowEnabled)
        {
            await _gstreamerServiceShadow.StopPipeline();
        }
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