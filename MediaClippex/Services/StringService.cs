using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaClippex.Services;

public static partial class StringService
{
    public static string ConvertToTimeFormat(TimeSpan timeSpan) => $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    
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

        var match = MyRegex().Match(link);
        if (match.Success)
        {
            videoId = match.Groups[1].Value;
        }

        return videoId;
    }
    
    public static bool IsYouTubeVideoUrl(string url) => YoutubeUrlRegex().Match(url).Success;

    public static string FixFileName(string fileName)
    {
        return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, invalidChar) => current.Replace(invalidChar, '_'));
    }

    [GeneratedRegex("(?:youtu\\.be/|youtube\\.com/(?:embed/|v/|shorts/|watch\\?v=|watch\\?.+&v=))([^?&\"'>]+)")]
    private static partial Regex MyRegex();
    [GeneratedRegex("^(https?://)?(www\\.)?(youtube\\.com/|youtu\\.be/|youtube\\.com/shorts/)(watch\\?v=|v/|shorts/)?[a-zA-Z0-9_-]{11}$")]
    private static partial Regex YoutubeUrlRegex();
}