using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;
using SagaServer.Dto;

namespace SagaUtil.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GenreController : ControllerBase
{
    private readonly BookCommands _bookCommands;

    public GenreController()
    {
        _bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
    }

    // GET: api/Genre
    [HttpGet]
    public List<GenreDto> Get()
    {
        var _genres = _bookCommands.GetGenres();
        return _genres.Select(g => new GenreDto
        {
            GenreId = g.GenreId, GenreName = g.GenreName,
            GenreDetails = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Genre/{g.GenreId}/Details"
        }).ToList();
    }


    // GET: api/Genre/5
    [HttpGet("{id}", Name = "GetGenre")]
    public GenreDto GetGenre(string id)
    {
        var _genre = _bookCommands.GetGenre(id);
        return new GenreDto
        {
            GenreId = _genre.GenreId,
            GenreName = _genre.GenreName
        };
    }

    // GET: api/Genre/5/Details
    [HttpGet("{id}/Details", Name = "GetGenreDetails")]
    public GenreDetailsDto GetGenreDetails(string id)
    {
        var _genre = _bookCommands.GetGenre(id);
        var _genreBookList = _bookCommands.GetBooksByGenreId(id);
        var _genreBooks = new List<BookLinkDto>();
        var _genreAuthors = new List<AuthorLinkDto>();
        foreach (var b in _genreBookList)
        {
            _genreBooks.Add(new BookLinkDto { BookTitle = b.BookTitle, BookId = b.BookId });
            var _authors = _bookCommands.GetAuthorsByBookId(b.BookId);
            _genreAuthors.AddRange(_authors.Select(a => new AuthorLinkDto
                { AuthorId = a.AuthorId, AuthorName = a.AuthorName }));
        }

        return new GenreDetailsDto
        {
            GenreId = _genre.GenreId,
            GenreName = _genre.GenreName,
            GenreAuthors = _genreAuthors,
            GenreBooks = _genreBooks
        };
    }
}