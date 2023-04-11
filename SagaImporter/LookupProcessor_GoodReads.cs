using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using CsvHelper;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SagaDb;
using SagaDb.Database;
using SagaDb.Models;
using SagaUtil;

namespace SagaImporter;

internal class LookupProcessorGoodReads
{
    private const string Goodreads = "https://www.goodreads.com";
    private readonly KeyMaker _keyMaker;
    private readonly Random _random;
    private readonly HtmlWeb _web;
    private bool _authors;
    private BookCommands _bookCommands;
    private bool _books;
    private string _hintfile;
    private bool _images;
    private bool _purge;
    private bool _retry;
    private bool _series;

    public LookupProcessorGoodReads()
    {
        _web = new HtmlWeb();
        _random = new Random();
        _keyMaker = new KeyMaker();
    }

    internal bool Initialize(LookupOptions l)
    {
        _bookCommands = new BookCommands(l.DatabaseFile);
        _retry = l.Retry;
        _hintfile = l.HintFile;
        _authors = l.Authors;
        _books = l.Books;
        _series = l.Series;
        _purge = l.PurgeAndRebuild;
        _images = l.Images;
        return true;
    }

    internal void Execute()
    {
        if (_books)
        {
            if (!string.IsNullOrEmpty(_hintfile))
            {
                // Failed Book CSV File
                using (var reader = new StreamReader(_hintfile))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                    {
                        var records = csv.GetRecords<FailBookHint>();

                        foreach (var r in records)
                        {
                            Console.WriteLine($"Updating {r.Title}");
                            var book = _bookCommands.GetBook(r.BookId);
                            var bookLookup = new QueryResult(null, null, r.GoodreadsLink);
                            var entry = ProcessGoodreadsBookEntry(bookLookup);
                            book.LookupFetchTried = true;
                            if (entry != null)
                            {
                                UpdateGenres(entry, book);
                                UpdateSeries(entry, book);
                                UpdateAuthors(entry, book);
                                UpdateBook(entry, book);
                            }
                        }
                    }
                }
            }
            else
            {
                List<Book> books;
                if (_retry)
                    books = _bookCommands.GetBooksFailedLookup();
                else
                    books = _bookCommands.GetBooksMissingLookup();
                Console.WriteLine($"Processing {books.Count} books");

                books.Sort((x, y) => String.Compare(x.BookId, y.BookId, StringComparison.Ordinal)); // Not in alphabetical order
                // var shuffled = myList.OrderBy(x => Guid.NewGuid()).ToList();

                foreach (var book in books)
                {
                    var authors = _bookCommands.GetAuthorsByBookId(book.BookId);

                    var title = book.BookTitle.ToLower();

                    Console.Write($"Good reads lookup of {title} ...");

                    var searchResult = SearchGoodreads(book, authors);
                    book.LookupFetchTried = true;
                    if (searchResult != null)
                    {
                        var entry = ProcessGoodreadsBookEntry(searchResult);

                        if (entry != null)
                        {
                            UpdateGenres(entry, book);
                            UpdateSeries(entry, book);
                            UpdateAuthors(entry, book);
                            UpdateBook(entry, book);
                        }

                        Console.WriteLine(" done");
                    }
                    else
                    {
                        _bookCommands.UpdateBook(book);
                        Console.WriteLine(" failed");
                    }
                }
            }
        }

        int i = 1;
        int count;

        if (_purge)
        {
            Console.WriteLine("== Purging and Rebuilding Series and Author information ==");

            Console.WriteLine("- Purging Authors");
            _bookCommands.PurgeAuthors();
            _bookCommands.PurgeAuthorLinks();
            Console.WriteLine("- Purging Series");
            _bookCommands.PurgeSeries();
            _bookCommands.PurgeSeriesLinks();
            Console.WriteLine("- Purging Genres");
            _bookCommands.PurgeGenres();
            _bookCommands.PurgeGenreLinks();
            Console.WriteLine("- Purging Images");
            _bookCommands.PurgeImages();
            var books = _bookCommands.GetBooks();
            count = books.Count;
            books.Shuffle();

            Console.WriteLine("== Processing Authors ==");
            foreach (var b in books)
            {
                Console.Write($"\r({i++}/{count}) {FormatOutputLine(b.LookupTitle)}");
                var bookLookup = new QueryResult(null, null, b.LookupLink);
                var entry = ProcessGoodreadsBookEntry(bookLookup);

                if (entry != null)
                {
                    UpdateGenres(entry, b);
                    UpdateSeries(entry, b);
                    UpdateAuthors(entry, b);
                }
            }
        }

        if (_series)
        {
            // Doesnt try to do anything smart. Looks up each book series and gets all books in that series.
            // Then sees if any of those books exist in the DB and adds them to the series if it does
            // Requires the books in question have existing good reads links.
            // Doesnt try to do anything clever. That is a recipe for trouble.
            var seriesList = _bookCommands.GetAllSeries();
            seriesList = seriesList.Where(s => s.SeriesDescription == null).ToList();

            i = 1;
            count = seriesList.Count;
            Console.Write("\n== Processing Series ==");
            foreach (var s in seriesList)
            {
                Console.WriteLine($"\r({i++}/{count}) {FormatOutputLine(s.SeriesName)}");
                var link = s.SeriesLink;
                var seriesBooks = GetSeriesFromGoodReads(link);

                if (seriesBooks.Count > 0)
                {
                    // Update the series description from first book
                    s.SeriesDescription = seriesBooks[0].SeriesDescription;
                    _bookCommands.UpdateSeries(s);
                }

                foreach (var b in seriesBooks)
                {
                    var seriesBook = _bookCommands.GetBookByGoodReadsLink(b.BookLink);
                    if (seriesBook.Count == 1)
                    {
                        // Ok we found the right book and only one
                        var book = seriesBook[0];

                        // Add a series link for the book if one doesn't exist.
                        _bookCommands.LinkBookToSeries(book, s, b.BookVolume);
                    }
                }
            }
        }

        if (_authors)
        {
            var authorList = _bookCommands.GetAuthorsWithGoodReads();
            i = 1;
            count = authorList.Count;

            Console.WriteLine("\n== Processing Authors ==");
            foreach (var a in authorList)
            {
                Console.Write($"\r({i++}/{count} {FormatOutputLine(a.AuthorName)}");
                var authDetails = GetAuthorFromGoodReads(a.GoodReadsAuthorLink);

                a.AuthorDescription = authDetails.AuthorDesc;
                a.AuthorImageLink = authDetails.AuthorImageLink;
                a.AuthorWebsite = authDetails.AuthorWebsite;
                a.Born = authDetails.BirthDate;
                a.Died = authDetails.DeathDate;
                a.Influences = authDetails.AuthorInfluences;
                a.Genre = authDetails.AuthorGenres;
                a.Twitter = authDetails.AuthorTwitter;

                _bookCommands.UpdateAuthor(a);
            }
        }

        if (_images)
        {
            var authorList = _bookCommands.GetAuthorsWithGoodReads();
            var bookList = _bookCommands.GetBooks();

            Console.WriteLine("\n== Downloading Cover Images ==");

            i = 1;
            count = bookList.Count;
            foreach (var b in bookList)
                if (!string.IsNullOrEmpty(b.LookupCoverImage))
                {
                    Console.Write($"\r({i++}/{count} {FormatOutputLine(b.LookupTitle)}");
                    var dbImage = _bookCommands.GetImage(b.BookId);
                    var image = ImageHelper.DownloadImage(b.LookupCoverImage);

                    if (dbImage == null)
                    {
                        dbImage = new DbImage();
                        dbImage.ImageId = b.BookId;
                        dbImage.ImageData = image;

                        _bookCommands.InsertImage(dbImage);
                    }
                    else
                    {
                        dbImage.ImageData = image;
                        _bookCommands.UpdateImage(dbImage);
                    }
                }

            Console.WriteLine("== Downloading Author Images ==");

            i = 1;
            count = authorList.Count;
            foreach (var a in authorList)
                if (!string.IsNullOrEmpty(a.AuthorImageLink))
                {
                    Console.Write($"\r({i++}/{count} {FormatOutputLine(a.AuthorName)}");
                    var dbImage = _bookCommands.GetImage(a.AuthorId);
                    var image = ImageHelper.DownloadImage(a.AuthorImageLink);

                    if (dbImage == null)
                    {
                        dbImage = new DbImage();
                        dbImage.ImageId = a.AuthorId;
                        dbImage.ImageData = image;

                        _bookCommands.InsertImage(dbImage);
                    }
                    else
                    {
                        dbImage.ImageData = image;
                        _bookCommands.UpdateImage(dbImage);
                    }
                }
        }
    }

    private AuthorDescription GetAuthorFromGoodReads(string authorLink)
    {
        var authorDescription = new AuthorDescription();

        var authorData = DoWebQuery(authorLink);

        HtmlNodeCollection dataNodes = null;

        if (authorData != null)
        {
            authorDescription.AuthorImageLink = authorData.DocumentNode
                .SelectSingleNode("//div[contains(@class, \"leftContainer\")]//img")
                ?.GetAttributeValue("src", string.Empty).Trim();
            dataNodes = authorData.DocumentNode.SelectNodes("//div[@class=\"dataTitle\"]");
        }

        if (dataNodes != null)
            foreach (var n in dataNodes)
            {
                var dataTitle = n.InnerText.ToLower();
                var dataValue = n.SelectSingleNode("./following-sibling::div").InnerText.Trim();
                switch (dataTitle)
                {
                    case "born":
                        authorDescription.BirthDate = dataValue;
                        break;

                    case "genre":
                        authorDescription.AuthorGenres = dataValue;
                        break;

                    case "died":
                        authorDescription.DeathDate = dataValue;
                        break;

                    case "website":
                        authorDescription.AuthorWebsite = dataValue;
                        break;

                    case "influences":
                        authorDescription.AuthorInfluences = dataValue;
                        break;

                    case "twitter":
                        authorDescription.AuthorTwitter = dataValue;
                        break;

                    case "url":
                    case "member since":
                        break;

                    default:
                        Console.WriteLine("Unknown DataTitle -> " + dataTitle);
                        break;
                }
            }

        if (authorData != null && authorData.DocumentNode.SelectNodes(
                "//div[@class=\"rightContainer\"]//div[@class=\"aboutAuthorInfo\"]//span[contains(@id, \"freeText\")]") is { } descNodes)
        {
            if (descNodes.Count > 1)
                authorDescription.AuthorDesc = descNodes[1].InnerHtml;
            else
                authorDescription.AuthorDesc = descNodes[0].InnerHtml;
        }

        return authorDescription;
    }

    private void UpdateAuthors(BookDetails entry, Book book)
    {
        foreach (var a in entry.Authors)
        {
            var authorKey = _keyMaker.AuthorKey(a.AuthorName);

            var author = _bookCommands.GetAuthor(authorKey);

            if (author == null)
            {
                author = new Author();
                author.AuthorId = authorKey;
                author.AuthorName = a.AuthorName;
                author.GoodReadsAuthor = true;
                author.GoodReadsAuthorLink = a.AuthorLink;
                author.AuthorType = a.AuthorType;
                _bookCommands.InsertAuthor(author);
            }
            else
            {
                author.GoodReadsAuthor = true;
                author.GoodReadsAuthorLink = a.AuthorLink;
                author.AuthorType = a.AuthorType;
                _bookCommands.UpdateAuthor(author);
            }

            _bookCommands.LinkAuthorToBook(author, book, a.AuthorType);
        }
    }

    private void UpdateBook(BookDetails entry, Book book)
    {
        book.LookupDescription = entry.BookDescription;
        book.LookupTitle = entry.BookTitle;
        book.LookupLink = entry.BookLink;
        book.LookupCoverImage = entry.CoverImageLink;
        _bookCommands.UpdateBook(book);
    }

    private void UpdateSeries(BookDetails entry, Book book)
    {
        if (string.IsNullOrEmpty(entry.SeriesTitle)) return;

        var seriesId = _keyMaker.SeriesKey(entry.SeriesTitle);
        var series = _bookCommands.GetSeries(seriesId);

        if (series == null)
        {
            series = new Series();
            series.SeriesId = seriesId;
            series.SeriesName = entry.SeriesTitle;
            series.SeriesLink = entry.SeriesLink;
            _bookCommands.InsertSeries(series);
        }

        _bookCommands.LinkBookToSeries(book, series, entry.SeriesVolume);
    }

    private void UpdateGenres(BookDetails entry, Book book)
    {
        foreach (var g in entry.Genres)
        {
            var genreId = _keyMaker.GenreKey(g);
            var genre = _bookCommands.GetGenre(genreId);

            if (genre == null)
            {
                genre = new Genre();
                genre.GenreId = genreId;
                genre.GenreName = g;
                _bookCommands.InsertGenre(genre);
            }

            _bookCommands.LinkBookToGenre(book, genre);
        }
    }

    private BookDetails ProcessGoodreadsBookEntry(QueryResult goodreadsEntry)
    {
        HtmlNode leftNode = null;
        HtmlNode rightNode = null;

        for (var i = 0; i < 20; i++)
        {
            var bookResult = DoWebQuery(goodreadsEntry.link);

            if (bookResult != null)
            {
                leftNode = bookResult.DocumentNode.SelectSingleNode("//div[@class=\"BookPage__leftColumn\"]");
                rightNode = bookResult.DocumentNode.SelectSingleNode("//div[@class=\"BookPage__rightColumn\"]");
            }

            if (leftNode != null && rightNode != null) break;

            Console.WriteLine("Failed Query");
        }

        if (leftNode == null || rightNode == null) return null;

        var bookDetails = new BookDetails();
        bookDetails.BookLink = goodreadsEntry.link.Split('?')[0].Trim();

        GetBookDetails(leftNode, rightNode, bookDetails);
        GetGenreList(rightNode, bookDetails);

        return bookDetails;
    }

    private void GetGenreList(HtmlNode rightNode, BookDetails bookDetails)
    {
        var _result = new List<string>();
        var _genreList = new List<string>();
        var _elementList =
            rightNode.SelectNodes(
                "//div[@class=\"stacked\"]//div[contains(@class, \"elementList\")]//div[@class=\"left\"]//a");

        if (_elementList != null)
            foreach (var e in _elementList)
                _genreList.Add(e.InnerText);
        bookDetails.Genres = _genreList;
    }

    private void GetBookDetails(HtmlNode leftNode, HtmlNode rightNode, BookDetails bookDetails)
    {
        var coverImageLink = leftNode.SelectSingleNode("//img[@class='ResponsiveImage']")
            ?.GetAttributeValue("src", string.Empty);
        
        var bookData = rightNode;
        var title = HttpUtility.HtmlDecode(bookData.SelectSingleNode("//h1[@class=\"Text Text__title1\"]")?.InnerText.Trim());
        var authorNodes = bookData.SelectNodes("//div[contains(@class, 'ContributorLinksList')]");
        var moreAuthorNodes = bookData.SelectNodes("//span[@class='toggleContent']/a[@class='authorName']");
        
        var authors = ProcessAuthorDetails(authorNodes, moreAuthorNodes);
        var seriesLabel = bookData.SelectSingleNode("//h2[@id=\"bookSeries\"]/a")?.InnerText.Trim();
        var seriesLinkElement = bookData.SelectSingleNode("//h2[@id=\"bookSeries\"]/a");
        if (seriesLinkElement != null)
        {
            var seriesLink = seriesLinkElement.GetAttributeValue("href", string.Empty);
            var seriesDto = ProcessSeriesLabel(seriesLabel);
            var seriesName = seriesDto.SeriesTitle;
            var seriesVolume = seriesDto.SeriesVolume;
            bookDetails.SeriesTitle = seriesName;
            bookDetails.SeriesVolume = seriesVolume;
            bookDetails.SeriesLink = $"{Goodreads}{seriesLink}";
        }

        var description = bookData
            .SelectSingleNode("//div[@id=\"descriptionContainer\"]/div[@id=\"description\"]/span[2]")?.InnerHtml.Trim();

        if (string.IsNullOrEmpty(description))
            description = bookData
                .SelectSingleNode("//div[@id=\"descriptionContainer\"]/div[@id=\"description\"]/span[1]")?.InnerHtml
                .Trim();

        bookDetails.BookTitle = title;
        bookDetails.BookDescription = description;
        bookDetails.Authors = authors;
        bookDetails.CoverImageLink = coverImageLink;
    }

    public string GetAuthorName(string author)
    {
        if (author.IndexOf('(') != -1) return author.Substring(0, author.IndexOf('(') - 1);

        return author;
    }

    public string GetAuthorTypes(string author)
    {
        if (author.IndexOf('(') != -1) return author.Substring(author.IndexOf('('));

        return string.Empty;
    }

    public List<AuthorDetails> ProcessAuthorDetails(HtmlNodeCollection authors, HtmlNodeCollection moreAuthors)
    {
        var _authorDetailsList = new List<AuthorDetails>();
        foreach (var a in authors)
        {
            var _authorInnerText = HttpUtility.HtmlDecode(a.InnerText.Replace(",", " ").Trim());
            var _name = GetAuthorName(_authorInnerText);
            var _description = GetAuthorTypes(_authorInnerText);
            var _link = a.SelectSingleNode("./a")?.GetAttributeValue("href", "");

            if (string.IsNullOrEmpty(_description))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Author;
                _authorDetailsList.Add(_authorDetails);
            }

            if (_description.ToLower().Contains("author"))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Author;
                _authorDetailsList.Add(_authorDetails);
            }

            if (_description.ToLower().Contains("editor"))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Editor;
                _authorDetailsList.Add(_authorDetails);
            }

            if (_description.ToLower().Contains("translator"))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Translator;
                _authorDetailsList.Add(_authorDetails);
            }

            if (_description.ToLower().Contains("foreword") || _description.ToLower().Contains("introduction"))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Foreword;
                _authorDetailsList.Add(_authorDetails);
            }

            if (_description.ToLower().Contains("contributor"))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Contributor;
                _authorDetailsList.Add(_authorDetails);
            }

            if (_description.ToLower().Contains("illustrator"))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Illustrator;
                _authorDetailsList.Add(_authorDetails);
            }

            if (_description.ToLower().Contains("narrator"))
            {
                var _authorDetails = new AuthorDetails();
                _authorDetails.AuthorLink = _link;
                _authorDetails.AuthorName = _name;
                _authorDetails.AuthorType = AuthorType.Narrator;
                _authorDetailsList.Add(_authorDetails);
            }
        }

        //var _authorList = moreAuthors.First().ParentNode?.InnerText.Trim().Split(',').ToList();
        if (moreAuthors != null)
            foreach (var a in moreAuthors)
            {
                var _authorInnerText = a.InnerText.Trim();
                var _name = a.InnerText.Trim();
                var _description = a.SelectSingleNode("//a/following-sibling::span")?.InnerText.Trim();
                var _link = a.GetAttributeValue("href", "");

                if (string.IsNullOrEmpty(_description))
                {
                    var _authorDetails = new AuthorDetails();
                    _authorDetails.AuthorLink = _link;
                    _authorDetails.AuthorName = _name;
                    _authorDetails.AuthorType = AuthorType.Author;
                    _authorDetailsList.Add(_authorDetails);
                }

                if (_description != null && _description.ToLower().Contains("author"))
                {
                    var authorDetails = new AuthorDetails();
                    authorDetails.AuthorLink = _link;
                    authorDetails.AuthorName = _name;
                    authorDetails.AuthorType = AuthorType.Author;
                    _authorDetailsList.Add(authorDetails);
                }

                if (_description != null && _description.ToLower().Contains("editor"))
                {
                    var authorDetails = new AuthorDetails();
                    authorDetails.AuthorLink = _link;
                    authorDetails.AuthorName = _name;
                    authorDetails.AuthorType = AuthorType.Editor;
                    _authorDetailsList.Add(authorDetails);
                }

                if (_description != null && _description.ToLower().Contains("translator"))
                {
                    var authorDetails = new AuthorDetails();
                    authorDetails.AuthorLink = _link;
                    authorDetails.AuthorName = _name;
                    authorDetails.AuthorType = AuthorType.Translator;
                    _authorDetailsList.Add(authorDetails);
                }

                if (_description != null && (_description.ToLower().Contains("foreword") || _description.ToLower().Contains("introduction")))
                {
                    var authorDetails = new AuthorDetails();
                    authorDetails.AuthorLink = _link;
                    authorDetails.AuthorName = _name;
                    authorDetails.AuthorType = AuthorType.Foreword;
                    _authorDetailsList.Add(authorDetails);
                }

                if (_description.ToLower().Contains("contributor"))
                {
                    var _authorDetails = new AuthorDetails();
                    _authorDetails.AuthorLink = _link;
                    _authorDetails.AuthorName = _name;
                    _authorDetails.AuthorType = AuthorType.Contributor;
                    _authorDetailsList.Add(_authorDetails);
                }

                if (_description.ToLower().Contains("illustrator"))
                {
                    var _authorDetails = new AuthorDetails();
                    _authorDetails.AuthorLink = _link;
                    _authorDetails.AuthorName = _name;
                    _authorDetails.AuthorType = AuthorType.Illustrator;
                    _authorDetailsList.Add(_authorDetails);
                }

                if (_description.ToLower().Contains("narrator"))
                {
                    var _authorDetails = new AuthorDetails();
                    _authorDetails.AuthorLink = _link;
                    _authorDetails.AuthorName = _name;
                    _authorDetails.AuthorType = AuthorType.Narrator;
                    _authorDetailsList.Add(_authorDetails);
                }
            }

        return _authorDetailsList;
    }

    private SeriesDto ProcessSeriesLabel(string seriesLabel)
    {
        var _seriesDto = new SeriesDto();
        seriesLabel = HttpUtility.HtmlDecode(seriesLabel);
        if (!string.IsNullOrEmpty(seriesLabel))
        {
            var _seriesLabel = seriesLabel.Replace('(', ' ').Replace(')', ' ').Replace(',', ' ');
            var _seriesParts = _seriesLabel.Split('#', StringSplitOptions.RemoveEmptyEntries);

            _seriesDto.SeriesTitle = _seriesParts.ElementAtOrDefault(0)?.Trim();
            _seriesDto.SeriesVolume = _seriesParts.ElementAtOrDefault(1);

            if (!string.IsNullOrEmpty(_seriesDto.SeriesVolume))
                _seriesDto.SeriesVolume = _seriesDto.SeriesVolume.Trim();
        }

        return _seriesDto;
    }

    private List<string> MakeGoodReadsQueriesSimple(string title)
    {
        var _result = new List<string>();
        var _baseQuery = $"{Goodreads}/search?utf8=%E2%9C%93&query=";

        var _splitTitle = title.Split("-", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var _count = _splitTitle.Length;

        for (var i = 1; i < _count; i++)
        {
            var _subQuery = _splitTitle.Take(i);
            var _title = string.Join(" ", _subQuery);
            var _titleQuery = Regex.Replace(_title, @"\s+", " ").Trim().Replace(" ", "+");
            _result.Add(_baseQuery + _titleQuery);
        }

        _result.Reverse();

        return _result;
    }

    private string MakeGoodReadsQueryFromTitle(string title)
    {
        var _baseQuery = $"{Goodreads}/search?utf8=%E2%9C%93&query=";
        var _titleQuery = Regex.Replace(title, @"\s+", " ").Trim().Replace(" ", "+");
        return _baseQuery + _titleQuery;
    }

    private string MakeGoodReadsQueryFromTitleAuthor(string title, List<Author> authors)
    {
        var _baseQuery = $"{Goodreads}/search?utf8=%E2%9C%93&query=";
        var _titleQuery = Regex.Replace(title, @"\s+", " ").Trim().Replace(" ", "+");
        var _authorQueryList = new List<string>();

        foreach (var a in authors)
            _authorQueryList.Add(Regex.Replace(a.AuthorName, @"\s+", " ").Trim().Replace(" ", "+"));

        return _baseQuery + _titleQuery + "+" + string.Join("+", _authorQueryList);
    }

    private string MakeGoodReadsQueryFromAuthors(List<Author> authors)
    {
        var _baseQuery = $"{Goodreads}/search?utf8=%E2%9C%93&query=";

        var _authorQueryList = new List<string>();

        foreach (var a in authors)
            _authorQueryList.Add(Regex.Replace(a.AuthorName, @"\s+", " ").Trim().Replace(" ", "+"));

        return _baseQuery + string.Join("+", _authorQueryList);
    }

    private HtmlDocument DoWebQuery(string request)
    {
        Thread.Sleep(_random.Next(100, 250)); // 2 - 4 sec delay so as not to upset good reads
        HtmlDocument result = null;

        for (var i = 0; i < 5; i++)
            // Try it a few times, throws exceptions on connection problems
            try
            {
                result = _web.Load(request);
                return result;
            }
            catch
            {
            }

        return null;
    }

    private List<SeriesResult> GetSeriesFromGoodReads(string link)
    {
        var _queryResult = DoWebQuery(link);

        if (_queryResult == null)
            return null;

        var _seriesBookList = ProcessSeriesQueryToList(_queryResult);

        return _seriesBookList;
    }

    private List<SeriesResult> ProcessSeriesQueryToList(HtmlDocument _queryResult)
    {
        var _result = new List<SeriesResult>();

        var _seriesDescJson = _queryResult.DocumentNode
            .SelectSingleNode("//div[@data-react-class=\"ReactComponents.SeriesHeader\"]")
            ?.GetAttributeValue("data-react-props", string.Empty);
        var _seriesDesc = JsonConvert.DeserializeObject<SeriesDescription>(HttpUtility.HtmlDecode(_seriesDescJson));

        var _seriesListElements =
            _queryResult.DocumentNode.SelectNodes(".//div[@data-react-class=\"ReactComponents.SeriesList\"]");

        var _grSeriesItems = new List<GRSeries>();

        foreach (var e in _seriesListElements)
        {
            var _jsonNode = HttpUtility.HtmlDecode(e.GetAttributeValue("data-react-props", string.Empty));
            var _seriesBooks = JsonConvert.DeserializeObject<SeriesChunk>(_jsonNode);

            if (_seriesBooks.seriesHeaders != null)
                for (var i = 0; i < _seriesBooks.series.Count; i++)
                    _seriesBooks.series[i].book.volume = _seriesBooks.seriesHeaders[i] != null
                        ? _seriesBooks.seriesHeaders[i].Replace("Book", " ").Trim()
                        : string.Empty;
            _grSeriesItems.AddRange(_seriesBooks.series);
        }

        foreach (var si in _grSeriesItems)
        {
            var _bookTitle = si.book.title.Split('(')[0].Trim();
            var _bookVolumeSplit = si.book.title.Replace(")", string.Empty).Split('#');
            var _bookVolume = _bookVolumeSplit.Count() > 1 ? _bookVolumeSplit[1] : string.Empty;

            _result.Add(new SeriesResult
            {
                BookTitle = _bookTitle,
                BookVolume = si.book.volume,
                BookLink = Goodreads + si.book.bookUrl,
                SeriesTitle = _seriesDesc.title,
                SeriesDescription = _seriesDesc.description.html,
                CoverLink = si.book.imageUrl
            });
        }

        return _result;
    }

    private QueryResult SearchGoodreads(Book book, List<Author> authors)
    {
        // Try a simple search for the title and then the title split on any dashes first
        // That seems to have been greatly improved
        var _searchRequestList = MakeGoodReadsQueriesSimple(book.BookTitle);

        foreach (var _query in _searchRequestList)
        {
            var _searchResult = DoWebQuery(_query);
            var _queryResult = ProcessSearchQueryToList(_searchResult);
            var _match = MatchBookResults(_queryResult, book, authors);

            if (_match != null)
                return _match;
        }

        var _searchRequest = MakeGoodReadsQueryFromAuthors(authors);

        var _bookResults = new List<QueryResult>();

        for (var i = 0; i < 5; i++) // Get up to the first 100 hits
        {
            var _searchResult = DoWebQuery(_searchRequest);
            if (_searchRequest == null)
                return null;

            _bookResults.AddRange(ProcessSearchQueryToList(_searchResult));
            _searchRequest = GetNextLink(_searchResult);
            if (_searchRequest == null) break;
        }

        var _bookMatch = MatchBookResults(_bookResults, book, authors);

        if (_bookMatch == null)
        {
            // Lets add the title in with the author name, this is kinda messy if there is
            // series names and things, but is for the case of Limitless by Alan Glynn
            var _book = NormalizeTitle(book.BookTitle);
            _searchRequest = MakeGoodReadsQueryFromTitleAuthor(_book, authors);
            var _searchResult = DoWebQuery(_searchRequest);

            if (_searchResult == null)
                return null;

            _bookResults = ProcessSearchQueryToList(_searchResult);
            // Trying to match the normalized title exactly but ignoring author
            _bookMatch = MatchBookResults(_bookResults, book, authors);
        }

        return _bookMatch;
    }

    private QueryResult MatchBookResults(List<QueryResult> bookResults, Book book)
    {
        var _bookToMatch = NormalizeTitle(HttpUtility.HtmlDecode(book.BookTitle));

        foreach (var br in bookResults)
        {
            var _resultTitle = NormalizeTitle(HttpUtility.HtmlDecode(br.Title));

            if (_bookToMatch.Contains(_resultTitle)) return br;
        }

        return null;
    }

    private QueryResult MatchBookResults(List<QueryResult> bookResults, Book book, List<Author> authors)
    {
        var _bookToMatch = NormalizeTitle(HttpUtility.HtmlDecode(book.BookTitle));

        QueryResult _bestMatch = null;
        var _bestMatchScore = -1;

        foreach (var br in bookResults)
        {
            var _matchScore = -1;
            // Now try to find the matching book

            // Match at least some of the authors. No author match def. wrong book.
            // Drop all spaces from author
            // Multi author anthos will only list a couple of authors. So match at least one author then stop

            var authorMatch = 0;

            foreach (var a in authors)
            foreach (var b in br.authors)
            {
                var a1 = NormalizeAuthor(a.AuthorName);
                var a2 = NormalizeAuthor(HttpUtility.HtmlDecode(b));

                var match = CalculateStringSimilarity(a1, a2);

                if (match > 0.70) authorMatch++;
            }

            if (authorMatch > 0) _matchScore = authorMatch;

            if (authorMatch == 0) _matchScore = -2;

            var _resultTitle = NormalizeTitle(HttpUtility.HtmlDecode(br.Title));
            var _resultSeries = string.IsNullOrEmpty(br.seriesTitle)
                ? null
                : NormalizeTitle(HttpUtility.HtmlDecode(br.seriesTitle));

            if (_bookToMatch.Contains(_resultTitle)) _matchScore += 2;

            if (!string.IsNullOrEmpty(br.seriesCount) && _bookToMatch.Contains($"book {br.seriesCount}")) _matchScore++;

            if (!string.IsNullOrEmpty(_resultSeries) && _bookToMatch.Contains(_resultSeries)) _matchScore++;

            if (!string.IsNullOrEmpty(_resultSeries) && CalculateStringSimilarity(_resultTitle, _resultSeries) > 0.95)
                _matchScore -= 2;

            //_matchScore += LongestCommonSubstringLength(_resultTitle, _bookToMatch);

            // Count the total number of common substrings and add that
            _matchScore += _resultTitle.Split().Intersect(_bookToMatch.Split(), new LevenshteinComparer()).Count();
            if (!string.IsNullOrEmpty(_resultSeries))
                _matchScore += _resultSeries.Split().Intersect(_bookToMatch.Split(), new LevenshteinComparer()).Count();

            // Record all the best matches and return them. Search with each different methodoly and see what is best
            // at the end.
            if (_matchScore > _bestMatchScore)
            {
                _bestMatchScore = _matchScore;
                _bestMatch = br;
            }
        }

        // Check the best match does make sense as a match
        if (_bestMatch != null && (_bookToMatch.Contains(NormalizeTitle(_bestMatch.Title)) ||
                                   NormalizeTitle(_bestMatch.Title).Contains(_bookToMatch))) return _bestMatch;

        return null;
    }

    private string NormalizeAuthor(string s)
    {
        //Regex.Replace(a.AuthorName, @"\s+", " ").Tr

        s = Regex.Replace(s, @"\s*,?\s*Jr\.?\s*$", string.Empty, RegexOptions.IgnoreCase)
            .Trim(); // Remove Jr and cominations there of at end of string)
        s = s.Replace('"', ' ').Replace("'", " "); // E.E. "Doc" Smith case

        return Regex.Replace(s, @"\s+", string.Empty).Replace(".", string.Empty).ToLower().Trim();
    }

    private string NormalizeTitle(string s)
    {
        s = s.ToLower().Replace(":", " ").Replace("_", " ").Replace("?", " ").Replace("!", " ").Replace("'", "")
            .Replace("'", "").Replace("-", " ");

        if (s.StartsWith("the")) s = s.Substring(3).Trim();

        s = Regex.Replace(s, @"^the\s+|-\s*the\s+", string.Empty);
        s = Regex.Replace(s, @"^a\s+|-\s*a\s+", string.Empty);
        s = s.Replace("-", " ").Replace(",", " ");

        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    private string GetNextLink(HtmlDocument searchResult)
    {
        var _links = searchResult.DocumentNode.Descendants("a");

        foreach (var l in _links)
        {
            var _class = l.GetAttributeValue("class", null);

            if (_class == "next_page")
            {
                var _href = l.GetAttributeValue("href", null);

                return Goodreads + _href;
            }
        }

        return null;
    }

    private List<QueryResult> ProcessSearchQueryToList(HtmlDocument searchResult)
    {
        var _result = new List<QueryResult>();

        var _tableRows = searchResult.DocumentNode.Descendants("tr")
            .Where(node => node.Attributes.Contains("itemscope"));

        foreach (var _tr in _tableRows)
        {
            var _authorNodes = _tr.Descendants("a").Where(div => div.GetAttributeValue("class", null) == "authorName");
            var _titleNode = _tr.Descendants("a").Where(a => a.GetAttributeValue("class", null) == "bookTitle");

            var _authors = new List<string>();
            foreach (var a in _authorNodes) _authors.Add(a.InnerText);

            var _href = Goodreads + _titleNode.First().GetAttributeValue("href", null);

            var _title = _titleNode.First().InnerText.Trim();

            _result.Add(new QueryResult(_title, _authors, _href));
        }

        return _result;
    }

    private int ComputeLevenshteinDistance(string s1, string s2)
    {
        if (s1 == null || s2 == null) return 0;

        if (s1.Length == 0 || s2.Length == 0) return 0;

        if (s1 == s2) return s1.Length;

        var sourceWordCount = s1.Length;
        var targetWordCount = s2.Length;

        // Step 1
        if (sourceWordCount == 0) return targetWordCount;

        if (targetWordCount == 0) return sourceWordCount;

        var distance = new int[sourceWordCount + 1, targetWordCount + 1];

        // Step 2
        for (var i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;

        for (var j = 0; j <= targetWordCount; distance[0, j] = j++) ;

        for (var i = 1; i <= sourceWordCount; i++)
        for (var j = 1; j <= targetWordCount; j++)
        {
            // Step 3
            var cost = s2[j - 1] == s1[i - 1] ? 0 : 1;

            // Step 4
            distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                distance[i - 1, j - 1] + cost);
        }

        return distance[sourceWordCount, targetWordCount];
    }

    private double CalculateStringSimilarity(string s1, string s2)
    {
        if (s1 == null || s2 == null) return 0.0;

        if (s1.Length == 0 || s2.Length == 0) return 0.0;

        if (s1 == s2) return 1.0;

        var stepsToSame = ComputeLevenshteinDistance(s1, s2);
        return 1.0 - stepsToSame / (double)Math.Max(s1.Length, s2.Length);
    }

    public int LongestCommonSubstringLength(string a, string b)
    {
        var lengths = new int[a.Length, b.Length];
        var greatestLength = 0;
        var output = "";
        for (var i = 0; i < a.Length; i++)
        for (var j = 0; j < b.Length; j++)
            if (a[i] == b[j])
            {
                lengths[i, j] = i == 0 || j == 0 ? 1 : lengths[i - 1, j - 1] + 1;
                if (lengths[i, j] > greatestLength)
                {
                    greatestLength = lengths[i, j];
                    output = a.Substring(i - greatestLength + 1, greatestLength);
                }
            }
            else
            {
                lengths[i, j] = 0;
            }

        return output.Length;
    }

    public string FormatOutputLine(string s)
    {
        var len = s.Length;

        if (len > 30) return s.Substring(0, 30);

        return s.PadRight(30);
    }
}