using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SagaServer.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;

namespace SagaUtil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GenreController : ControllerBase
    {
        private BookCommands _bookCommands;

        public GenreController()
        {
            this._bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
        }

        // GET: api/Genre
        [HttpGet]
        public List<GenreDto> Get()
        {
            var _genres = this._bookCommands.GetGenres();
            return _genres.Select(g => new GenreDto() { GenreId = g.GenreId, GenreName = g.GenreName, GenreDetails = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Genre/{g.GenreId}/Details" }).ToList();
        }


        // GET: api/Genre/5
        [HttpGet("{id}", Name = "GetGenre")]
        public GenreDto GetGenre(string id)
        {
            var _genre = this._bookCommands.GetGenre(id);
            return new GenreDto()
            {
                GenreId = _genre.GenreId,
                GenreName = _genre.GenreName
            };
        }

        // GET: api/Genre/5/Details
        [HttpGet("{id}/Details", Name = "GetGenreDetails")]
        public GenreDetailsDto GetGenreDetails(string id)
        {
            var _genre = this._bookCommands.GetGenre(id);
            var _genreBookList = this._bookCommands.GetBooksByGenreId(id);
            var _genreBooks = new List<BookLinkDto>();
            var _genreAuthors = new List<AuthorLinkDto>();
            foreach (var b in _genreBookList)
            {
                _genreBooks.Add(new BookLinkDto() { BookTitle = b.BookTitle, BookId = b.BookId });
                var _authors = this._bookCommands.GetAuthorsByBookId(b.BookId);
                _genreAuthors.AddRange(_authors.Select(a => new AuthorLinkDto() {AuthorId = a.AuthorId, AuthorName = a.AuthorName }));
            }

            return new GenreDetailsDto()
            {
                GenreId = _genre.GenreId,
                GenreName = _genre.GenreName,
                GenreAuthors = _genreAuthors,
                GenreBooks = _genreBooks
            };
        }
    }
}
