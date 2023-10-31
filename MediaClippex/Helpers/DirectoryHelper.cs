using System;
using System.IO;

namespace MediaClippex.Helpers;

// TODO: refactor this, check the directories on first app launch rather than per download.
public static class DirectoryHelper
{
    private static readonly string MainSavingDirectory =
        Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            "MediaClippex");

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

    public static string GetPlaylistSavingDirectory(string playlistTitle = "")
    {
        CreateMainDirectoryIfNotPresent();

        var playlistSavingDirectory = Path.Combine(Path.Combine(MainSavingDirectory, "Playlists"), playlistTitle);

        if (!Directory.Exists(playlistSavingDirectory)) Directory.CreateDirectory(playlistSavingDirectory);

        return playlistSavingDirectory;
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