using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;
using SagaDb.Models;
using SagaServer.Dto;

namespace SagaUtil.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BookController : ControllerBase
{
    private readonly BookCommands _bookCommands;

    public BookController()
    {
        _bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
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
            _bookDto.Title = book.LookupTitle != null ? book.LookupTitle : book.BookTitle;
            _bookDto.BookId = book.BookId;
            if (string.IsNullOrEmpty(book.LookupDescription))
            {
                _bookDto.ShortDesc = "...";
            }
            else
            {
                if (book.LookupDescription.Length < 100)
                    _bookDto.ShortDesc = HtmlHelper.HtmlToPlainText(book.LookupDescription);
                else
                    _bookDto.ShortDesc =
                        HtmlHelper.HtmlToPlainText(book.LookupDescription).Substring(0, 100).Trim() + " ...";
            }

            _bookDto.CoverImageId = book.BookId;
            _bookDto.Authors = _Authors != null
                ? _Authors.Where(a => a.AuthorType == AuthorType.Author || a.AuthorType == AuthorType.Editor)
                    .Select(a => a.AuthorName).ToList()
                : null;
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
        _bookDto.Title = _book.LookupTitle;
        _bookDto.BookId = _book.BookId;
        _bookDto.ShortDesc = HtmlHelper.HtmlToPlainText(_book.LookupDescription).Substring(0, 100).Trim() + " ...";
        _bookDto.CoverImageId = _book.BookId;
        _bookDto.Authors = _Authors.Where(a => a.AuthorType == AuthorType.Author || a.AuthorType == AuthorType.Editor)
            .Select(a => a.AuthorName).ToList();
        return _bookDto;
    }

    // GET: api/Book/5/details
    [HttpGet("{id}/Details", Name = "GetDetails")]
    public BookDetailsDto GetDetails(string id)
    {
        var _book = _bookCommands.GetBook(id);
        var _authors = _bookCommands.GetAuthorsByBookId(_book.BookId);
        var _series = _bookCommands.GetBookSeriesByBookId(_book.BookId);
        var _genres = _bookCommands.GetGenresByBookId(_book.BookId);
        var _files = _bookCommands.GetAudioFilesByBookId(_book.BookId);

        var _bookDetailsDto = new BookDetailsDto();

        _bookDetailsDto.Title = _book.LookupTitle;
        _bookDetailsDto.BookId = _book.BookId;

        _bookDetailsDto.Authors = _authors
            .Select(a => new AuthorLinkDto { AuthorId = a.AuthorId, AuthorName = a.AuthorName }).ToList();
        _bookDetailsDto.Series = _series
            .Select(s => new SeriesLinkDto { SeriesId = s.SeriesId, SeriesName = s.SeriesName }).ToList();
        _bookDetailsDto.Genres = _genres.Select(g => new GenreLinkDto { GenreId = g.GenreId, GenreName = g.GenreName })
            .ToList();
        _bookDetailsDto.Files = _files.Select(f => new FilesDto
            { FileId = f.AudioFileId, Duration = f.Duration, Filename = f.AudioFileName }).ToList();

        _bookDetailsDto.CoverImageId = _book.BookId;
        _bookDetailsDto.DescriptionHtml = _book.LookupDescription;
        _bookDetailsDto.DescriptionText = HtmlHelper.HtmlToPlainText(_book.LookupDescription);

        return _bookDetailsDto;
    }
}