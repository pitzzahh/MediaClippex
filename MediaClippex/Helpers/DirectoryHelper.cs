using System.IO;
using MediaClippex.Services.Settings.Interfaces;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.Helpers;

[Service(Scope.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public class DirectoryHelper
{
    private readonly ISettings _settings;

    public DirectoryHelper(ISettings settings)
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
}