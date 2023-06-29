using System;
using System.Text.RegularExpressions;

namespace MediaClippex.Services;

public static partial class StringService
{
    public static string ConvertToTimeFormat(int? duration) => $"{duration / 3600:D2}:{duration % 3600 / 60:D2}:{duration % 60:D2}";
    
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

    [GeneratedRegex("(?:youtu\\.be/|youtube\\.com/(?:embed/|v/|shorts/|watch\\?v=|watch\\?.+&v=))([^?&\"'>]+)")]
    private static partial Regex MyRegex();
}