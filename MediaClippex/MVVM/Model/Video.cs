using System.ComponentModel.DataAnnotations;

namespace MediaClippex.MVVM.Model;

public class Video
{
    [Key]
    public int VideoId { get; set; }
    
    public string ThumbnailUrl { get; set; }
    public string? Title { get; set; }
    public string? Duration { get; set; }
    public string? Description { get; set; }
    public string? FileType { get; set; }
    public string? FileSize { get; set; }
    public string? Path { get; set; }

    public Video(string thumbnailUrl, string? title, string? duration, string? description, string? fileType, string? fileSize, string? path)
    {
        ThumbnailUrl = thumbnailUrl;
        Title = title;
        Duration = duration;
        Description = description;
        FileType = fileType;
        FileSize = fileSize;
        Path = path;
    }
}