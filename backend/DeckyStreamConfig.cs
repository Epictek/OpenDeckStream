using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace deckystream;


public enum StreamType
{
    ndi,
    rtmp
}

public class DeckyStreamConfig
{
    const string CONFIG_PATH = "/home/deck/homebrew/settings/deckystream.json";

    public StreamType StreamingMode { get; set; }
    public string RtmpEndpoint { get; set; }

    public bool MicEnabled { get; set; }

    public static async Task<DeckyStreamConfig> LoadConfig()
    {
        if (File.Exists(CONFIG_PATH))
        {
            var cfg = await File.ReadAllTextAsync(CONFIG_PATH);
            return JsonSerializer.Deserialize<DeckyStreamConfig>(cfg);
        }
        else
        {
            var config = new DeckyStreamConfig()
            {
                StreamingMode = StreamType.ndi,
                MicEnabled = false
            };
            await SaveConfig(config);
            return config;
        }
    }
    
    public static async Task SaveConfig(DeckyStreamConfig config)
    {
        await File.WriteAllTextAsync(CONFIG_PATH, JsonSerializer.Serialize(config, new JsonSerializerOptions(){Converters = { new JsonStringEnumConverter()  }}));
    }
}