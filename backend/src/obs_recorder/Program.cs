using System;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using obs_recorder;

// using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(Environment.GetEnvironmentVariable("DECKY_PLUGIN_LOG_DIR"), "obs-recorder.log"),
       rollingInterval: RollingInterval.Day,
       fileSizeLimitBytes: 10 * 1024 * 1024,
       retainedFileCountLimit: 2,
       rollOnFileSizeLimit: true,
       shared: true,
	   flushToDiskInterval: TimeSpan.FromSeconds(1)
	   	)
	.CreateLogger();

try {

var builder = WebApplication.CreateBuilder(args);
// builder.Logging.ClearProviders();
// builder.Logging.AddConsole();

#if DEBUG

builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy", x => x.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin()));

#else

builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy",x => x.AllowAnyMethod().AllowCredentials().AllowAnyHeader().WithOrigins("https://steamloopback.host")));
#endif


builder.Host.UseSerilog(); 
builder.Services.Configure<JsonOptions>(options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddSingleton<ObsRecordingService>();
builder.Services.AddSingleton(x => ActivatorUtilities.CreateInstance<ConfigService>(x, Environment.GetEnvironmentVariable("DECKY_PLUGIN_SETTINGS_DIR") + "/config.json"));
builder.Services.AddSignalR();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseWebSockets();


app.UseCors("CorsPolicy");

app.MapHub<SignalrHub>("/SignalrHub");


app.MapGet("/", () => "Hello World!");

var recorder = app.Services.GetRequiredService<ObsRecordingService>();
recorder.Init();


app.Run("http://0.0.0.0:9988");
} catch (Exception e) {

	Log.Error(e, "Exception:");
}
finally {
	Log.CloseAndFlush();
}