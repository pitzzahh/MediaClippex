using MediaClippex.MVVM.Model;
using Microsoft.EntityFrameworkCore;

namespace MediaClippex.DB;

public class MediaClippexDataContext : DbContext
{
    // ReSharper disable once UnusedMember.Global
    protected DbSet<Video> Videos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=mediaclippex.db"); // SQLite connection string
    }
}