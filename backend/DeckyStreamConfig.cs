using System.IO;
using System.Text.Json;

namespace deckystream;


public enum StreamType
{
    ndi,
    rtmp
}

public class DeckyStreamConfig
{
    const string CONFIG_PATH = "/home/deck/homebrew/settings/deckystream.json";
    public StreamType StreamingMode = StreamType.ndi;
    public string RtmpEndpoint;

    public static async Task<DeckyStreamConfig> LoadConfig()
    {
        if (File.Exists(CONFIG_PATH))
        {
            var cfg = await File.ReadAllTextAsync(CONFIG_PATH);
            return JsonSerializer.Deserialize<DeckyStreamConfig>(cfg);
        }
        else
        {
            var config = new DeckyStreamConfig();
            await SaveConfig(config);
            return config;
        }
    }
    
    public static async Task SaveConfig(DeckyStreamConfig config)
    {
        await File.WriteAllTextAsync(CONFIG_PATH, JsonSerializer.Serialize(config));
    }
}