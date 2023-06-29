using System.Collections.Generic;
using System.Threading.Tasks;
using VideoLibrary;

namespace MediaClippex.Services;

public static class VideoService
{
    public static async Task<Video> GetVideo(string url)
    {
        return await YouTube.Default.GetVideoAsync(url);
    }
    
    public static async Task<IEnumerable<Video>> GetAllVideos(string url)
    {
        return await YouTube.Default.GetAllVideosAsync(url);
    }
    
}