using System;
using System.Text.RegularExpressions;

namespace MediaClippex.Services;

public static class StringService
{
    public static string ConvertToTimeFormat(TimeSpan timeSpan)
    {
        return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }

    public static TimeSpan ConvertFromString(string? seconds)
    {
        if (!int.TryParse(seconds, out var totalSeconds))
        {
            throw new ArgumentException("Invalid seconds value");
        }

        return TimeSpan.FromSeconds(totalSeconds);
    }

    public static string ConvertBytesToFormattedString(long bytes)
    {
        const int byteConversion = 1024;
        double bytesDouble = bytes;

        if (bytesDouble >= Math.Pow(byteConversion, 4))
        {
            var terabytes = bytesDouble / Math.Pow(byteConversion, 4);
            return $"{terabytes:0.##} TB";
        }

        if (bytesDouble >= Math.Pow(byteConversion, 3))
        {
            var gigabytes = bytesDouble / Math.Pow(byteConversion, 3);
            return $"{gigabytes:0.##} GB";
        }

        if (bytesDouble >= Math.Pow(byteConversion, 2))
        {
            var megabytes = bytesDouble / Math.Pow(byteConversion, 2);
            return $"{megabytes:0.##} MB";
        }

        if (bytesDouble >= byteConversion)
        {
            var kilobytes = bytesDouble / byteConversion;
            return $"{kilobytes:0.##} KB";
        }

        return $"{bytesDouble} Bytes";
    }

    public static string ExtractVideoId(string link)
    {
        var videoId = "";

        var match = YoutubeUrlRegex().Match(link);
        if (match.Success) videoId = match.Groups[1].Value;

        return videoId;
    }

    public static bool IsYouTubeVideoUrl(string url)
    {
        return YoutubeUrlRegex().Match(url).Success;
    }

    public static bool IsYouTubePlaylistUrl(string url)
    {
        return YoutubePlaylistRegex().Match(url).Success;
    }


    private static Regex YoutubeUrlRegex()
    {
        return new Regex(
            "(?:youtu\\.be/|youtube\\.com/(?:embed/|v/|shorts/|watch\\?v=|watch\\?.+&v=))([^?&\"'>]+)(?:&list=([^?&\"'>]+))?");
    }

    private static Regex YoutubePlaylistRegex()
    {
        // This regular expression matches YouTube playlist URLs
        return new Regex("(?:youtu\\.be/|youtube\\.com/playlist\\?list=)([^?&\"'>]+)");
    }
}