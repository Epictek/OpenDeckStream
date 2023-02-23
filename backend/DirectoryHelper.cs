namespace deckystream;

public static class DirectoryHelper
{
    public static string HOMEBREW_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../");
    public static string LOG_DIR = $"{HOMEBREW_DIR}/logs/deckystream";
    public static string SETTINGS_DIR = $"{HOMEBREW_DIR}/settings/deckystream/";
    public static string CLIPS_DIR = "/home/deck/Videos/DeckyStream";
    
    public static string REPLAY_CLIPS_DIR = CLIPS_DIR + "/ReplayClips";

    

    public static void CreateDirs()
    {
        Directory.CreateDirectory(CLIPS_DIR);
        Directory.CreateDirectory(REPLAY_CLIPS_DIR);
    }
}