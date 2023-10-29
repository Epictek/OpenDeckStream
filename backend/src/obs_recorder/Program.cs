using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

#if DEBUG

builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy", x => x.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin()));

#else

builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy",x => x.AllowAnyMethod().AllowCredentials().AllowAnyHeader().WithOrigins("https://steamloopback.host")));
#endif


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

app.MapPost("/api/ToggleBufferOutput", (bool enabled, ObsRecordingService recordingService, ConfigService configService) =>
{
    var config = configService.GetConfig();

    if (config.ReplayBufferEnabled == enabled) return true;

    config.ReplayBufferEnabled = enabled;
    _ = configService.SaveConfig(config);

    if (config.ReplayBufferEnabled)
    {
        return recordingService.StartBufferOutput();
    }
    else
    {
        recordingService.StopBufferOutput();
        return true;
    }
});

app.MapGet("/StartStreaming", (ObsRecordingService recorder) => recorder.StartStreaming());
app.MapGet("/StopStreaming", (ObsRecordingService recorder) => recorder.StopStreaming());
app.MapGet("/UpdateBufferSettings", (ObsRecordingService recorder) => recorder.UpdateBufferSettings());
app.MapGet("/SaveBuffer", (ObsRecordingService recorder) => recorder.SaveReplayBuffer());

var recorder = app.Services.GetRequiredService<ObsRecordingService>();
recorder.Init();


app.Run("http://0.0.0.0:9988");
Console.WriteLine("I should never be reached");
Task.Delay(-1).Wait();