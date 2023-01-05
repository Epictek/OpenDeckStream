

// const string UnixSocketPath = "/tmp/decky-stream.sock";

// if (File.Exists(UnixSocketPath))
// {
//     File.Delete(UnixSocketPath);
// }

using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenUnixSocket(UnixSocketPath);
// });

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddSingleton<GstreamerService>();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(x => x.AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(origin => true));


app.MapGet("/start", (GstreamerService gstreamerService) =>
{
    return gstreamerService.Start();
});

app.MapGet("/start-stream", (GstreamerService gstreamerService) =>
{
    return gstreamerService.StartStream();
});

app.MapGet("/stop", (GstreamerService gstreamerService) =>
{
    return gstreamerService.Stop;
});

app.MapGet("/isRecording", (GstreamerService gstreamerService) => gstreamerService.GetIsRecording());
app.MapGet("/isStreaming", (GstreamerService gstreamerService) => gstreamerService.GetIsStreaming());


app.MapGet("/list", () =>
{
    return Directory.GetFiles("/home/deck/Videos/DeckyStream", "*.mp4", SearchOption.AllDirectories)
        .OrderByDescending(d => new FileInfo(d).CreationTime)
        .Select((x) => x.Replace("/home/deck/Videos/DeckyStream", ""));
});

app.MapGet("/list-count", () => Directory.GetFiles("/home/deck/Videos/DeckyStream", "*.mp4", SearchOption.AllDirectories).Length);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
    "/home/deck/Videos/DeckyStream"),
    RequestPath = "/Videos"
});
 
app.Run("http://localhost:6969");