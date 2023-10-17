using System;
using System.Threading.Tasks;
using static obs_net.Obs;
using obs_net;
using Microsoft.Extensions.Logging;

using System.IO;
using System.Reflection;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Runtime.InteropServices;
using System.Linq;

public class ObsRecordingService : IRecordingService, IDisposable
{
    public void Dispose()
    {
        StopRecording();
    }

    IntPtr bufferOutput;
    IntPtr recordOutput;

    IntPtr videoEncoder;
    IntPtr audioEncoder;
    IntPtr streamOutput;
    public EventHandler OnStatusChanged { get; set; }
    public EventHandler<VolumePeakChangedArg> OnVolumePeakChanged { get; set; }

    bool initialised;
    bool Initialized
    {
        get
        {
            return initialised;
        }
        set
        {
            initialised = value;
            OnStatusChanged?.Invoke(this, null);
        }
    }

    bool recording;
    bool Recording
    {
        get
        {
            return recording;
        }
        set
        {
            recording = value;
            OnStatusChanged?.Invoke(this, null);
        }
    }


    readonly ILogger Logger;
    readonly ConfigService ConfigService;

    public ObsRecordingService(ILogger<ObsRecordingService> logger, ConfigService configService)
    {
        Logger = logger;
        ConfigService = configService;
    }

    public void Init()
    {
        //need to change directory so obs can find its plugins (this is a massive hack and I hate it but it works)
        Directory.SetCurrentDirectory(Path.Combine(System.AppContext.BaseDirectory, "obs"));
        Logger.LogError("Current directory: " + Directory.GetCurrentDirectory());

        if (obs_initialized())
        {
            throw new Exception("error: obs already initialized");
        }

        obs_set_nix_platform(obs_nix_platform_type.OBS_NIX_PLATFORM_X11_EGL);
        obs_set_nix_platform_display(UnixSysCalls.XOpenDisplay(IntPtr.Zero));


        // base_set_log_handler(new log_handler_t((lvl, msg, args, p) =>
        // {
        //      va_list.LinuxX64Callback(msg, args, Logger);
        // //    if (Logger is not null)
        // //     {
        // //         Logger.Log(LogErrorLvlToLogLvl((LogErrorLevel)lvl), logMsg);
        // //     }
        //  }), IntPtr.Zero);

        Logger.LogInformation("libobs version: " + obs_get_version_string());
        if (!obs_startup("en-US", null, IntPtr.Zero))
        {
            throw new Exception("error on libobs startup");
        }

        //var obsPath = "~/obs-portable/";
        var obsPath = "./";

        obs_add_data_path($"{obsPath}data/libobs/");
        obs_add_module_path($"{obsPath}obs-plugins/64bit/", $"{obsPath}data/obs-plugins/%module%/");
        obs_load_all_modules();
        obs_log_loaded_modules();

        obs_audio_info avi = new()
        {
            samples_per_sec = 44100,
            speakers = speaker_layout.SPEAKERS_STEREO
        };
        bool resetAudioCode = obs_reset_audio(ref avi);

        ResetVideo();

        obs_post_load_modules();
        Logger.LogInformation("Loaded modules");

        InitVideoOut();
        Initialized = true;

        var config = ConfigService.GetConfig();
        if (config.ReplayBufferEnabled && config.ReplayBufferSeconds > 0) StartBufferOutput();
    }

    private void ResetVideo()
    {
        // scene rendering resolution
        int MainWidth = 1280;
        int MainHeight = 800;

        int outputWidth = MainWidth;
        int outputHeight = MainHeight;

        obs_video_info ovi = new()
        {
            adapter = 0,
            graphics_module = "libobs-opengl",
            fps_num = 60,
            fps_den = 1,
            base_width = (uint)MainWidth,
            base_height = (uint)MainHeight,
            output_width = (uint)outputWidth,
            output_height = (uint)outputHeight,
            output_format = video_format.VIDEO_FORMAT_NV12,
            gpu_conversion = true,
            colorspace = video_colorspace.VIDEO_CS_DEFAULT,
            range = video_range_type.VIDEO_RANGE_DEFAULT,
            scale_type = obs_scale_type.OBS_SCALE_BILINEAR
        };

        int resetVideoCode = obs_reset_video(ref ovi);
        if (resetVideoCode != 0)
        {
            throw new Exception("error on libobs reset video: " + ((VideoResetError)resetVideoCode).ToString());
        }
    }

    public void StopRecording()
    {
        Logger.LogInformation("Stopping recording");
        //obs_output_stop(bufferOutput);
        obs_output_stop(recordOutput);

        //obs_output_release(bufferOutput);
        //obs_output_release(recordOutput);
        //todo release all the things

        Recording = false;
    }

    DateTime lastSampleTime = DateTime.MinValue;

    void OnAudioData(IntPtr param, IntPtr source, ref AudioData audioData, bool muted)
    {
        double elapsed = (DateTime.Now - lastSampleTime).TotalSeconds;
        Console.WriteLine($"Elapsed: {elapsed}");
        if (elapsed < 0.5) return;
        lastSampleTime = DateTime.Now;

        float rms = 0.0f;

        for (int plane = 0; plane < Obs.MAX_AV_PLANES && audioData.data[plane] != IntPtr.Zero; plane++)
        {
            float[] data = new float[audioData.frames];
            Marshal.Copy(audioData.data[plane], data, 0, (int)audioData.frames);

            float sum = data.Select(x => x * x).Sum();  // Square the sample to get the power

            rms += (float)Math.Sqrt(sum / data.Length);  // Root Mean Square (RMS) amplitude per plane
        }

        float db = 20.0f * (float)Math.Log10(rms);  // Convert amplitude to decibels (dB)

        // Map dB level to a percentage
        float minDb = -60.0f;
        float maxDb = 0.0f;
        float percentage = ((db - minDb) / (maxDb - minDb)) * 100.0f;

        percentage = Math.Min(Math.Max(percentage, 0.0f), 100.0f);

        Console.WriteLine($"Current audio level: {percentage}%");
        OnVolumePeakChanged?.Invoke(null, new VolumePeakChangedArg() { Peak = percentage, Channel = 0 });
    }


    public void InitVideoOut()
    {
        var config = ConfigService.GetConfig();

        IntPtr videoSource = obs_source_create("pipewire-gamescope-capture-source", "Gamescope Capture Source", IntPtr.Zero, IntPtr.Zero);

        obs_set_output_source(0, videoSource); //0 = VIDEO CHANNEL


        IntPtr videoEncoderSettings = obs_data_create();

        obs_data_set_int(videoEncoderSettings, "level", 40);
        obs_data_set_int(videoEncoderSettings, "bitrate", 3500);
        obs_data_set_int(videoEncoderSettings, "qp", 20);
        obs_data_set_int(videoEncoderSettings, "maxrate", 0);


        videoEncoder = obs_video_encoder_create("hevc_ffmpeg_vaapi", "FFMPEG VAAPI Encoder", videoEncoderSettings, IntPtr.Zero);
        //videoEncoder = obs_video_encoder_create("ffmpeg_vaapi", "FFMPEG VAAPI Encoder", videoEncoderSettings, IntPtr.Zero);
        // IntPtr videoEncoder = obs_video_encoder_create("obs_x264", "simple_h264_recording", videoEncoderSettings, IntPtr.Zero);

        obs_encoder_set_video(videoEncoder, obs_get_video());
        obs_data_release(videoEncoderSettings);

        // SETUP NEW AUDIO SOURCE
        IntPtr audioSource = obs_source_create("pulse_output_capture", "Audio Capture Source", IntPtr.Zero, IntPtr.Zero);
        obs_set_output_source(1, audioSource); //1 = AUDIO CHANNEL
                                               // SETUP NEW AUDIO ENCODER

        obs_source_add_audio_capture_callback(audioSource, OnAudioData, IntPtr.Zero);


        audioEncoder = obs_audio_encoder_create("ffmpeg_aac", "simple_aac_recording", IntPtr.Zero, (UIntPtr)0, IntPtr.Zero);
        obs_encoder_set_audio(audioEncoder, obs_get_audio());


        var videoDir = "/home/deck/Videos/DeckyStream/";
        Directory.CreateDirectory(videoDir);

        // SETUP NEW RECORD OUTPUT
        IntPtr recordOutputSettings = obs_data_create();
        obs_data_set_string(recordOutputSettings, "path", $"{videoDir}/Record-{DateTime.Now:u}.mp4");
        recordOutput = obs_output_create("ffmpeg_muxer", "simple_ffmpeg_output", recordOutputSettings, IntPtr.Zero);
        obs_data_release(recordOutputSettings);

        obs_output_set_video_encoder(recordOutput, videoEncoder);
        obs_output_set_audio_encoder(recordOutput, audioEncoder, (UIntPtr)0);

        var replayDir = "/home/deck/Videos/DeckyStream/Replays/";

        Directory.CreateDirectory(replayDir);

        IntPtr bufferOutputSettings = obs_data_create();
        obs_data_set_string(bufferOutputSettings, "directory", replayDir);
        obs_data_set_string(bufferOutputSettings, "format", "%CCYY-%MM-%DD %hh-%mm-%ss");
        obs_data_set_string(bufferOutputSettings, "extension", "mp4");
        obs_data_set_int(bufferOutputSettings, "duration_sec", 60);
        // obs_data_set_int(bufferOutputSettings, "max_time_sec", (uint)config.ReplayBufferSeconds);
        obs_data_set_int(bufferOutputSettings, "max_size_mb", 500);
        bufferOutput = obs_output_create("replay_buffer", "replay_buffer_output", bufferOutputSettings, IntPtr.Zero);
        obs_data_release(bufferOutputSettings);

        obs_output_set_video_encoder(bufferOutput, videoEncoder);
        obs_output_set_audio_encoder(bufferOutput, audioEncoder, (UIntPtr)0);
    }

    public void StartBufferOutput()
    {
        if (!Initialized)
        {
            Logger.LogWarning("Not initialized yet, skipping start buffer");
            return;
        }


        bool bufferOutputStartSuccess = obs_output_start(bufferOutput);
        Logger.LogInformation("buffer output successful start: " + bufferOutputStartSuccess);
        if (bufferOutputStartSuccess != true)
        {
            Logger.LogError("buffer output error: '" + obs_output_get_last_error(bufferOutput) + "'");
        }
    }


    public void StartStreamOutput()
    {
        if (!Initialized)
        {
            Logger.LogWarning("Not initialized yet, skipping start stream output");
            return;
        }

        // SETUP NEW twitch OUTPUT

        IntPtr rtmpOutputSettings = obs_data_create();
        obs_data_set_string(rtmpOutputSettings, "server", "rtmp://lhr08.contribute.live-video.net/app/");
        obs_data_set_string(rtmpOutputSettings, "key", "live_47002671_72BSNf0DzRm30WCSuXhHDWIaR5EQUw");
        obs_data_set_string(rtmpOutputSettings, "service", "Twitch");
        obs_data_set_bool(rtmpOutputSettings, "use_auth", false);


        streamOutput = obs_output_create("rtmp_output", "simple_rtmp_output", rtmpOutputSettings, IntPtr.Zero);
        obs_data_release(rtmpOutputSettings);

        obs_output_set_video_encoder(streamOutput, videoEncoder);

        obs_output_set_audio_encoder(streamOutput, audioEncoder, (UIntPtr)0);

        bool twitchOutputStartSuccess = obs_output_start(streamOutput);

        Logger.LogInformation("twitch output successful start: " + twitchOutputStartSuccess);
    }

    public void StopStreamOutput()
    {
        obs_output_stop(streamOutput);
        obs_output_release(streamOutput);
    }

    public void StartRecording()
    {
        if (!Initialized)
        {
            Logger.LogWarning("Not initialized yet, skipping start recording");
            return;
        }

        if (Recording)
        {
            Logger.LogWarning("Already recording, skipping start recording");
            return;
        }

        Recording = true;

        Logger.LogInformation("Starting recording");


        // START RECORD OUTPUT
        bool recordOutputStartSuccess = obs_output_start(recordOutput);
        Logger.LogInformation("record output successful start: " + recordOutputStartSuccess);
        if (recordOutputStartSuccess != true)
        {
            Logger.LogError("record output error: '" + obs_output_get_last_error(recordOutput) + "'");
        }

        return;
    }

    public Task SaveReplayBuffer()
    {
        calldata_t cd = new();
        var ph = obs_output_get_proc_handler(bufferOutput);
        Logger.LogInformation("buffer output successful save: " + proc_handler_call(ph, "save", cd));
        calldata_free(cd);
        return Task.CompletedTask;

    }

    public void StopBufferOutput()
    {
        obs_output_stop(bufferOutput);
    }

    public (bool Running, bool Recording) GetStatus()
    {
        return (Initialized, Recording);
    }


}

public class VolumePeakChangedArg : EventArgs
{
    public float Peak { get; set; }
    public int Channel { get; set; }
}