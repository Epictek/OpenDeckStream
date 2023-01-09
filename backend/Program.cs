

// const string UnixSocketPath = "/tmp/decky-stream.sock";

// if (File.Exists(UnixSocketPath))
// {
//     File.Delete(UnixSocketPath);
// }

using System.Net.Http.Headers;
using System.Text.Json;
using deckystream;
using Microsoft.AspNetCore.Http.Extensions;
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


app.MapGet("/start", (GstreamerService gstreamerService) => gstreamerService.Start());

app.MapGet("/start-stream", async (GstreamerService gstreamerService) => await gstreamerService.StartStream());

app.MapGet("/stop", (GstreamerService gstreamerService) =>
{
     gstreamerService.Stop();
});

app.MapGet("/config", async () => await DeckyStreamConfig.LoadConfig());

app.MapPut("/config", async (ctx) =>
{
    var config = await ctx.Request.ReadFromJsonAsync<DeckyStreamConfig>();
    DeckyStreamConfig.SaveConfig(config);
});



app.MapGet("/isRecording", (GstreamerService gstreamerService) => gstreamerService.GetIsRecording());
app.MapGet("/isStreaming", (GstreamerService gstreamerService) => gstreamerService.GetIsStreaming());


app.MapDelete("/delete/{*path}", (string path) =>
{
    Console.WriteLine(path);
    File.Delete($"/home/deck/Videos/DeckyStream/{path}");
});


app.MapGet("/list", () =>
{
    return Directory.GetFiles("/home/deck/Videos/DeckyStream", "*.mp4", SearchOption.AllDirectories)
        .OrderByDescending(d => new FileInfo(d).CreationTime)
        .Select((x) => x.Replace("/home/deck/Videos/DeckyStream", ""));
});

app.MapGet("/list-count", () => Directory.GetFiles("/home/deck/Videos/DeckyStream", "*.mp4", SearchOption.AllDirectories).Length);


app.MapGet("/twitch-callback/", async (HttpRequest request, HttpClient client) =>
{
    Console.WriteLine(request.GetDisplayUrl());
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
    var url = await client.GetStringAsync("https://api.twitch.tv/helix/streams/key");
    Console.WriteLine();
});


app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
    "/home/deck/Videos/DeckyStream"),
    RequestPath = "/Videos"
});
 
app.Run("http://localhost:6969");