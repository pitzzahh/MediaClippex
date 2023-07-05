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
        await Youtube.Videos.DownloadAsync( new[] { audioStreamInfo }, conversionRequestBuilder.Build(), progressHandler, cancellationToken);
    }

    public static async Task DownloadMuxed(IStreamInfo audioStreamInfo, IVideoStreamInfo videoStreamInfo,
        string path, Progress<double> progressHandler, CancellationToken cancellationToken = default)
    {
        var conversionRequestBuilder = new ConversionRequestBuilder($"{path}.{videoStreamInfo.Container}");
        conversionRequestBuilder.SetFFmpegPath("ffmpeg.exe");
        await Youtube.Videos.DownloadAsync(new[] { audioStreamInfo, videoStreamInfo }, conversionRequestBuilder.Build(), progressHandler, cancellationToken);
    }
}