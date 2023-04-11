using System;
using System.IO;
using System.Linq;
using SagaDb.Database;

namespace SagaImporter;

internal class CleanProcessor
{
    private BookCommands _bookCommands;

    internal bool Initialize(CleanOptions c)
    {
        _bookCommands = new BookCommands(c.DatabaseFile);
        return true;
    }

    internal void Execute()
    {
        var _books = _bookCommands.GetBooks();
        var _removeBookCount = 0;
        var _removeAuthorCount = 0;
        var _removeSeriesCount = 0;

        foreach (var book in _books)
            if (!Directory.Exists(book.BookLocation))
            {
                var title = book.BookTitle;
                _bookCommands.RemoveBookToSeriesLinksByBook(book);
                _bookCommands.RemoveBookToAuthorLinksByBook(book);
                _bookCommands.RemoveBookToGenreLinksByBook(book);
                _bookCommands.RemoveBookToAudioLinksAndAudioFilesByBook(book);
                _bookCommands.RemoveBook(book);
                _removeBookCount++;
                Console.WriteLine($"Removed {title}: Directory Missing");
            }

        var _authors = _bookCommands.GetAuthors();

        foreach (var author in _authors)
        {
            var _bookCount = _bookCommands.GetBooksByAuthorId(author.AuthorId).ToList().Count();

            if (_bookCount == 0)
            {
                Console.WriteLine($"Orphaned Author: {author.AuthorName}");
                _bookCommands.RemoveAuthor(author);
                _removeAuthorCount++;
            }
        }

        var _series = _bookCommands.GetAllSeries();

        foreach (var series in _series)
        {
            var _bookCount = _bookCommands.GetSeriesBooks(series.SeriesId).ToList().Count();

            if (_bookCount == 0)
            {
                Console.WriteLine($"Orphaned Series: {series.SeriesName}");
                _bookCommands.RemoveSeries(series);
                _removeSeriesCount++;
            }
        }

        Console.WriteLine($"Removed {_removeBookCount} books from library");
        Console.WriteLine($"Removed {_removeAuthorCount} authors from library");
        Console.WriteLine($"Removed {_removeSeriesCount} series from library");
    }
}