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
    public static readonly JsonSerializerOptions JsonSerializerOptions = new () { Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true };
    
    private static string CONFIG_PATH = $"{DirectoryHelper.SETTINGS_DIR}/deckystream.json";

    public StreamType StreamingMode { get; set; }
    public string RtmpEndpoint { get; set; }

    public bool MicEnabled { get; set; }
    public int ReplayBuffer { get; set; } = 30;

    public static async Task<DeckyStreamConfig> LoadConfig()
    {
        if (File.Exists(CONFIG_PATH))
        {
            var cfg = await File.ReadAllTextAsync(CONFIG_PATH);
            var serialised = JsonSerializer.Deserialize<DeckyStreamConfig>(cfg, JsonSerializerOptions);
            if (serialised == null)
            {
                return await CreateNewConfig();
            }
        }

        return await CreateNewConfig();
    }

    private static async Task<DeckyStreamConfig> CreateNewConfig()
    {
        await using (File.Create(CONFIG_PATH)){};
        var config = new DeckyStreamConfig()
        {
            StreamingMode = StreamType.ndi,
            MicEnabled = false,
            ReplayBuffer = 30
        };
        await SaveConfig(config);
        return config;
    }
    
    public static async Task SaveConfig(DeckyStreamConfig config)
    {
        await File.WriteAllTextAsync(CONFIG_PATH, JsonSerializer.Serialize(config, JsonSerializerOptions));
    }
}