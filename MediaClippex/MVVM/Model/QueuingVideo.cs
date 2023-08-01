using System.ComponentModel.DataAnnotations;

namespace MediaClippex.MVVM.Model;

public class QueuingVideo
{

    [Key]
    public int QueuingVideoId { get; set; }
    public string Title { get; set; }
    public string Duration { get; set; }
    public string ThumbnailUrl { get; set; }
    public string FileType { get; set; }
    public string Url { get; set; }
    public double Progress { get; set; }
    public string ProgressInfo { get; set; }
    public string SelectedQuality { get; set; }
    public bool Paused { get; set; }
    public bool IsAudioOnly { get; set; }

    public QueuingVideo(string title, string duration, string thumbnailUrl, string fileType, string url, double progress,
        string progressInfo, string selectedQuality, bool paused, bool isAudioOnly = false)

    {
        Title = title;
        Duration = duration;
        ThumbnailUrl = thumbnailUrl;
        FileType = fileType;
        Url = url;
        Progress = progress;
        ProgressInfo = progressInfo;
        SelectedQuality = selectedQuality;
        Paused = paused;
        IsAudioOnly = isAudioOnly;
    }
}