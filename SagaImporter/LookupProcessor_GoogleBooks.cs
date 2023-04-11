using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SagaDb;
using SagaDb.Database;

namespace SagaImporter
{
    internal class LookupProcessor_GoogleBooks
    {
        //https://developers.google.com/books/docs/v1/reference/volumes#resource
        
        private BookCommands _bookCommands;
        private bool _retry;
        private string _hintfile;
        private bool _authors;
        private bool _images;
        private bool _books;
        private bool _series;
        private bool _purge;
        private const string GOOGLEBOOKS = "https://www.googleapis.com/books/v1/volumes?q=inauthor:Larry%20Correia";
        private readonly Random _random;
        private readonly KeyMaker _keyMaker;

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
            
        }
    }
}
