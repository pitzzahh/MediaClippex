using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace MediaClippex.Services;

public static class VideoService
{
    private static YoutubeClient Youtube { get; } = new();

    public static async Task<Video> GetVideo(string url)
    {
        return await Youtube.Videos.GetAsync(url);
    }

    public static async Task<StreamManifest> GetManifest(string url)
    {
        return await Youtube.Videos.Streams.GetManifestAsync(url);
    }

    public static IVideoStreamInfo GetVideoOnlyStreamInfo(StreamManifest manifest, string quality)
    {
        return manifest
            .GetVideoStreams()
            .First(s => s.VideoQuality.Label == quality);
    }

    public static IAudioStreamInfo GetAudioOnlyStream(StreamManifest manifest, string quality)
    {
        return manifest
            .GetAudioStreams()
            .First(s => s.Bitrate.ToString() == quality);
    }

    public static async Task DownloadAudioOnly(IAudioStreamInfo audioStreamInfo, string path,
        Progress<double> progressHandler, CancellationToken cancellationToken = default)
    {
        var conversionRequestBuilder = new ConversionRequestBuilder($"{path}.mp3");
        conversionRequestBuilder
            .SetContainer("mp3")
            .SetFFmpegPath("ffmpeg.exe");
        await Youtube.Videos.DownloadAsync(new[] { audioStreamInfo }, conversionRequestBuilder.Build(), progressHandler,
            cancellationToken);
    }

    public static async Task DownloadMuxed(IStreamInfo audioStreamInfo, IVideoStreamInfo videoStreamInfo,
        string path, Progress<double> progressHandler, CancellationToken cancellationToken = default)
    {
        var conversionRequestBuilder = new ConversionRequestBuilder($"{path}.{videoStreamInfo.Container}");
        conversionRequestBuilder.SetFFmpegPath("ffmpeg.exe");
        await Youtube.Videos.DownloadAsync(new[] { audioStreamInfo, videoStreamInfo }, conversionRequestBuilder.Build(),
            progressHandler, cancellationToken);
    }

    public static string GetVideoFileSizeFormatted(StreamManifest manifest, Video video, string selectedQuality)
    {
        if (!video.Duration.HasValue) return string.Empty;
        var duration = video.Duration.Value.TotalSeconds;
        var fileSize = (long)(GetAudioOnlyStream(manifest, selectedQuality).Bitrate.BitsPerSecond +  GetVideoOnlyStreamInfo(manifest, selectedQuality).Bitrate.BitsPerSecond * duration / 8);
        return StringService.ConvertBytesToFormattedString(fileSize);
    }

    public static string GetAudioFileSizeFormatted(StreamManifest manifest, Video video, string selectedQuality)
    {
        if (!video.Duration.HasValue) return string.Empty;
        var duration = video.Duration.Value.TotalSeconds;
        var fileSize = (long)(GetAudioOnlyStream(manifest, selectedQuality).Bitrate.BitsPerSecond * duration / 8);
        return StringService.ConvertBytesToFormattedString(fileSize);
    }
}