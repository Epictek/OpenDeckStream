using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public enum MicSetting {
    Disabled,
    Mixed,
    SeparateTrack
}

[JsonSerializable(typeof(Config))]
public class Config
{
    public bool ReplayBufferEnabled { get; set; } = true;
    public int ReplayBufferSeconds { get; set; } = 60;
    public MicSetting RecordMic { get; set; }
}

public class ConfigService
{
    private readonly string _configFilePath;
    private ILogger<ConfigService> Logger { get; }

    public ConfigService(string configFilePath, ILogger<ConfigService> logger)
    {
        Logger = logger;
        _configFilePath = configFilePath;

        if (!File.Exists(_configFilePath))
        {
            var config = new Config();
            _= SaveConfig(config);
        }
    }


    public Config GetConfig()
    {
        Logger.LogInformation("Loading config");
        if (!File.Exists(_configFilePath))
        {
            Logger.LogWarning("Config file not found");
            return new Config();
        }

        var json = File.ReadAllText(_configFilePath);
        return JsonSerializer.Deserialize<Config>(json);
    }

    public async Task<Config> SaveConfig(Config config)
    {
        Logger.LogInformation("Saving config");
        var json = JsonSerializer.Serialize(config);
        await File.WriteAllTextAsync(_configFilePath, json);
        return config;
    }
}