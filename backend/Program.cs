using deckystream;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddSingleton<GstreamerService>();
builder.Services.AddCors();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors(x => x.AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(origin => true));


app.MapGet("/start", (GstreamerService gstreamerService) => gstreamerService.Start());

app.MapGet("/start-stream", async (GstreamerService gstreamerService) => await gstreamerService.StartStream());

app.MapGet("/stop", (GstreamerService gstreamerService) => gstreamerService.Stop());

app.MapGet("/config", async () => await DeckyStreamConfig.LoadConfig());

app.MapPut("/config", async (ctx) =>
{
    var config = await ctx.Request.ReadFromJsonAsync<DeckyStreamConfig>();
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