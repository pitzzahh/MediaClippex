using System;
using System.IO;
using MediaClippex.Services.Settings.Interfaces;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.Services.Helpers;

[Service(Scope.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public class DirectoryService
{
    private readonly ISettings _settings;

    public DirectoryService(ISettings settings)
    {
        _settings = settings;
    }

    public string GetVideoSavingDirectory()
    {
        return CreateDirectoryIfNotPresent(Path.Combine(_settings.DownloadPath(), "Videos"));
    }

    public string GetPlaylistSavingDirectory(string playlistTitle = "")
    {
        return CreateDirectoryIfNotPresent(Path.Combine(_settings.DownloadPath(), "Playlists", playlistTitle));
    }

    public string GetAudioSavingDirectory()
    {
        return CreateDirectoryIfNotPresent(Path.Combine(_settings.DownloadPath(), "Audios"));
    }

    public static string CreateDirectoryIfNotPresent(string directory)
    {
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        return directory;
    }

    public static bool IsDirectoryWritable(string path)
    {
        var testFilePath = Path.Combine(path, $"{Guid.NewGuid()}");
        try
        {
            File.WriteAllText(testFilePath, "");
            File.Delete(testFilePath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static bool IsSubdirectory(string basePath, string path)
    {
        var baseDirectory = new DirectoryInfo(basePath);
        var directory = new DirectoryInfo(path);

        while (directory.Parent != null)
        {
            if (directory.Parent.FullName.Equals(baseDirectory.FullName, StringComparison.OrdinalIgnoreCase))
                return true;
            directory = directory.Parent;
        }

        return false;
    }
}