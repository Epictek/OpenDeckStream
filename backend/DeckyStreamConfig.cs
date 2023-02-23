using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace deckystream;


public enum StreamType
{
    ndi,
    rtmp
}

public record DeckyStreamConfig()
{
    public StreamType StreamingMode { get; set; }
    public string? RtmpEndpoint { get; set; }
    public bool ShadowEnabled { get; set; }

    public bool MicEnabled { get; set; }
    public int ReplayBuffer { get; set; }
}

public class SettingsService
{
    ILogger<SettingsService> _logger;
    
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    internal async Task Initialise()
    {
        Directory.CreateDirectory(DirectoryHelper.SETTINGS_DIR);
        
        Current = await Load();
    }

    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new () { Converters = { new JsonStringEnumConverter() }, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    private DeckyStreamConfig DefaultConfig = new()
    {
        StreamingMode = StreamType.ndi,
        MicEnabled = false,
        RtmpEndpoint = null,
        ReplayBuffer = 30,
        ShadowEnabled = false
    };

    private static string CONFIG_PATH = $"{DirectoryHelper.SETTINGS_DIR}/deckystream.json";

    public EventHandler<DeckyStreamConfig> SettingChanged;
    public DeckyStreamConfig Current;
    public async Task<DeckyStreamConfig> Load()
    {
        if (!File.Exists(CONFIG_PATH)) return DefaultConfig;
        try
        {

            var cfg = await File.ReadAllTextAsync(CONFIG_PATH);
            var serialised = JsonSerializer.Deserialize<DeckyStreamConfig>(cfg, JsonSerializerOptions);
            if (serialised != null) return serialised;
            _logger.LogError("Error loading settings file, null object, using defaults");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings file, using defaults");
        }

        return DefaultConfig;
    }

    public Task Save(DeckyStreamConfig config)
    {
        return File.WriteAllTextAsync(CONFIG_PATH, JsonSerializer.Serialize(config, JsonSerializerOptions), Encoding.UTF8);
    }
}