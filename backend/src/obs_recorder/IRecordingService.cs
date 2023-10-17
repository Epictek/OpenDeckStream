using System;
using System.Threading.Tasks;

public interface IRecordingService
{
    public EventHandler OnStatusChanged { get; set; }
    public EventHandler<VolumePeakChangedArg> OnVolumePeakChanged { get; set; }

    public void Init();
    public void StartRecording();
    public void StopRecording();
    public Task SaveReplayBuffer();
    (bool Running, bool Recording) GetStatus();
    void StartStreamOutput();
    void StopStreamOutput();
    void StartBufferOutput();
    void StopBufferOutput();
}

