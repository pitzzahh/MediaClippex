using MediaClippex.MVVM.Model;
using Microsoft.EntityFrameworkCore;

namespace MediaClippex.DB;

public class MediaClippexDataContext : DbContext
{
    // ReSharper disable once UnusedMember.Global
    public DbSet<Video> Videos { get; set; } = null!;

    // ReSharper disable once UnusedMember.Global
    public DbSet<QueuingContent> QueuingVideos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=mediaclippex.db"); // SQLite connection string
    }
}