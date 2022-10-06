using SagaDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaDb.Database
{
    public class BookCommands
    {
        private readonly string _dbFile;
        private bool _isEnsuredCreated;

        public BookCommands()
        {
            _isEnsuredCreated = false;
            //this._dbFile = @"D:\Testing\audiobook-test.db";
            this._dbFile = @"audiobook-test.db";
            using var db = new BookOrganizerContext(this._dbFile);
            db.Database.EnsureCreated();
            _isEnsuredCreated = true;
        }

        public BookCommands(string dbFile)
        {
            _isEnsuredCreated = false;
            this._dbFile = dbFile;
            using var db = new BookOrganizerContext(this._dbFile);
            db.Database.EnsureCreated();
            _isEnsuredCreated = true;
        }

        public BookOrganizerContext InitContext(BookOrganizerContext context)
        {
            if (context == null)
            {
                context = new BookOrganizerContext(this._dbFile);
                context.ChangeTracker.AutoDetectChangesEnabled = false;
                
                if (!_isEnsuredCreated)
                {
                    context.Database.EnsureCreated();
                    _isEnsuredCreated = true;
                }
            }
            return context;
        }

        #region Purge Commands
        public void PurgeAuthors(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            db.Authors.RemoveRange(db.Authors);
            db.SaveChanges();
        }
        public void PurgeAuthorLinks(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            db.BookAuthors.RemoveRange(db.BookAuthors);
            db.SaveChanges();
        }
        public void PurgeSeries(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            db.Series.RemoveRange(db.Series);
            db.SaveChanges();
        }
        public void PurgeSeriesLinks(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            db.BookSeries.RemoveRange(db.BookSeries);
            db.SaveChanges();
        }
        public void PurgeGenres(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            db.Genres.RemoveRange(db.Genres);
            db.SaveChanges();
        }

        public void PurgeImages(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            db.Images.RemoveRange(db.Images);
            db.SaveChanges();
        }

        public void PurgeGenreLinks(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            db.BookGenres.RemoveRange(db.BookGenres);
            db.SaveChanges();
        }
        #endregion

        #region Book/File/Author links

        public void RemoveBookToSeriesLinksByBook(Book _book, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _links = db.BookSeries.Where(bs => bs.BookId == _book.BookId).ToList();

            foreach (var link in _links)
            {
                db.BookSeries.Remove(link);
                db.SaveChanges();
            }
        }
        
        public void RemoveBookToGenreLinksByBook(Book _book, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _links = db.BookGenres.Where(ba => ba.BookId == _book.BookId).ToList();

            foreach (var link in _links)
            {
                db.BookGenres.Remove(link);
                db.SaveChanges();
            }
        }

        public void RemoveBookToAuthorLinksByBook(Book _book, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _links = db.BookAuthors.Where(ba => ba.BookId == _book.BookId).ToList();

            foreach (var link in _links)
            {
                db.BookAuthors.Remove(link);
                db.SaveChanges();
            }
        }

        public void RemoveBookToAudioLinksAndAudioFilesByBook(Book _book, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _links = db.BookFiles.Where(bf => bf.BookId == _book.BookId).ToList();

            foreach (var _link in _links)
            {
                var _audioFile = GetAudioFile(_link.FileId);
                RemoveAudioFile(_audioFile);
                db.BookFiles.Remove(_link);
                db.SaveChanges();
            }
        }


        public void LinkBookToSeries(Book _book, Series _series, string _bookVolume, BookOrganizerContext db = null)
        {
            db = InitContext(db);
            var _entry = db.BookSeries.FirstOrDefault(a => a.BookId == _book.BookId && a.SeriesId == _series.SeriesId && a.SeriesVolume == _bookVolume);

            if (_entry == null)
            {
                var _bookSerie = new BookSerie
                {
                    BookId = _book.BookId,
                    SeriesId = _series.SeriesId,
                    SeriesVolume = _bookVolume
                };
                db.BookSeries.Add(_bookSerie);
                db.SaveChanges();
            }
        }

        public void LinkBookToGenre(Book _book, Genre _genre, BookOrganizerContext db = null)
        {
            db = InitContext(db);
            var _entry = db.BookGenres.FirstOrDefault(a => a.BookId == _book.BookId && a.GenreId == _genre.GenreId);

            if (_entry == null)
            {
                var _bookGenre = new BookGenre
                {
                    BookId = _book.BookId,
                    GenreId = _genre.GenreId
                };
                db.BookGenres.Add(_bookGenre);
                db.SaveChanges();
            }
        }

        public List<BookSerie> GetSeriesBooks(string seriesId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _seriesBooks = db.BookSeries.Where(bs => bs.SeriesId == seriesId).ToList().OrderBy(bs => ConvertSeriesVolumeToDouble(bs.SeriesVolume)).ToList();

            return _seriesBooks;
        }

        private double ConvertSeriesVolumeToDouble(string volume)
        {
            try
            {
                var result = double.Parse(volume);
                return result;
            }
            catch
            {
                return 0.0;
            }
        }

        public List<AudioFile> GetAudioFilesByBookId(string bookId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _bookFiles = db.BookFiles.Where(bf => bf.BookId == bookId).Select(bf => bf.FileId).ToList();

            var _audioFileIdList = db.AudioFiles.Where(f => _bookFiles.Contains(f.AudioFileId)).OrderBy(af => af.AudioFileName).ToList();

            return _audioFileIdList;
        }


        public List<Genre> GetGenresByBookId(String bookId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _bookGenres = db.BookGenres.Where(g => g.BookId == bookId).Select(g => g.GenreId).ToList();

            return db.Genres.Where(g => _bookGenres.Contains(g.GenreId)).ToList();
        }

        public List<SeriesInfo> GetBookSeriesByBookId(string bookId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _bookSeries = db.BookSeries.Where(bs => bs.BookId == bookId).ToList();

            var _result = new List<SeriesInfo>();

            foreach (var bs in _bookSeries)
            {
                var _series = db.Series.Where(s => s.SeriesId == bs.SeriesId).FirstOrDefault();
                _result.Add(new SeriesInfo() { SeriesName = _series.SeriesName, SeriesVolume = bs.SeriesVolume, SeriesId = _series.SeriesId, SeriesLink = _series.SeriesLink });
            }

            return _result;

        }

        public void LinkBookToFile(Book _book, AudioFile _audioFile, BookOrganizerContext db = null)
        {
            db = InitContext(db);
            var _entry = db.BookFiles.FirstOrDefault(a => a.BookId == _book.BookId && a.FileId == _audioFile.AudioFileId);

            if (_entry == null)
            {
                var _bookFile = new BookFile
                {
                    BookId = _book.BookId,
                    FileId = _audioFile.AudioFileId
                };
                db.BookFiles.Add(_bookFile);
                db.SaveChanges();
            }
        }
        public void LinkAuthorToBook(Author _author, Book _book, AuthorType _type, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _entry = db.BookAuthors.FirstOrDefault(a => a.BookId == _book.BookId && a.AuthorId == _author.AuthorId);
            // No entry for author
            if (_entry == null)
            {
                _entry = new BookAuthors
                {
                    AuthorId = _author.AuthorId,
                    BookId = _book.BookId,
                    AuthorType = _type
                };
                db.BookAuthors.Add(_entry);
                db.SaveChanges();
            }
            else
            {
                // entry for author but type is unknown, happens on insert, so update to known
                if (_entry.AuthorType == AuthorType.Unknown)
                {
                    _entry.AuthorType = _type;
                    db.BookAuthors.Attach(_entry);
                    db.Entry(_entry).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    // Entry exists, but author is editor or something but also an author or something, So add both.  
                    _entry = new BookAuthors
                    {
                        AuthorId = _author.AuthorId,
                        BookId = _book.BookId,
                        AuthorType = _type
                    };
                    db.BookAuthors.Add(_entry);
                    db.SaveChanges();
                }
            }
        }

        public void LinkAuthorToBook(List<Author> authorEntries, Book _book, AuthorType _type, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            foreach (var _author in authorEntries)
            {
                var _entry = db.BookAuthors.FirstOrDefault(a => a.BookId == _book.BookId && a.AuthorId == _author.AuthorId);
                if (_entry == null)
                {
                    _entry = new BookAuthors
                    {
                        AuthorId = _author.AuthorId,
                        BookId = _book.BookId,
                        AuthorType = _type
                    };
                    db.BookAuthors.Add(_entry);
                    db.SaveChanges();
                }
            }
        }

        #endregion

        #region Books
        public void DumpBooks(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            foreach (var a in db.Books)
            {
                Console.WriteLine(a.BookId + " -> " + a.BookTitle);
            }
        }

        public List<BookSerie> GetSeriesListBySeriesId(string seriesId, BookOrganizerContext db = null)
        {
            db = InitContext(db);
            return db.BookSeries.Where(bs => bs.SeriesId == seriesId).ToList();
        }


        public List<Book> GetBooksByGenreId(string GenreId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _genreBooks = db.BookGenres.Where(bg => bg.GenreId == GenreId).ToList();

            var _books = new List<Book>();

            foreach (var gb in _genreBooks)
            {
                var _book = db.Books.Where(b => b.BookId == gb.BookId).FirstOrDefault();
                _books.Add(_book);
            }
            return _books;
        }

        public List<Book> GetBooksByAuthorId(string AuthorId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _authorBooks = db.BookAuthors.Where(ba => ba.AuthorId == AuthorId).ToList();

            var _books = new List<Book>();

            foreach (var ab in _authorBooks)
            {
                var _book = db.Books.Where(b => b.BookId == ab.BookId).FirstOrDefault();
                _books.Add(_book);
            }
            return _books;
        }

        public List<Author> GetAuthorsByBookId(string BookId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            var _authorBooks = db.BookAuthors.Where(ba => ba.BookId == BookId).ToList();

            var _authors = new List<Author>();

            foreach (var ab in _authorBooks)
            {
                var _author = db.Authors.Where(a => a.AuthorId == ab.AuthorId).FirstOrDefault();
                _authors.Add(_author);
            }
            return _authors;
        }
        public List<Book> GetBooksMissingGoodReadsAndFailedGoodReads(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Books.Where(b => b.GoodReadsFetchTried == false).OrderBy(b => b.BookTitle).ToList();
        }

        public List<Book> GetBooksMissingGoodReads(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Books.Where(b => b.GoodReadsFetchTried == false).OrderBy(b => b.BookTitle).ToList();
        }

        public List<Book> GetBooksFailedGoodReads(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Books.Where(b => b.GoodReadsFetchTried == true && String.IsNullOrEmpty(b.GoodReadsDescription)).OrderBy(b => b.BookTitle).ToList();
        }


        public List<Book> GetBooks(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Books.OrderBy(b => b.BookTitle).ToList();
        }

        public Book GetBook(string bookId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Books
                .FirstOrDefault(s => s.BookId == bookId);
        }

        public void InsertBook(Book book, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Books.Add(book);
            db.SaveChanges();
        }

        public void UpdateBook(Book book, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Books.Attach(book);
            db.Entry(book).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void RemoveBook(Book book, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Books.Remove(book);
            db.SaveChanges();
        }

        #endregion

        #region Authors
        public void DumpAuthors(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            foreach (var a in db.Authors)
            {
                Console.WriteLine(a.AuthorId + " -> " + a.AuthorName);
            }
        }

        public List<Author> GetAuthors(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            return db.Authors.OrderBy(a => a.AuthorName).ToList();
        }

        public List<Author> GetAuthorsWithGoodReads(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            return db.Authors.Where(a => !String.IsNullOrEmpty(a.GoodReadsAuthorLink)).OrderBy(a => a.AuthorName).ToList();
        }

        public List<Book> GetBookByGoodReadsLink(string link, BookOrganizerContext db = null)
        {
            db = InitContext(db);
            return db.Books.Where(b => b.GoodReadsLink == link).ToList();
        }


        public Author GetAuthor(string authorId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Authors
                .FirstOrDefault(s => s.AuthorId == authorId);
        }

        public void InsertAuthor(Author author, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Authors.Add(author);
            db.SaveChanges();
        }

        public void UpdateAuthor(Author author, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Authors.Attach(author);
            db.Entry(author).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void RemoveAuthor(Author author, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Authors.Remove(author);
            db.SaveChanges();
        }

        #endregion

        #region AudioFiles

        public AudioFile GetAudioFile(string audioFileId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.AudioFiles
                .FirstOrDefault(s => s.AudioFileId == audioFileId);
        }

        public void InsertAudioFile(AudioFile audioFile, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.AudioFiles.Add(audioFile);
            db.SaveChanges();
        }
        public void UpdateAudioFile(AudioFile audioFile, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.AudioFiles.Attach(audioFile);
            db.Entry(audioFile).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void RemoveAudioFile(AudioFile audioFile, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.AudioFiles.Remove(audioFile);
            db.SaveChanges();
        }

        public void DumpAudiofiles(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            foreach (var a in db.AudioFiles)
            {
                Console.WriteLine(a.AudioFileId + " -> " + a.AudioFileName);
            }
        }

        #endregion

        #region Genre

        public void DumpGenres(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            foreach (var a in db.Genres)
            {
                Console.WriteLine(a.GenreId + " -> " + a.GenreName);
            }
        }

        public List<Genre> GetGenres(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            return db.Genres.OrderBy(b => b.GenreName).ToList();
        }

        public Genre GetGenre(string genreId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Genres
                .FirstOrDefault(s => s.GenreId == genreId);
        }

        public void InsertGenre(Genre genre, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Genres.Add(genre);
            db.SaveChanges();
        }
        public void UpdateGenre(Genre genre, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Genres.Attach(genre);
            db.Entry(genre).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void RemoveGenre(Genre genre, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Genres.Remove(genre);
            db.SaveChanges();
        }
        #endregion

        #region Series

        public void DumpSeries(BookOrganizerContext db = null)
        {
            db = InitContext(db);

            foreach (var a in db.Series)
            {
                Console.WriteLine(a.SeriesId + " -> " + a.SeriesName);
            }
        }

        public List<Series> GetAllSeries(BookOrganizerContext db = null)
        {
            db = InitContext(db);
            return db.Series.OrderBy(s => s.SeriesName).ToList();
        }

        public Series GetSeries(string seriesId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Series
                .FirstOrDefault(s => s.SeriesId == seriesId);
        }

        public void InsertSeries(Series series, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Series.Add(series);
            db.SaveChanges();
        }
        public void UpdateSeries(Series series, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Series.Attach(series);
            db.Entry(series).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void RemoveSeries(Series series, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Series.Remove(series);
            db.SaveChanges();
        }
        #endregion

        #region Images
        public DbImage GetImage(string imageId, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            return db.Images
                .FirstOrDefault(i => i.ImageId == imageId);
        }

        public void InsertImage(DbImage image, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Images.Add(image);
            db.SaveChanges();
        }

        public void UpdateImage(DbImage image, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Images.Attach(image);
            db.Entry(image).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void RemoveImage(DbImage image, BookOrganizerContext db = null)
        {
            db = InitContext(db);

            db.Images.Remove(image);
            db.SaveChanges();
        }

        #endregion

        #region Search Functions
        public List<Book> SearchTitleByString(string phrase, BookOrganizerContext db = null)
        {
            db = InitContext(db);
            var _result = db.Books.Where(b => b.BookTitle.ToLower().Contains(phrase.ToLower())).ToList();
            return _result;
        }

        #endregion
    }
}
