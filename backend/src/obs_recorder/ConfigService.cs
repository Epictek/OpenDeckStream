using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class Config
{
    public string VideoOutputPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "DeckyStream");
    public bool ReplayBufferEnabled { get; set; } = false;
    public int ReplayBufferSeconds { get; set; } = 60;
    public string Encoder { get; set; } = "ffmpeg_vaapi";
    public int ReplayBufferSize { get; set; } = 500;
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
