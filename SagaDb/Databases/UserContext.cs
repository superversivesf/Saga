using Microsoft.EntityFrameworkCore;
using SagaDb.Models;

namespace SagaDb.Databases;

public class UserContext : DbContext
{
    private readonly string _dbFile;

    public UserContext(string dbFile = "User.db")
    {
        _dbFile = dbFile;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<BookProgress> BookProgresses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=" + _dbFile);
    }
}