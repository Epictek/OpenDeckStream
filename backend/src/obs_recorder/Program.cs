using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
	//	System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "logs\\obs-recorder.log"),
        Path.Combine(Environment.GetEnvironmentVariable("HOME"), "homebrew", "logs", "decky-obs", "obs-recorder.log"),
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

builder.Services.AddSingleton<IRecordingService, ObsRecordingService>();
var app = builder.Build();
app.UseSerilogRequestLogging();

app.UseCors("CorsPolicy");

app.MapGet("/", () => "Hello World!");

app.MapGet("/start", async (IRecordingService recorder) => {
	try {
	  recorder.StartRecording();
	} catch (Exception e) {
		return e.Message;
	}
	return "Started recording";
});

app.MapGet("/stop", async (IRecordingService recorder) => {
	try {
	 recorder.StopRecording();
	} catch (Exception e) {
		return e.Message;
	}
	return "Stopped recording";
});

app.MapGet("/saveBuffer", async (IRecordingService recorder) => {
	try {
		await recorder.SaveReplayBuffer();
	} catch (Exception e) {
		return e.Message;
	}
	return "Saved replay buffer";
});

var recorder = app.Services.GetRequiredService<IRecordingService>();
recorder.Init();


app.Run("http://0.0.0.0:9988");
} catch (Exception e) {

	Log.Error(e, "Exception:");
}
finally {
	Log.CloseAndFlush();
}