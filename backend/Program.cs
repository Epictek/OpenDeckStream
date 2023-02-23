using System.IO.Compression;
using System.Text;
using System.Text.Json.Serialization;
using deckystream;
using deckystream.LogHooks;
using Microsoft.Extensions.FileProviders;
using Serilog;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

// DirectoryHelper.CreateDirs();


CaptureFilePathHook filePathHook = new CaptureFilePathHook();


Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    // .WriteTo.File($"{DirectoryHelper.LOG_DIR}/deckystream.log", rollingInterval: RollingInterval.Day, hooks: filePathHook)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

#if DEBUG

builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy",
        x => x.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin()));

#else

builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy",
                        x => x.AllowAnyMethod().AllowCredentials().AllowAnyHeader().WithOrigins("https://steamloopback.host")));
#endif
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    // .WriteTo.File($"{DirectoryHelper.LOG_DIR}/deckystream.log", rollingInterval: RollingInterval.Day, hooks: filePathHook)
);

builder.Services.Configure<JsonOptions>(options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });


builder.Services.AddSignalR();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<GstreamerService>();
builder.Services.AddSingleton<GstreamerServiceShadow>();

var app = builder.Build();
app.UseSerilogRequestLogging();

app.UseCors("CorsPolicy");

var settings = app.Services.GetRequiredService<SettingsService>();
await settings.Initialise();


app.MapHub<StreamHub>("/streamhub");

app.MapGet("/start", (GstreamerService gstreamerService) => gstreamerService.Start());

app.MapGet("/start-shadow", (GstreamerServiceShadow gstreamerService) =>
{
     _ = gstreamerService.StartPipeline();
     return "";
});

app.MapGet("/stop-shadow", (GstreamerServiceShadow gstreamerService) =>
{
    _ = gstreamerService.StopPipeline();
    return "";
});

app.MapGet("/save-shadow",  (GstreamerServiceShadow gstreamerService) =>
{
    _ = gstreamerService.StartRecording();
    return "";
});

app.MapGet("/start-stream", async (GstreamerService gstreamerService) => await gstreamerService.StartStream());

app.MapGet("/stop", (GstreamerService gstreamerService) => gstreamerService.Stop());


app.MapGet("/isRecording", (GstreamerService gstreamerService) => gstreamerService.GetIsRecording());
app.MapGet("/isStreaming", (GstreamerService gstreamerService) => gstreamerService.GetIsStreaming());

app.MapDelete("/delete/{*path}", (string path) =>
{
    Console.WriteLine(path);
    File.Delete($"/home/deck/Videos/DeckyStream/{path}");
});

app.MapGet("/debug/dot", (GstreamerService gstreamerService) => gstreamerService.GetDotDebug());

app.MapGet("/debug/shadow/dot", (GstreamerServiceShadow gstreamerService) => gstreamerService.GetDotDebug());

app.MapGet("/debug/zip", async (HttpResponse response, GstreamerService gstreamerService, ILogger<Program> logger) =>
{
    response.ContentType = "application/octet-stream";
    response.Headers.Add("Content-Disposition", "attachment; filename=\"DeckyStreamTroubleshoot.zip\"");

    string? dotText = null;
    using var archive = new ZipArchive(response.BodyWriter.AsStream(), ZipArchiveMode.Create);

    try
    {
        dotText = gstreamerService.GetDotDebug();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get dot graph");
    }

    if (!string.IsNullOrEmpty(dotText))
    {
        var entry = archive.CreateEntry("dot");
        await using var entryStream = entry.Open();
        await entryStream.WriteAsync(Encoding.UTF8.GetBytes(dotText));

    }

    if (!string.IsNullOrEmpty(filePathHook.Path))
    {
        var logFile = File.ReadAllBytes(filePathHook.Path);
        var logEntry = archive.CreateEntry(Path.GetFileName(filePathHook.Path));
        await using var logEntryStream = logEntry.Open();
        await logEntryStream.WriteAsync(logFile);
    }
    else
    {
        logger.LogError("No log file");
    }
});


app.MapGet("/list", () =>
{
    return Directory.GetFiles(DirectoryHelper.CLIPS_DIR, "*.mp4", SearchOption.AllDirectories)
        .OrderByDescending(d => new FileInfo(d).CreationTime)
        .Select((x) => x.Replace(DirectoryHelper.CLIPS_DIR, ""));
});

app.MapGet("/list-count", () => Directory.GetFiles(DirectoryHelper.CLIPS_DIR, "*.mp4", SearchOption.AllDirectories).Length);

app.Run("http://*:6969");