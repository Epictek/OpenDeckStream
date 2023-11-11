using System;
using System.Threading.Tasks;
using static obs_net.Obs;
using obs_net;
using Microsoft.Extensions.Logging;

using System.IO;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text.Json;
using System.Data;
using System.Collections.Generic;

public class ObsRecordingService : IDisposable
{
    readonly IntPtr NULL = IntPtr.Zero;

    public void Dispose()
    {
        StopRecording();

        obs_output_stop(bufferOutput);
        obs_output_release(bufferOutput);
        obs_shutdown();
    }

    IntPtr bufferOutput;
    IntPtr recordOutput;

    IntPtr videoEncoder;
    Dictionary<string, IntPtr> audioEncoders = new();
    IntPtr streamOutput;
    public Action<StatusModel> OnStatusChanged { get; set; }
    public Action<VolumePeakLevel> OnVolumePeakChanged { get; set; }

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
            OnStatusChanged?.Invoke(GetStatus());
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
            OnStatusChanged?.Invoke(GetStatus());
        }
    }

    bool bufferRunning;

    public bool BufferRunning
    {
        get
        {
            return bufferRunning;
        }
        set
        {
            bufferRunning = value;
            OnStatusChanged?.Invoke(GetStatus());
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

        IntPtr display = IntPtr.Zero;

        try
        {
            display = X11Interop.XOpenDisplay(IntPtr.Zero);
            while (display == IntPtr.Zero)
            {
                Logger.LogError("Failed to open display, retrying in 5 second");
                Task.Delay(5000).Wait();
                display = X11Interop.XOpenDisplay(IntPtr.Zero);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "failed to open display");
        }

        if (obs_initialized())
        {
            throw new Exception("error: obs already initialized");
        }

        obs_set_nix_platform(obs_nix_platform_type.OBS_NIX_PLATFORM_X11_EGL);
        obs_set_nix_platform_display(display);


        //this currently segfaults when built with AOT
        // base_set_log_handler(new log_handler_t((lvl, msg, args, p) =>
        // {
        //     try
        //     {
        //         va_list.LinuxX64Callback(msg, args, Logger);
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.LogError(ex, "Error in log handler: ");
        //     }
        // }), IntPtr.Zero);

        var obsModuleConfig = Path.Combine(Environment.GetEnvironmentVariable("DECKY_PLUGIN_SETTINGS_DIR") ?? "/home/deck/homebrew/settings/OpenDeckStream", "obs");
        Directory.CreateDirectory(obsModuleConfig);

        Logger.LogInformation("libobs version: " + obs_get_version_string());
        if (!obs_startup("en-US", obsModuleConfig, IntPtr.Zero))
        {
            throw new Exception("error on libobs startup");
        }

        obs_add_data_path($"data/libobs/");
        obs_add_module_path($"obs-plugins/64bit/", $"data/obs-plugins/%module%/");


        obs_audio_info avi = new()
        {
            samples_per_sec = 44100,
            speakers = speaker_layout.SPEAKERS_STEREO
        };
        bool resetAudioCode = obs_reset_audio(ref avi);

        ResetVideo();

        obs_load_all_modules();
        obs_log_loaded_modules();


        obs_post_load_modules();
        Logger.LogInformation("Loaded modules");

        InitVideoOut();

        Initialized = true;
        Logger.LogInformation("Initialized");
        var config = ConfigService.GetConfig();
        Logger.LogError("Config: " + JsonSerializer.Serialize(config, ConfigSourceGenerationContext.Default.ConfigModel));

        if (config.ReplayBufferEnabled && config.ReplayBufferSeconds > 0)
        {
            StartBufferOutput();
        }
    }


    IntPtr service = IntPtr.Zero;




    public void StartStreaming()
    {
        var config = ConfigService.GetConfig();
        string server = "";
        switch (config.StreamingService)
        {
            case "twitch":
                server = "rtmp://lhr08.contribute.live-video.net/app/";
                break;
            case "rtc":

                break;
            default:
                throw new Exception("Unknown streaming service: " + config.StreamingService);
        }

        IntPtr settings = obs_data_create();

        if (config.StreamingService == "whip")
        {
            service = obs_service_create("whip_custom", "whip_service", IntPtr.Zero, IntPtr.Zero);


            if (service == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create WHIP service.");
                return;
            }

            server = "https://b.siobud.com/api/whip";

            obs_data_set_string(settings, "service", "whip_custom");
            obs_data_set_string(settings, "bearer_token", config.StreamingKey);
            obs_data_set_string(settings, "server", server);

            obs_service_update(service, settings);
            obs_data_release(settings);

            streamOutput = obs_output_create("whip_output", "whip_output", IntPtr.Zero, IntPtr.Zero);
        }
        else
        {

            obs_data_set_string(settings, "server", server);
            obs_data_set_string(settings, "service", config.StreamingService);
            obs_data_set_string(settings, "key", config.StreamingKey);

            service = obs_service_create("rtmp_common", "rtmp_service", settings, IntPtr.Zero);
            obs_data_release(settings);

            if (service == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create rtmp service.");
                return;
            }

            streamOutput = obs_output_create("rtmp_output", "rtmp_output", IntPtr.Zero, IntPtr.Zero);
        }

        obs_output_set_video_encoder(streamOutput, videoEncoder);

        nuint idx = 0;
        foreach (var audioEncoder in audioEncoders)
        {
            obs_output_set_audio_encoder(streamOutput, audioEncoder.Value, idx);
            idx++;
        }
        obs_output_set_service(streamOutput, service);

        var success = obs_output_start(streamOutput);
        Logger.LogInformation("stream output successful start: " + success);
    }

    public void StartBeamOutput()
    {
        var beamOutput = obs_get_output_by_name("Beam Output");
        if (beamOutput == IntPtr.Zero)
        {
            Logger.LogError("Failed to get stream output");
            return;
        }
        IntPtr settings = obs_output_get_settings(beamOutput);
        if (settings == IntPtr.Zero)
        {
            Logger.LogError("Failed to get stream output settings");
            settings = obs_data_create();
        }

        obs_data_set_bool(settings, "enabled", true);
        obs_data_set_bool(settings, "connection_type_socket", true);
        obs_data_set_string(settings, "network_interface_list", "Any: 0.0.0.0");

        obs_output_update(beamOutput, settings);
        obs_data_release(settings);
        obs_output_start(beamOutput);
    }

    public void StopBeamOutput()
    {
        var beamOutput = obs_get_output_by_name("Beam Output");
        if (beamOutput == IntPtr.Zero)
        {
            Logger.LogError("Failed to get stream output");
            return;
        }
        IntPtr settings = obs_output_get_settings(beamOutput);
        if (settings == IntPtr.Zero)
        {
            Logger.LogError("Failed to get stream output settings");
            settings = obs_data_create();
        }

        obs_data_set_bool(settings, "enabled", false);
        obs_output_update(beamOutput, settings);
        obs_data_release(settings);

        obs_output_stop(beamOutput);
    }

    public void StopStreaming()
    {
        streamOutput = obs_get_output_by_name("Beam Output");
        if (streamOutput == IntPtr.Zero)
        {
            Logger.LogError("Failed to get stream output");
            return;
        }
        IntPtr settings = obs_output_get_settings(streamOutput);
        if (settings == IntPtr.Zero)
        {
            Logger.LogError("Failed to get stream output settings");
            settings = obs_data_create();
        }

        obs_data_set_bool(settings, "enabled", false);


        obs_output_stop(streamOutput);
        obs_output_release(streamOutput);
        obs_service_release(service);
    }

    private void ResetVideo()
    {
        int MainWidth = 1280;
        int MainHeight = 800;

        try
        {
            var sizes = X11Interop.GetSize();
            (MainWidth, MainHeight) = (sizes.width, sizes.height);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "failed to get size from X11 falling back to defaults");
        }

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
        obs_output_stop(recordOutput);

        obs_output_release(recordOutput);
        //todo release all the things

        Recording = false;
    }

    DateTime lastSampleTime = DateTime.MinValue;

    void OnAudioData(IntPtr param, IntPtr source, ref AudioData audioData, bool muted)
    {
        double elapsed = (DateTime.Now - lastSampleTime).TotalSeconds;
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

        OnVolumePeakChanged?.Invoke(new VolumePeakLevel() { Peak = percentage, Channel = 0 });
    }


    void InitVideoOut()
    {
        var config = ConfigService.GetConfig();

        IntPtr videoSource = obs_source_create("pipewire-gamescope-capture-source", "Gamescope Capture Source", IntPtr.Zero, IntPtr.Zero);

        obs_set_output_source(0, videoSource);

        IntPtr videoEncoderSettings = obs_data_create();

        var MonitorSize = X11Interop.GetSize(); 
        obs_data_set_int(videoEncoderSettings, "width", (uint)MonitorSize.width);
        obs_data_set_int(videoEncoderSettings, "height", (uint)MonitorSize.height);
        obs_data_set_int(videoEncoderSettings, "fps_num", (uint)config.FPS);


        if (config.Encoder == "obs_x264")
        {
            obs_data_set_int(videoEncoderSettings, "bitrate", 3500);

            obs_data_set_string(videoEncoderSettings, "preset", "ultrafast");
            obs_data_set_string(videoEncoderSettings, "profile", "main");
            obs_data_set_string(videoEncoderSettings, "tune", "zerolatency");
            obs_data_set_string(videoEncoderSettings, "x264opts", "");
        }
        else if (config.Encoder == "ffmpeg_vaapi")
        {
            obs_data_set_int(videoEncoderSettings, "level", 40);
            obs_data_set_int(videoEncoderSettings, "bitrate", 3500);
            obs_data_set_int(videoEncoderSettings, "qp", 20);
            obs_data_set_int(videoEncoderSettings, "maxrate", 0);
            obs_data_set_bool(videoEncoderSettings, "use_bufsize", true);
        }
        videoEncoder = obs_video_encoder_create(config.Encoder, config.Encoder + " Video Encoder", videoEncoderSettings, IntPtr.Zero);

        obs_encoder_set_video(videoEncoder, obs_get_video());
        obs_data_release(videoEncoderSettings);


        var combinedEncoder = obs_audio_encoder_create("ffmpeg_aac", "combined_encoder", IntPtr.Zero, 0, IntPtr.Zero);
        audioEncoders.Add("combined_encoder", combinedEncoder);
        obs_encoder_set_audio(combinedEncoder, obs_get_audio());

        var desktopAudio = obs_source_create("pulse_output_capture", "desktop_audio", IntPtr.Zero, IntPtr.Zero);
        obs_set_output_source(1, desktopAudio);
        obs_source_set_audio_mixers(desktopAudio, 1 | 2);  // Adjusted mixer logic for 2 channels
        obs_source_set_volume(desktopAudio, config.DesktopAudioLevel / (float)100);
        var desktopEncoder = obs_audio_encoder_create("ffmpeg_aac", "desktop_audio_encoder", IntPtr.Zero, (UIntPtr)1, IntPtr.Zero);
        audioEncoders.Add("desktop_audio_encoder", desktopEncoder);
        obs_encoder_set_audio(desktopEncoder, obs_get_audio());

        if (config.MicrophoneEnabled)
        {
            var micAudio = obs_source_create("pulse_input_capture", "mic_audio", IntPtr.Zero, IntPtr.Zero);
            obs_set_output_source(2, micAudio);  // Using index 2 for the second channel
            obs_source_set_audio_mixers(micAudio, 1 | 4);  // Adjusted mixer logic for 2 channels
            obs_source_set_volume(micAudio, config.MicAudioLevel / (float)100);

            var micEncoder = obs_audio_encoder_create("ffmpeg_aac", "mic_audio_encoder", IntPtr.Zero, (UIntPtr)2, IntPtr.Zero);
            audioEncoders.Add("mic_audio_encoder", micEncoder);
            obs_encoder_set_audio(micEncoder, obs_get_audio());
        }
    }

    void InitBufferOutput()
    {
        var config = ConfigService.GetConfig();

        var replayDir = Path.Combine(config.VideoOutputPath, "Replays");
        Directory.CreateDirectory(replayDir);

        IntPtr bufferOutputSettings = obs_data_create();
        obs_data_set_string(bufferOutputSettings, "directory", replayDir);
        obs_data_set_string(bufferOutputSettings, "format", "%CCYY-%MM-%DD %hh-%mm-%ss");
        obs_data_set_string(bufferOutputSettings, "extension", "mp4");
        //obs_data_set_int(bufferOutputSettings, "duration_sec", 60);
        obs_data_set_int(bufferOutputSettings, "max_time_sec", (uint)config.ReplayBufferSeconds);
        obs_data_set_int(bufferOutputSettings, "max_size_mb", (uint)config.ReplayBufferSize);
        bufferOutput = obs_output_create("replay_buffer", "replay_buffer_output", bufferOutputSettings, IntPtr.Zero);
        obs_data_release(bufferOutputSettings);

        obs_output_set_video_encoder(bufferOutput, videoEncoder);
        nuint idx = 0;
        foreach (var audioEncoder in audioEncoders)
        {
            obs_output_set_audio_encoder(bufferOutput, audioEncoder.Value, idx);
            idx++;
        }
    }

    public void UpdateBufferSettings()
    {
        var config = ConfigService.GetConfig();
        IntPtr bufferOutputSettings = obs_data_create();
        obs_data_set_int(bufferOutputSettings, "max_time_sec", (uint)config.ReplayBufferSeconds);
        obs_data_set_int(bufferOutputSettings, "max_size_mb", (uint)config.ReplayBufferSize);
        obs_output_update(bufferOutput, bufferOutputSettings);
        obs_data_release(bufferOutputSettings);
    }

    public void SetupNewRecordOutput()
    {
        var config = ConfigService.GetConfig();
        var videoDir = config.VideoOutputPath;
        Directory.CreateDirectory(videoDir);

        IntPtr recordOutputSettings = obs_data_create();

        obs_data_set_string(recordOutputSettings, "path", $"{videoDir}/Record-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.mp4");
        recordOutput = obs_output_create("ffmpeg_muxer", "simple_ffmpeg_output", recordOutputSettings, IntPtr.Zero);
        obs_data_release(recordOutputSettings);

        obs_output_set_video_encoder(recordOutput, videoEncoder);
        nuint idx = 0;
        foreach (var audioEncoder in audioEncoders)
        {
            obs_output_set_audio_encoder(recordOutput, audioEncoder.Value, idx);
            idx++;
        }
    }

    public bool StartBufferOutput()
    {
        Logger.LogInformation("Starting buffer output");
        InitBufferOutput();
        if (!Initialized)
        {
            Logger.LogWarning("Not initialized yet, skipping start buffer");
            return false;
        }

        bool bufferOutputStartSuccess = obs_output_start(bufferOutput);
        Logger.LogInformation("buffer output successful start: " + bufferOutputStartSuccess);
        if (!bufferOutputStartSuccess) Logger.LogError("buffer output error: '" + obs_output_get_last_error(bufferOutput) + "'");
        BufferRunning = bufferOutputStartSuccess;
        return bufferOutputStartSuccess;
    }


    bool startingRecording = false;

    public bool StartRecording()
    {
        if (startingRecording)
        {
            Logger.LogWarning("Already starting recording, skipping start recording");
            return false;
        }

        if (!Initialized)
        {
            Logger.LogWarning("Not initialized yet, skipping start recording");
            return false;
        }

        if (Recording)
        {
            Logger.LogWarning("Already recording, skipping start recording");
            return false;
        }

        startingRecording = true;

        SetupNewRecordOutput();

        Logger.LogInformation("Starting recording");

        // START RECORD OUTPUT
        bool recordOutputStartSuccess = obs_output_start(recordOutput);
        Logger.LogInformation("record output successful start: " + recordOutputStartSuccess);
        if (!recordOutputStartSuccess) Logger.LogError("record output error: '" + obs_output_get_last_error(recordOutput) + "'");
        Recording = recordOutputStartSuccess;
        startingRecording = false;

        return recordOutputStartSuccess;
    }

    public bool SaveReplayBuffer()
    {
        calldata_t cd = new();
        var ph = obs_output_get_proc_handler(bufferOutput);
        var successful = proc_handler_call(ph, "save", cd);
        Logger.LogInformation("buffer output successful save: {successful}", successful);
        calldata_free(cd);

        return successful;
    }

    public void StopBufferOutput()
    {
        BufferRunning = false;
        obs_output_stop(bufferOutput);
        obs_output_release(bufferOutput);
    }



    public StatusModel GetStatus()
    {
        return new StatusModel()
        {
            Running = Initialized,
            Recording = Recording,
            BufferRunning = BufferRunning
        };
    }


}


public class StatusModel
{
    public bool Running { get; set; }
    public bool Recording { get; set; }
    public bool BufferRunning { get; set; }
}

public class VolumePeakLevel
{
    public float Peak { get; set; }
    public int Channel { get; set; }
}