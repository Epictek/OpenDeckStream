using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


public class ConfigModel
{
    public string VideoOutputPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ODS");
    public bool ReplayBufferEnabled { get; set; } = false;
    public int ReplayBufferSeconds { get; set; } = 60;
    public string Encoder { get; set; } = "ffmpeg_vaapi";
    public int ReplayBufferSize { get; set; } = 500;
    public float MicAudioLevel { get; set; } = 100;
    public float DesktopAudioLevel { get; set; } = 100;
    public bool MicrophoneEnabled { get; set; } = false;
    public string StreamingService { get; set; } = "twitch";
    public string StreamingKey { get; set; } = "";
    public int FPS { get; set; } = 30;

}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ConfigModel))]
internal partial class ConfigSourceGenerationContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(StatusModel))]
internal partial class StatusSourceGenerationContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(VolumePeakLevel))]
internal partial class VolumePeakLevelSourceGenerationContext : JsonSerializerContext
{
}





public class ConfigService
{
    private readonly string ConfigFilePath = Environment.GetEnvironmentVariable("DECKY_PLUGIN_SETTINGS_DIR") + "/config.json";
    private ILogger Logger { get; }

    public ConfigService(ILogger<ConfigService> logger)
    {
        Logger = logger;

        if (!File.Exists(ConfigFilePath))
        {
            var config = new ConfigModel();
            _= SaveConfig(config);
        }
    }

    public ConfigModel GetConfig()
    {
        Logger.LogInformation("Loading config");
        try {
            if (!File.Exists(ConfigFilePath))
            {
                Logger.LogWarning("Config file not found");
                return new ConfigModel();
            }

            var json = File.ReadAllText(ConfigFilePath);
            if (string.IsNullOrEmpty(json))
            {
                Logger.LogWarning("Config file is empty");
                return new ConfigModel();
            }
            Logger.LogInformation("Config file found");
            var deserialisedConfig = JsonSerializer.Deserialize(json, ConfigSourceGenerationContext.Default.ConfigModel);
            if (deserialisedConfig != null) return deserialisedConfig;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to deserialize config");
        }
        return new ConfigModel();
    }

    public async Task<ConfigModel> SaveConfig(ConfigModel config)
    {
        Logger.LogInformation("Saving config");
        var json = JsonSerializer.Serialize(config, ConfigSourceGenerationContext.Default.ConfigModel);
        await File.WriteAllTextAsync(ConfigFilePath, json);
        return config;
    }
}
