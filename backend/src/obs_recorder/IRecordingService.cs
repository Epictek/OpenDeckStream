using System.Threading.Tasks;

internal interface IRecordingService
{
    public void Init();
    public void StartRecording();
    public void StopRecording();
    public Task SaveReplayBuffer();
}

