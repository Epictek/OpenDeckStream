using System.Text;
using Serilog.Sinks.File;

namespace deckystream.LogHooks;

internal class CaptureFilePathHook : FileLifecycleHooks
{
    public string? Path { get; private set; }

    public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
    {
        Path = path;
        return base.OnFileOpened(path, underlyingStream, encoding);
    }
}