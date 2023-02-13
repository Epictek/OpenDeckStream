namespace deckystream;

public static class DirectoryHelper
{
    public static string HOMEBREW_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../");
    public static string LOG_DIR = $"{HOMEBREW_DIR}/logs/deckystream";
    public static string SETTINGS_DIR = $"{HOMEBREW_DIR}/settings/";
    public static string CLIPS_DIR = "/home/deck/Videos/DeckyStream";

    public static void CreateDirs()
    {
        Directory.CreateDirectory(LOG_DIR);
        Directory.CreateDirectory(CLIPS_DIR);
    }
}