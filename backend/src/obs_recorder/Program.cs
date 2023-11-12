using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();


builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy", x => x.AllowAnyMethod().AllowCredentials().AllowAnyHeader().WithOrigins("https://steamloopback.host")));



builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, StatusSourceGenerationContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(1, ConfigSourceGenerationContext.Default);
});



// builder.Services.Configure<JsonOptions>(options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddSingleton<ObsRecordingService>();
builder.Services.AddSingleton<ConfigService>();

var app = builder.Build();

app.UseCors("CorsPolicy");

app.MapGet("/", () => "Hello World!");
app.MapGet("/api/StartRecording", (ObsRecordingService recorder) => recorder.StartRecording());
app.MapGet("/api/StopRecording", (ObsRecordingService recorder) => recorder.StopRecording());
app.MapGet("/api/StartStreaming", (ObsRecordingService recorder) => recorder.StartStreaming());
app.MapGet("/api/StopStreaming", (ObsRecordingService recorder) => recorder.StopStreaming());

app.MapGet("/api/GetStatus", (ObsRecordingService recorder) => recorder.GetStatus());
app.MapGet("/api/GetConfig", (ConfigService config) => config.GetConfig());
app.MapPost("/api/SaveConfig", (ConfigService config, ConfigModel newConfig) => config.SaveConfig(newConfig));
app.MapGet("/api/UpdateBufferSettings", (ObsRecordingService recorder) => recorder.UpdateBufferSettings());
app.MapGet("/api/SaveReplayBuffer", (ObsRecordingService recorder) => recorder.SaveReplayBuffer());

app.MapPost("/api/ToggleBuffer", async (bool enabled, ObsRecordingService recordingService, ConfigService configService, ILogger<Program> logger) =>
{
    try
    {
        var config = configService.GetConfig();

        if (config.ReplayBufferEnabled == enabled) return true;

        config.ReplayBufferEnabled = enabled;
        await configService.SaveConfig(config);

        if (config.ReplayBufferEnabled)
        {
            return recordingService.StartBufferOutput();
        }
        else
        {
            recordingService.StopBufferOutput();
            return true;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to toggle buffer");
        return false;
    }
});

app.MapGet("/api/ToggleMic", async (ObsRecordingService recordingService, ILogger<Program> logger) =>
{
    try
    {
        recordingService.ToggleMic();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to toggle mic");
    }
});


app.MapGet("/api/volume-event", async (ILogger<Program> logger, HttpContext context, ObsRecordingService recordingService) =>
{
    try
    {
        logger.LogInformation("volume event stream connected");

        var response = context.Response;
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("Connection", "keep-alive");

        var cts = new CancellationTokenSource();

        Action<VolumePeakLevel> volumeChangedAction = async (data) =>
        {
            if (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var serializedData = JsonSerializer.Serialize(data, VolumePeakLevelSourceGenerationContext.Default.VolumePeakLevel);
                    await response.WriteAsync("event: peak\n");
                    await response.WriteAsync($"data: {serializedData}\n\n");
                    await response.Body.FlushAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send status event");
                }
            }

        };

        recordingService.OnVolumePeakChanged += volumeChangedAction;

        context.RequestAborted.Register(() =>
        {
            logger.LogInformation("Request aborted");
            cts.Cancel();
            recordingService.OnVolumePeakChanged -= volumeChangedAction;

        });
        await Task.Delay(Timeout.Infinite, cts.Token);


    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send status event");
    }
});


app.MapGet("/api/status-event", async (ILogger<Program> logger, HttpContext context, ObsRecordingService recordingService, ConfigService configService) =>
{
    try
    {
        logger.LogInformation("Status event stream connected");

        var response = context.Response;
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("Connection", "keep-alive");

        var cts = new CancellationTokenSource();

        Action<StatusModel> statusChangedAction = async (data) =>
        {
            logger.LogInformation("Status changed");
            if (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var serializedData = JsonSerializer.Serialize(data, StatusSourceGenerationContext.Default.StatusModel);
                    logger.LogInformation("Sending status event {data}", serializedData);
                    await response.WriteAsync("event: status\n");
                    await response.WriteAsync($"data: {serializedData}\n\n");
                    await response.Body.FlushAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send status event");
                }
            }
        };


        configService.ConfigChanged += async (object o, System.EventArgs sender) =>
        {
            var serializedData = JsonSerializer.Serialize(configService.Config, ConfigSourceGenerationContext.Default.ConfigModel);
            await response.WriteAsync("event: config\n");
            await response.WriteAsync($"data: {serializedData}\n\n");
            await response.Body.FlushAsync();

        };
        recordingService.OnStatusChanged += statusChangedAction;

        context.RequestAborted.Register(() =>
        {
            logger.LogInformation("Request aborted");
            cts.Cancel();
            recordingService.OnStatusChanged -= statusChangedAction;
            configService.ConfigChanged -= async (object o, System.EventArgs sender) =>
        {
            var serializedData = JsonSerializer.Serialize(configService.Config, ConfigSourceGenerationContext.Default.ConfigModel);
            await response.WriteAsync("event: config\n");
            await response.WriteAsync($"data: {serializedData}\n\n");
            await response.Body.FlushAsync();

        };

        });
        await Task.Delay(Timeout.Infinite, cts.Token);


    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send status event");
    }
});



var recorder = app.Services.GetRequiredService<ObsRecordingService>();
recorder.Init();


app.Run("http://0.0.0.0:9988");
Console.WriteLine("I should never be reached");
Task.Delay(-1).Wait();