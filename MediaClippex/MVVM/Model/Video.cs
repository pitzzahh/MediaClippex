using System.ComponentModel.DataAnnotations;

namespace MediaClippex.MVVM.Model;

public class Video
{
    public Video(string thumbnailUrl, string? title, string? duration, string? description, string? fileType,
        string? path)
    {
        ThumbnailUrl = thumbnailUrl;
        Title = title;
        Duration = duration;
        Description = description;
        FileType = fileType;
        Path = path;
    }

    [Key] public int VideoId { get; set; }

    public string ThumbnailUrl { get; set; }
    public string? Title { get; set; }
    public string? Duration { get; set; }
    public string? Description { get; set; }
    public string? FileType { get; set; }
    public string? Path { get; set; }
}