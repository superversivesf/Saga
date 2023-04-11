using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CsvHelper;
using Newtonsoft.Json;
using SagaDb.Database;
using SagaDb.Models;

namespace SagaImporter;

internal class OutputProcessor
{
    private BookCommands _bookCommands;
    private string _rootDir;
    private string _topLevelFolder;
    private bool _writeAuthors;
    private bool _writeBooks;
    private bool _writeDescriptions;
    private bool _writeFailBooks;
    private bool _writeGenres;
    private bool _writeReports;
    private bool _writeSeries;

    internal bool Initialize(OutputOptions o)
    {
        _rootDir = o.OutputFolder;
        _bookCommands = new BookCommands(o.DatabaseFile);
        _topLevelFolder = o.TopLevelOverride;
        _writeAuthors = o.WriteAuthors;
        _writeGenres = o.WriteGenre;
        _writeSeries = o.WriteSeries;
        _writeDescriptions = o.WriteDescriptions;
        _writeBooks = o.WriteBooks;
        _writeFailBooks = o.WriteFailBooks;
        _writeReports = o.WriteReports;

        if (!string.IsNullOrEmpty(_topLevelFolder))
        {
            var _opCount = (_writeAuthors ? 1 : 0) + (_writeGenres ? 1 : 0) + (_writeSeries ? 1 : 0);
            if (_opCount > 1)
                throw new Exception(
                    "If Top level directory override specified then only one write operation may be chosen");
        }

        return true;
    }

    internal void Execute()
    {
        Console.WriteLine("## Writing Outputs ##");
        if (_writeAuthors)
            WriteAuthors();
        if (_writeGenres)
            WriteGenres();
        if (_writeSeries)
            WriterSeries();
        if (_writeBooks)
            WriteBooks();
        if (_writeFailBooks)
            WriteFailBooks();
        if (_writeReports)
            WriteReports();
    }

    private void WriteDescription(Book book, string location)
    {
        var _location = location;
        var _descFile = Path.Combine(_location, "desc.txt");
        var _infoFile = Path.Combine(_location, "info.json");
        var _description = HtmlToPlainText(book.LookupDescription);
        File.WriteAllText(_descFile, _description);

        var _authors = _bookCommands.GetAuthorsByBookId(book.BookId);
        var _series = _bookCommands.GetBookSeriesByBookId(book.BookId);

        var _bookInfo = new BookInfo();

        _bookInfo.GoodreadsLink = book.LookupLink;
        _bookInfo.Title = book.LookupTitle == null ? book.BookTitle : book.LookupTitle;
        _bookInfo.Authors.AddRange(_authors.Select(a => a.AuthorName));
        _bookInfo.Series.AddRange(_series);
        _bookInfo.GoodreadsDescription = book.LookupDescription;
        _bookInfo.GoodreadsFetchTried = book.LookupFetchTried;

        File.WriteAllText(_infoFile, JsonConvert.SerializeObject(_bookInfo, Formatting.Indented));
    }

    private void WriteGenres()
    {
        Console.WriteLine(" == Writing Genres ==");
        if (!Directory.Exists(_rootDir))
            throw new Exception($"{_rootDir} does not exist. Exiting");
        var _topLevelDir = string.IsNullOrEmpty(_topLevelFolder) ? "Genres" : _topLevelFolder;
        var _toplevelDir = Path.Combine(_rootDir, _topLevelDir);
        var _genres = _bookCommands.GetGenres();

        var i = 1;
        var count = _genres.Count();
        foreach (var _genre in _genres)
        {
            var _genreDir = Path.Combine(_toplevelDir, _genre.GenreName);
            Directory.CreateDirectory(_genreDir);

            var _books = _bookCommands.GetBooksByGenreId(_genre.GenreId);

            //var _books = _bookCommands.GetBooksByAuthorId(_author.AuthorId);

            foreach (var _book in _books)
            {
                Console.Write(
                    $"\r({i}/{count}) Writing Book: {SafeSubstring(_book.LookupTitle, 40)}                          ");
                var _title = CleanTitle(_book.LookupTitle == null ? _book.BookTitle : _book.LookupTitle);
                var _bookDir = Path.Combine(_genreDir, _title);
                Directory.CreateDirectory(_bookDir);
                WriteLinks(_book, _bookDir);
                WriteDescription(_book, _bookDir);
            }

            i++;
        }
    }

    private void WriterSeries()
    {
        Console.WriteLine(" == Writing Series ==");
        if (!Directory.Exists(_rootDir))
            throw new Exception($"{_rootDir} does not exist. Exiting");
        var _topLevelDir = string.IsNullOrEmpty(_topLevelFolder) ? "Series" : _topLevelFolder;
        var _toplevelDir = Path.Combine(_rootDir, _topLevelDir);
        var _series = _bookCommands.GetAllSeries();

        var i = 1;
        var count = _series.Count();
        foreach (var s in _series)
        {
            var _seriesDir = Path.Combine(_toplevelDir, NormalizeTitle(s.SeriesName));
            Directory.CreateDirectory(_seriesDir);

            var _books = _bookCommands.GetSeriesListBySeriesId(s.SeriesId);

            //var _books = _bookCommands.GetBooksByAuthorId(_author.AuthorId);

            foreach (var b in _books)
            {
                var _book = _bookCommands.GetBook(b.BookId);
                Console.Write(
                    $"\r({i}/{count}) Writing Book: {SafeSubstring(_book.LookupTitle, 40)}                          ");
                var _volume = string.IsNullOrEmpty(b.SeriesVolume) ? null : $"Book {b.SeriesVolume}, ";
                var _title = _volume +
                             CleanTitle(_book.LookupTitle == null ? _book.BookTitle : _book.LookupTitle);
                var _bookDir = Path.Combine(_seriesDir, _title);
                Directory.CreateDirectory(_bookDir);
                WriteLinks(_book, _bookDir);
                WriteDescription(_book, _bookDir);
            }

            i++;
        }
    }

    private bool LinkFile(string link, string target)
    {
        var psi = new ProcessStartInfo();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            psi.FileName = "/bin/ln";
            psi.Arguments = $" \"{target}\" \"{link}\"";
        }
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //{
        //    psi.FileName = "cmd.exe";
        //    psi.Arguments = $"/c mklink /j \"{link}\" \"{target}\"";
        //}
        else
        {
            throw new Exception("Unsupported platform that I dont know how to symlink on");
        }

        if (!File.Exists(link))
        {
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            var p = Process.Start(psi);
            p.WaitForExit();
            p.Close();
        }

        return true;
    }

    private bool LinkDirectory(string link, string target)
    {
        var psi = new ProcessStartInfo();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            psi.FileName = "/bin/ln";
            psi.Arguments = $"-s \"{target}\" \"{link}\"";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c mklink /j \"{link}\" \"{target}\"";
        }
        else
        {
            throw new Exception("Unsupported platform that I dont know how to symlink on");
        }

        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        var p = Process.Start(psi);
        p.WaitForExit();
        p.Close();

        return true;
    }

    private void WriteLinks(Book book, string target)
    {
        var _source = book.BookLocation;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            LinkDirectory(target, _source);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var files = Directory.GetFiles(_source, "*.mp3", SearchOption.TopDirectoryOnly);

            foreach (var f in files)
            {
                var _newPath = Path.Combine(target, Path.GetFileName(f));
                LinkFile(_newPath, f);
            }

            // Write cover and description
            var _descFile = Path.Combine(target, "desc.txt");
            var _coverFile = Path.Combine(target, "cover.jpg");
            var _description = HtmlToPlainText(book.LookupDescription);
            File.WriteAllText(_descFile, _description);
            var _coverImage = _bookCommands.GetImage(book.BookId);
            if (_coverImage != null && _coverImage.ImageData != null)
                File.WriteAllBytes(_coverFile, _coverImage.ImageData);
        }
        else
        {
            throw new Exception("Unsupported platform that I dont know how to link on");
        }
    }

    private void WriteAuthors()
    {
        if (!Directory.Exists(_rootDir))
            throw new Exception($"{_rootDir} does not exist. Exiting");
        var _topLevelDir = string.IsNullOrEmpty(_topLevelFolder) ? "Authors" : _topLevelFolder;
        var _toplevelDir = Path.Combine(_rootDir, _topLevelDir);
        var _books = _bookCommands.GetBooks();

        var i = 1;
        var count = _books.Count();
        Console.WriteLine(" == Writing Authors ==");
        foreach (var _book in _books)
        {
            Console.Write(
                $"\r({i++}/{count}) Writing Book: {SafeSubstring(_book.BookTitle, 40)}                          ");
            var _authors = _bookCommands.GetAuthorsByBookId(_book.BookId);

            var _goodReadsAuthors = _authors.Where(a => a.GoodReadsAuthor).ToList();

            if (_goodReadsAuthors.Count() != 0)
                _authors = _goodReadsAuthors;

            foreach (var _author in _authors)
            {
                var _authDir = Path.Combine(_toplevelDir, NormalizeTitle(_author.AuthorName));
                var _title = CleanTitle(_book.BookTitle);
                var _bookDir = Path.Combine(_authDir, _title);

                if (_bookDir.Contains('"'))
                    _bookDir = _bookDir.Replace('"', ' ');

                Directory.CreateDirectory(_bookDir);
                WriteLinks(_book, _bookDir);
                WriteDescription(_book, _bookDir);
            }
        }

        Console.WriteLine(string.Empty);
    }

    private void WriteReports()
    {
        Console.WriteLine(" - Writing Reports");
        if (!Directory.Exists(_rootDir))
            throw new Exception($"{_rootDir} does not exist. Exiting");

        var _failedBooks = _bookCommands.GetBooksFailedLookup();
        var _missingGoodreads = _bookCommands.GetBooksMissingLookup();
        var _failedHintList = new List<FailBookHint>();
        var _missingHintList = new List<FailBookHint>();
        var _allBooks = _bookCommands.GetBooks();
        var _allHintList = new List<GoodBookHint>();
        var _suspectBooks = _bookCommands.GetBooks();
        var _suspectBookList = new List<SuspectBookHint>();

        foreach (var f in _failedBooks)
        {
            var _bookHint = new FailBookHint
            {
                Title = f.BookTitle,
                GoodreadsLink = f.LookupLink,
                BookId = f.BookId
            };

            _failedHintList.Add(_bookHint);
        }

        foreach (var f in _missingGoodreads)
        {
            var _bookHint = new FailBookHint
            {
                Title = f.BookTitle,
                GoodreadsLink = f.LookupLink,
                BookId = f.BookId
            };

            _missingHintList.Add(_bookHint);
        }

        foreach (var s in _suspectBooks)
            if (!CleanForMatchTitle(s.BookTitle).Contains(CleanForMatchTitle(s.LookupTitle)))
            {
                var _bookHint = new SuspectBookHint
                {
                    Title = s.BookTitle,
                    GoodReadsTitle = s.LookupTitle,
                    GoodreadsLink = s.LookupLink,
                    BookId = s.BookId
                };

                _suspectBookList.Add(_bookHint);
            }

        foreach (var a in _allBooks)
        {
            var _series = _bookCommands.GetBookSeriesByBookId(a.BookId);
            var _bookHint = new GoodBookHint
            {
                Title = a.BookTitle,
                GoodReadsTitle = a.LookupTitle,
                SeriesTitle = _series.Count > 0 ? _series[0].SeriesName : string.Empty,
                SeriesVolume = _series.Count > 0 ? _series[0].SeriesVolume : string.Empty,
                GoodreadsLink = a.LookupLink,
                BookId = a.BookId
            };

            _allHintList.Add(_bookHint);
        }

        var _topLevelFolder = string.IsNullOrEmpty(this._topLevelFolder) ? "Reports" : this._topLevelFolder;
        var _topLevelDir = Path.Combine(_rootDir, _topLevelFolder);

        var _di = Directory.CreateDirectory(_topLevelDir);

        var _failedBookFile = Path.Combine(_topLevelDir, "FailedBooksHintFile.csv");
        var writer = new StreamWriter(_failedBookFile, false);
        var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_failedHintList);
        writer.Close();

        var _missingBookFile = Path.Combine(_topLevelDir, "MissingBooksHintFile.csv");
        writer = new StreamWriter(_missingBookFile, false);
        csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_missingHintList);
        writer.Close();

        var _suspectBookFile = Path.Combine(_topLevelDir, "SuspectBooksHintFile.csv");
        writer = new StreamWriter(_suspectBookFile, false);
        csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_suspectBookList);
        writer.Close();

        var _allBookFile = Path.Combine(_topLevelDir, "AllBooksHintFile.csv");
        writer = new StreamWriter(_allBookFile, false);
        csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_allHintList);
        writer.Close();
    }

    private void WriteFailBooks()
    {
        Console.WriteLine(" - Writing Failbooks");
        if (!Directory.Exists(_rootDir))
            throw new Exception($"{_rootDir} does not exist. Exiting");
        var _topLevelDir = string.IsNullOrEmpty(_topLevelFolder) ? "FailedBooks" : _topLevelFolder;
        var _toplevelDir = Path.Combine(_rootDir, _topLevelDir);
        var _books = _bookCommands.GetBooksFailedLookup();

        foreach (var _book in _books)
        {
            var _title = CleanTitle(_book.BookTitle);
            var _bookDir = Path.Combine(_toplevelDir, _title);
            Directory.CreateDirectory(_bookDir);
            WriteLinks(_book, _bookDir);
            WriteDescription(_book, _bookDir);
        }
    }

    private void WriteBooks()
    {
        Console.WriteLine(" - Writing Books");
        if (!Directory.Exists(_rootDir))
            throw new Exception($"{_rootDir} does not exist. Exiting");
        var _topLevelDir = string.IsNullOrEmpty(_topLevelFolder) ? "Books" : _topLevelFolder;
        var _toplevelDir = Path.Combine(_rootDir, _topLevelDir);
        var _books = _bookCommands.GetBooks();

        var i = 1;
        var count = _books.Count();
        Console.WriteLine(" == Writing Books ==");
        foreach (var _book in _books)
        {
            Console.Write(
                $"\r({i++}/{count}) Writing Book: {SafeSubstring(_book.BookTitle, 40)}                          ");
            var _title = CleanTitle(_book.BookTitle);
            var _bookDir = Path.Combine(_toplevelDir, _title);
            Directory.CreateDirectory(_bookDir);
            WriteLinks(_book, _bookDir);
            WriteDescription(_book, _bookDir);
        }

        Console.WriteLine(string.Empty);
    }

    private string NormalizeTitle(string s)
    {
        s = s.Replace(":", " ").Replace("_", " ").Replace("?", " ").Replace("!", " ").Replace("'", "").Replace("'", "")
            .Replace("-", " ");

        if (s.EndsWith('.'))
            s = s.Substring(0, s.Length - 1);

        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    private string CleanForMatchTitle(string s)
    {
        s = s.ToLower().Replace(":", " ").Replace("_", " ").Replace("?", " ").Replace("!", " ").Replace("'", "")
            .Replace("'", "").Replace("-", " ");

        if (s.StartsWith("the")) s = s.Substring(3).Trim();

        s = Regex.Replace(s, @"^the\s+|-\s*the\s+", string.Empty);
        s = Regex.Replace(s, @"^a\s+|-\s*a\s+", string.Empty);
        s = s.Replace("-", " ").Replace(",", " ");

        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    private string CleanTitle(string title)
    {
        return title.Replace("(Unabridged)", string.Empty).Replace(":", " -").Replace("'", string.Empty).Trim();
    }

    private string SafeSubstring(string input, int length)
    {
        if (input.Length < length)
            return input.PadRight(length + 4);
        return input.Substring(0, length) + " ...";
    }

    private string HtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        string buf;
        var block = "address|article|aside|blockquote|canvas|dd|div|dl|dt|" +
                    "fieldset|figcaption|figure|footer|form|h\\d|header|hr|li|main|nav|" +
                    "noscript|ol|output|p|pre|section|table|tfoot|ul|video";

        var patNestedBlock = $"(\\s*?</?({block})[^>]*?>)+\\s*";
        buf = Regex.Replace(html, patNestedBlock, "\n", RegexOptions.IgnoreCase);

        // Replace br tag to newline.
        buf = Regex.Replace(buf, @"<(br)[^>]*>", "\n", RegexOptions.IgnoreCase);

        // (Optional) remove styles and scripts.
        buf = Regex.Replace(buf, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.Singleline);

        // Remove all tags.
        buf = Regex.Replace(buf, @"<[^>]*(>|$)", "", RegexOptions.Multiline);

        // Replace HTML entities.
        buf = WebUtility.HtmlDecode(buf);
        return buf;
    }
}