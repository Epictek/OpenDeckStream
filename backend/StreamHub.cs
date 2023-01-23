using Microsoft.AspNetCore.SignalR;

namespace deckystream;

public class StreamHub : Hub<IStreamClient>
{
    private readonly GstreamerService _gstreamerService;

    public StreamHub(GstreamerService gstreamerService)
    {
        _gstreamerService = gstreamerService;
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
    Task GstreamerStateChange(GstreamerState state, string reason = "");

}