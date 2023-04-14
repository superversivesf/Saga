using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SagaDb;
using SagaDb.Database;
using SagaDb.Models;
using SagaImporter.Dto;

namespace SagaImporter
{
    internal class LookupProcessor_GoogleBooks
    {
        private const string Googlebooks = "https://www.googleapis.com/books/v1/volumes";
        private readonly KeyMaker _keyMaker;
        private readonly Random _random;
        private bool _authors;
        private BookCommands _bookCommands;
        private bool _books;
        private string _hintfile;
        private bool _images;
        private bool _purge;
        private bool _retry;
        private bool _series;

        public LookupProcessor_GoogleBooks()
        {
            _random = new Random();
            _keyMaker = new KeyMaker();
        }
        
        public bool Initialize(LookupOptions l)
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

        public void Execute()
        {
            if(_series)
                throw new Exception("Series mode not supported by Google lookup ... sorry");
            
            if (_books)
            {
                if (!String.IsNullOrEmpty(_hintfile))
                {
                    throw new Exception("Hint file not yet supported by Google lookup ... sorry");
                }
                else
                {
                    List<Book> books;
                    if (_retry)
                        books = _bookCommands.GetBooksFailedLookup();
                    else
                        books = _bookCommands.GetBooksMissingLookup();
                    Console.WriteLine($"Processing {books.Count} books");

                    foreach (var book in books)
                    {

                        books.Sort((x, y) =>
                            String.Compare(x.BookId, y.BookId, StringComparison.Ordinal)); // Not in alphabetical order

                        var authors = _bookCommands.GetAuthorsByBookId(book.BookId);

                        var title = book.BookTitle.ToLower();

                        Console.Write($"Google lookup of {title} ...");

                        GBQueryResult searchResult = SearchGoogleBooks(book, authors);
                        book.LookupFetchTried = true;
                        if (searchResult != null)
                        {
                            BookDetails entry = ProcessGoogleBookEntry(searchResult);

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
        }

        private void UpdateBook(BookDetails entry, Book book)
        {
        }

        private void UpdateAuthors(BookDetails entry, Book book)
        {
        }

        private void UpdateSeries(BookDetails entry, Book book)
        {
        }

        private void UpdateGenres(BookDetails entry, Book book)
        {
        }

        private BookDetails ProcessGoogleBookEntry(GBQueryResult searchResult)
        {
            return new BookDetails();
        }

        private GBQueryResult SearchGoogleBooks(Book book, List<Author> authors)
        {
            var results = new List<Items>();
            foreach (var a in authors)
            {
                var authorName = a.AuthorName.ToLower();
                var bookResults = DoGoogleBooksQuery(authorName);
                results.AddRange(bookResults);
            }

            var queryResult = MatchBookResults(results, book, authors);

            return queryResult;
        }

        private GBQueryResult MatchBookResults(List<Items> items, Book book, List<Author> authors)
        {
            GBQueryResult bestMatch = null;
            var bestMatchScore = 0;
            var normalBookTitle = NormalizeTitle(book.BookTitle);
            var normalAuthorStrings = authors.Select(x => NormalizeAuthor(x.AuthorName)).ToList();
            
            foreach (var i in items)
            {
                double score = CalculateMatch(i, normalBookTitle, normalAuthorStrings);

                if (score > bestMatchScore)
                {
                    var t = i.volumeInfo.title;
                    var a = i.volumeInfo.authors.ToList();
                    var l = i.selfLink;

                    bestMatch = new GBQueryResult(t, a, l);
                }
            }

            return bestMatch;
        }

        private int CalculateMatch(Items i, string book, List<string> authors)
        {
            var t = NormalizeTitle(i.volumeInfo.title);
            var a = i.volumeInfo.authors.Select(x => NormalizeAuthor(x)).ToList();
            
            // Calculate how close the book match lev distance?
            
            // Calculate how close the author matches are
            
            // score increases for each match
            
            // Then normalize to out of 100. 
            
            return 0;
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

        private List<Items> DoGoogleBooksQuery(string authorName)
        {
            var index = 0;
            var items = new List<Items>();

            while (true)
            {
                var query = $"{Googlebooks}?q=inauthor:{authorName}&startIndex={index}&maxResults=20";
                Console.WriteLine(query);
                var jsonResult = DoWebQuery(query);
                if (jsonResult != null)
                {
                    GoogleBooksDto results = JsonConvert.DeserializeObject<GoogleBooksDto>(jsonResult);

                    if (results.items != null)
                        items.AddRange(results.items);
                    else
                        break;
                    index += 20;
                }
            }

            return items;

            // Need to paginate results and get all the book results. 
            // https://www.googleapis.com/books/v1/volumes?q=harry+potter&startIndex=0&maxResults=40
            // Move the index till you get them all and stop when number of results less than
            // max results. 

            // Cover images https://books.google.com/books/content?id=2ld0CwAAQBAJ&printsec=frontcover&img=1&zoom=100

        }

        private string DoWebQuery(string query)
        {
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> taskResponse = client.GetAsync(query);
                taskResponse.Wait();
                var response = taskResponse.Result;
                if (response.IsSuccessStatusCode)
                {
                    var resultString = response.Content.ReadAsStringAsync();
                    resultString.Wait();
                    return resultString.Result;
                }
                else
                {
                    throw new Exception($"HTTP Error {response.StatusCode}");
                }
                    
            }

            return "";
        }
    }
}
