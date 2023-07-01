using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Playlists;
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

    public static async Task<Stream> GetVideoStream(string id, string quality)
    {
        return await Youtube.Videos.Streams.GetAsync(GetVideoOnlyStreamInfo(id, quality));
    }

    public static async Task<Stream> GetAudioStream(string id, string quality)
    {
        return await Youtube.Videos.Streams.GetAsync(GetAudioOnlyStream(id, quality));
    }

    public static async Task<IReadOnlyList<PlaylistVideo>> GetAllVideos(string url)
    {
        return await Youtube.Playlists.GetVideosAsync(url);
    }

    public static async Task<StreamManifest> GetManifest(string url)
    {
        return await Youtube.Videos.Streams.GetManifestAsync(url);
    }

    public static StreamManifest GetMuxedStream(string id)
    {
        return GetManifest(id).GetAwaiter().GetResult();
    }

    private static IVideoStreamInfo GetVideoOnlyStreamInfo(string id, string quality)
    {
        return GetManifest(id)
            .GetAwaiter()
            .GetResult()
            .GetVideoOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .First(s => s.VideoQuality.Label == quality);
    }

    private static IStreamInfo GetAudioOnlyStream(string id, string quality)
    {
        return GetManifest(id)
            .GetAwaiter()
            .GetResult()
            .GetAudioOnlyStreams()
            .First(s => s.Bitrate.ToString() == quality);
    }

    public static async Task DownloadAudioOnly(string path, string url, string quality,
        Progress<double> progressHandler)
    {
        var streamManifest = await GetManifest(url);
        // Select best audio stream (highest bitrate)
        var audioStreamInfo = streamManifest
            .GetAudioStreams()
            .First(s => s.Bitrate.ToString() == quality);
        var streamInfos = new[] { audioStreamInfo };
        var conversionRequestBuilder = new ConversionRequestBuilder($"{path}.mp3");
        conversionRequestBuilder
            .SetContainer("mp3")
            .SetFFmpegPath("ffmpeg.exe");
        await Youtube.Videos.DownloadAsync(streamInfos, conversionRequestBuilder.Build(), progressHandler);
    }

    public static async Task DownloadMuxed(string path, string url, string quality, Progress<double> progressHandler)
    {
        var streamManifest = await GetManifest(url);

        var audioStreamInfo = streamManifest
            .GetAudioStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate();

        var videoStreamInfo = streamManifest
            .GetVideoStreams()
            .Where(s => s.Container == Container.Mp4)
            .First(s => s.VideoQuality.Label == quality);

        var streamInfos = new[] { audioStreamInfo, videoStreamInfo };
        var conversionRequestBuilder = new ConversionRequestBuilder($"{path}.{videoStreamInfo.Container}");
        conversionRequestBuilder.SetFFmpegPath("ffmpeg.exe");
        await Youtube.Videos.DownloadAsync(streamInfos, conversionRequestBuilder.Build(), progressHandler);
    }
}