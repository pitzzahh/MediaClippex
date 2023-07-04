using MediaClippex.MVVM.Model;
using Microsoft.EntityFrameworkCore;

namespace MediaClippex.DB;

public class MediaClippexDataContext : DbContext
{
    public DbSet<Video> Videos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=mediaclippex.db"); // SQLite connection string
    }
}