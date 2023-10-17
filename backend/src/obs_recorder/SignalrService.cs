using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace obs_recorder;

public class SignalrHub : Hub<SignalrHubClient>, IDisposable
{

    private readonly IRecordingService RecordingService;
    private readonly ConfigService ConfigService;

    public SignalrHub(IRecordingService recordingService, ConfigService configService)
    {
        ConfigService = configService;
        RecordingService = recordingService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        Console.WriteLine("Client connected");

        RecordingService.OnStatusChanged += OnStatusChanged;

        RecordingService.OnVolumePeakChanged += OnVolumePeakChanged;

        var (Running, Recording) = RecordingService.GetStatus();
        _ = Clients.Caller.OnStatusChanged(Running, Recording);
    }

    void OnVolumePeakChanged(object sender, VolumePeakChangedArg e)
    {
        Console.WriteLine($"Peak changed: {e.Channel} {e.Peak}");
        _ = Clients.Caller.OnVolumePeakChanged(e.Channel, e.Peak);
    }

    void OnStatusChanged(object sender, EventArgs e)
    {
        var (Running, Recording) = RecordingService.GetStatus();
        _ = Clients.Caller.OnStatusChanged(Running, Recording);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public void StartRecording()
    {
        RecordingService.StartRecording();
    }

    public void StopRecording()
    {
        RecordingService.StopRecording();
    }

    public void StartBufferOutput()
    {
        RecordingService.StartBufferOutput();
    }

    public void StopBufferOutput()
    {
        RecordingService.StopBufferOutput();
    }

    public void SaveReplayBuffer()
    {
        _ = RecordingService.SaveReplayBuffer();
    }

    public void StartStreamOutput()
    {
        RecordingService.StartStreamOutput();
    }

    public void StopStreamOutput()
    {
        RecordingService.StopStreamOutput();
    }

    public Config GetConfig(){
        return ConfigService.GetConfig();
    }

    public Task<Config> SaveConfig(Config config){
        return ConfigService.SaveConfig(config);
    }

    public void Dispose()
    {
        RecordingService.OnStatusChanged -= OnStatusChanged;

        RecordingService.OnVolumePeakChanged -= OnVolumePeakChanged;
    }

}

public interface SignalrHubClient
{
    public Task OnStatusChanged(bool running, bool recording);

    public Task OnVolumePeakChanged(int channel, float peak);
}