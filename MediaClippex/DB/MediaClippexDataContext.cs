using MediaClippex.MVVM.Model;
using Microsoft.EntityFrameworkCore;
using Russkyc.DependencyInjection.Attributes;

namespace MediaClippex.DB;

[Service]
public class MediaClippexDataContext : DbContext
{
    // ReSharper disable once UnusedMember.Global
    public DbSet<Video> Videos { get; set; } = null!;

    // ReSharper disable once UnusedMember.Global
    public DbSet<QueuingContent> QueuingVideos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=data.db"); // SQLite connection string
    }
}