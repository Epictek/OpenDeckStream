using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public enum MicSetting {
    Disabled,
    Mixed,
    SeparateTrack
}

public class Config
{
    public bool ReplayBufferEnabled { get; set; }
    public int ReplayBufferSeconds { get; set; }
    public MicSetting RecordMic { get; set; }
}

public class ConfigService
{
    private readonly string _configFilePath;

    public ConfigService(string configFilePath)
    {
        _configFilePath = configFilePath;
    }


    public Config GetConfig()
    {
        if (!File.Exists(_configFilePath))
        {
            return default;
        }

        var json = File.ReadAllText(_configFilePath);
        return JsonSerializer.Deserialize<Config>(json);
    }

    public async Task<Config> SaveConfig(Config config)
    {
        var json = JsonSerializer.Serialize(config);
        await File.WriteAllTextAsync(_configFilePath, json);
        return config;
    }
}
