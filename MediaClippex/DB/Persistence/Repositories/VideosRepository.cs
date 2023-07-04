using MediaClippex.MVVM.Model;
using Microsoft.EntityFrameworkCore;

namespace MediaClippex.DB.Persistence.Repositories;

public class VideosRepository : Repository<Video>
{

    public VideosRepository(DbContext context) : base(context)
    {
    }

}