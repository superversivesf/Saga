using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SagaServer.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;
using SagaDb.Models;

namespace SagaUtil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookController : ControllerBase
    {
        private BookCommands _bookCommands;

        public BookController()
        {
            this._bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
        }


        // GET: api/Book
        [HttpGet]
        public List<BookDto> Get()
        {
            var _books = _bookCommands.GetBooks();
            var _result = new List<BookDto>();

            foreach (var book in _books)
            {
                var _Authors = _bookCommands.GetAuthorsByBookId(book.BookId);

                var _bookDto = new BookDto();
                _bookDto.Title = book.GoodReadsTitle != null ? book.GoodReadsTitle : book.BookTitle;
                _bookDto.BookId = book.BookId;
                if (string.IsNullOrEmpty(book.GoodReadsDescription))
                {
                    _bookDto.ShortDesc = "...";
                }
                else
                {
                    if (book.GoodReadsDescription.Length < 100)
                        _bookDto.ShortDesc = HtmlHelper.HtmlToPlainText(book.GoodReadsDescription);
                    else
                        _bookDto.ShortDesc = HtmlHelper.HtmlToPlainText(book.GoodReadsDescription).Substring(0, 100).Trim() + " ...";
                }
                _bookDto.CoverImageId = book.BookId;
                _bookDto.Authors = _Authors != null ? _Authors.Where(a => a.AuthorType == AuthorType.Author || a.AuthorType == AuthorType.Editor).Select(a => a.AuthorName).ToList() : null;
                _result.Add(_bookDto);
            }
            return _result;
        }

        // GET: api/Book/5
        [HttpGet("{id}", Name = "GetBook")]
        public BookDto Get(string id)
        {
            var _book = _bookCommands.GetBook(id);
            var _Authors = _bookCommands.GetAuthorsByBookId(_book.BookId);

            var _bookDto = new BookDto();
            _bookDto.Title = _book.GoodReadsTitle;
            _bookDto.BookId = _book.BookId;
            _bookDto.ShortDesc = HtmlHelper.HtmlToPlainText(_book.GoodReadsDescription).Substring(0, 100).Trim() + " ...";
            _bookDto.CoverImageId = _book.BookId;
            _bookDto.Authors = _Authors.Where(a => a.AuthorType == AuthorType.Author || a.AuthorType == AuthorType.Editor).Select(a => a.AuthorName).ToList();
            return _bookDto;
        }

        // GET: api/Book/5/details
        [HttpGet("{id}/Details", Name = "GetDetails")]
        public BookDetailsDto GetDetails(string id)
        {
            var _book = this._bookCommands.GetBook(id);
            var _authors = this._bookCommands.GetAuthorsByBookId(_book.BookId);
            var _series = this._bookCommands.GetBookSeriesByBookId(_book.BookId);
            var _genres = this._bookCommands.GetGenresByBookId(_book.BookId);
            var _files = this._bookCommands.GetAudioFilesByBookId(_book.BookId);

            var _bookDetailsDto = new BookDetailsDto();

            _bookDetailsDto.Title = _book.GoodReadsTitle;
            _bookDetailsDto.BookId = _book.BookId;

            _bookDetailsDto.Authors = _authors.Select(a => new AuthorLinkDto() { AuthorId = a.AuthorId, AuthorName = a.AuthorName }).ToList();
            _bookDetailsDto.Series = _series.Select(s => new SeriesLinkDto() { SeriesId = s.SeriesId, SeriesName = s.SeriesName }).ToList();
            _bookDetailsDto.Genres = _genres.Select(g => new GenreLinkDto() { GenreId = g.GenreId, GenreName = g.GenreName }).ToList();
            _bookDetailsDto.Files = _files.Select(f => new FilesDto() { FileId = f.AudioFileId, Duration = f.Duration, Filename = f.AudioFileName }).ToList();

            _bookDetailsDto.CoverImageId = _book.BookId; 
            _bookDetailsDto.DescriptionHtml = _book.GoodReadsDescription;
            _bookDetailsDto.DescriptionText = HtmlHelper.HtmlToPlainText(_book.GoodReadsDescription);

            return _bookDetailsDto;
        }
    }
}
