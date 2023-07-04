using System;
using System.IO;

namespace MediaClippex.Helpers;

public static class DirectoryHelper
{
    private static readonly string MainSavingDirectory = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), AppDownloadsDirectory);
    private const string AppDownloadsDirectory = "MediaClippex";

    public static string GetVideoSavingDirectory()
    {
        
        CreateMainDirectoryIfNotPresent();
        
        var videoSavingDirectory = Path.Combine(MainSavingDirectory, "Videos");

        if (!Directory.Exists(videoSavingDirectory))
        {
            Directory.CreateDirectory(videoSavingDirectory);
        }

        return videoSavingDirectory;
    }

    public static string GetAudioSavingDirectory()
    {

        CreateMainDirectoryIfNotPresent();
        
        var audioSavingDirectory = Path.Combine(MainSavingDirectory, "Audios");
        
        if (!Directory.Exists(audioSavingDirectory))
        {
            Directory.CreateDirectory(audioSavingDirectory);
        }

        return audioSavingDirectory;
    }
    
    private static void CreateMainDirectoryIfNotPresent()
    {
        if (!Directory.Exists(MainSavingDirectory))
        {
            Directory.CreateDirectory(MainSavingDirectory);
        }
    }
}