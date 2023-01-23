using System.Text.Json.Serialization;
using deckystream;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.FileProviders;
using Serilog;



Directory.CreateDirectory($"{DirectoryHelper.HOME_DIR}/logs");

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File($"{DirectoryHelper.HOME_DIR}/logs/deckystream.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

Log.Logger.Error(DirectoryHelper.HOME_DIR);

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(
    options => options.AddPolicy("CorsPolicy",
                        x => x.AllowAnyMethod().AllowCredentials().AllowAnyHeader().WithOrigins("https://steamloopback.host")));

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File($"{DirectoryHelper.HOME_DIR}/logs/deckystream.log", rollingInterval: RollingInterval.Day)
);

builder.Services.Configure<JsonOptions>(options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });


builder.Services.AddSignalR();

builder.Services.AddSingleton<GstreamerService>();

var app = builder.Build();
app.UseSerilogRequestLogging();

app.UseCors("CorsPolicy");



app.MapHub<StreamHub>("/streamhub");

app.MapGet("/start", (GstreamerService gstreamerService) => gstreamerService.Start());

app.MapGet("/start-stream", async (GstreamerService gstreamerService) => await gstreamerService.StartStream());

app.MapGet("/stop", (GstreamerService gstreamerService) => gstreamerService.Stop());

app.MapGet("/config", async () => await DeckyStreamConfig.LoadConfig());

app.MapPost("/config", async (HttpRequest ctx, ILogger<Program> logger) =>
{
    using var sr = new StreamReader(ctx.Body);
    var content = await sr.ReadToEndAsync();

    logger.LogInformation(content);
    var config = System.Text.Json.JsonSerializer.Deserialize<DeckyStreamConfig>(content);
    await DeckyStreamConfig.SaveConfig(config);
});

app.MapGet("/isRecording", (GstreamerService gstreamerService) => gstreamerService.GetIsRecording());
app.MapGet("/isStreaming", (GstreamerService gstreamerService) => gstreamerService.GetIsStreaming());

app.MapDelete("/delete/{*path}", (string path) =>
{
    Console.WriteLine(path);
    File.Delete($"/home/deck/Videos/DeckyStream/{path}");
});

app.MapGet("/debug/dot", (GstreamerService gstreamerService) => gstreamerService.GetDotDebug());

app.MapGet("/list", () =>
{
    return Directory.GetFiles("/home/deck/Videos/DeckyStream", "*.mp4", SearchOption.AllDirectories)
        .OrderByDescending(d => new FileInfo(d).CreationTime)
        .Select((x) => x.Replace("/home/deck/Videos/DeckyStream", ""));
});

app.MapGet("/list-count", () => Directory.GetFiles("/home/deck/Videos/DeckyStream", "*.mp4", SearchOption.AllDirectories).Length);


app.UseFileServer(new FileServerOptions()
{
    FileProvider = new PhysicalFileProvider(
        "/home/deck/Videos/DeckyStream"),
    RequestPath = "/Videos",
    EnableDirectoryBrowsing = true
});

app.Run("http://*:6969");