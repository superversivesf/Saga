using SagaDb.Models;
using Microsoft.EntityFrameworkCore;

namespace SagaDb.Database
{
    public class BookOrganizerContext : DbContext
    {
        readonly string _dbFile;

        public BookOrganizerContext(string dbFile = "BookOrganizer.db")
        {
            _dbFile = dbFile;
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<AudioFile> AudioFiles { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<BookAuthors> BookAuthors { get; set; }
        public DbSet<BookFile> BookFiles { get; set; }
        public DbSet<BookGenre> BookGenres { get; set; }
        public DbSet<BookSerie> BookSeries { get; set; }
        public DbSet<DbImage> Images { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=" + _dbFile);
        }
    }


}
