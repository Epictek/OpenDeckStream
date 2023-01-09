using System.Text.Json;

namespace deckystream;


public enum StreamType
{
    Ndi,
    Twitch
}

public class DeckyStreamConfig
{
    public StreamType StreamingMode = StreamType.Ndi;
    
    public static async Task<DeckyStreamConfig> LoadConfig()
    {
       var cfg = await File.ReadAllTextAsync("~/homebrew/settings/deckystream.json");
       return JsonSerializer.Deserialize<DeckyStreamConfig>(cfg);
    }
    
    public static async Task SaveConfig(DeckyStreamConfig config)
    {
        await File.WriteAllTextAsync("~/homebrew/settings/deckystream.json", JsonSerializer.Serialize(config));
    }
}