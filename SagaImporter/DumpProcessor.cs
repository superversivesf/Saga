using System;
using SagaDb.Database;

namespace SagaImporter;

internal class DumpProcessor
{
    private bool _authors;
    private BookCommands _bookCommands;
    private bool _books;
    private bool _duplicates;
    private bool _failedLookups;
    private bool _genres;
    private bool _series;
    private bool _stats;

    internal bool Initialize(DumpOptions d)
    {
        _bookCommands = new BookCommands(d.DatabaseFile);
        _authors = d.Authors;
        _books = d.Books;
        _failedLookups = d.FailedLookups;
        _genres = d.Genres;
        _series = d.Series;
        _stats = d.Stats;
        _duplicates = d.Duplicates;
        return true;
    }

    internal void Execute()
    {
        if (this._authors)
        {
            var _authors = _bookCommands.GetAuthors();
            Console.WriteLine("\n=== Authors ===");
            foreach (var a in _authors)
            {
                var _msg = a.GoodReadsAuthor ? " - GR" : "";
                _msg = $"- {a.AuthorType.ToString()}";
                Console.WriteLine($"{a.AuthorName}{_msg}");
            }
        }

        if (this._books)
        {
            var _books = _bookCommands.GetBooks();
            Console.WriteLine("\n=== Books ===");
            foreach (var b in _books)
            {
                var _gr = string.IsNullOrEmpty(b.LookupDescription) ? " - GR" : "";
                Console.WriteLine($"{b.BookTitle}{_gr}");
            }
        }

        if (_failedLookups)
        {
            var _books = _bookCommands.GetBooksFailedLookup();
            Console.WriteLine("\n=== Failed Lookups ===");
            foreach (var b in _books) Console.WriteLine($"{b.BookTitle}");
        }

        if (this._genres)
        {
            var _genres = _bookCommands.GetGenres();
            Console.WriteLine("\n=== Genres ===");
            foreach (var g in _genres) Console.WriteLine($"{g.GenreName}");
        }

        if (this._series)
        {
            var _series = _bookCommands.GetAllSeries();
            Console.WriteLine("\n=== Series ===");
            foreach (var s in _series) Console.WriteLine($"{s.SeriesName}");
        }

        if (_duplicates)
        {
            var _books = _bookCommands.GetBooks();
            var _books2 = _bookCommands.GetBooks();

            foreach (var book in _books)
            {
                var matchCount = 0;
                foreach (var book2 in _books2)
                    if (book.BookTitle == book2.BookTitle)
                        matchCount++;

                if (matchCount > 1) Console.WriteLine($"Duplicate Title: {book.BookTitle} -> {book.BookLocation}");
            }
        }

        if (_stats)
        {
            var _series = _bookCommands.GetAllSeries();
            var _genres = _bookCommands.GetGenres();
            var _missed = _bookCommands.GetBooksFailedLookup();
            var _books = _bookCommands.GetBooks();
            var _missingGoodReads = _bookCommands.GetBooksMissingLookup();
            Console.WriteLine("\n=== Stats ===");
            Console.WriteLine($"Books: {_books.Count}");
            Console.WriteLine($"Series: {_series.Count}");
            Console.WriteLine($"Genres: {_genres.Count}");
            Console.WriteLine($"Failed Lookup: {_missed.Count}");
            Console.WriteLine($"Miss Percentage: {(_missed.Count / (double)_books.Count * 100).ToString("F0")}%");
            Console.WriteLine($"Goodreads Lookup not done: {_missingGoodReads.Count}");
        }
    }
}