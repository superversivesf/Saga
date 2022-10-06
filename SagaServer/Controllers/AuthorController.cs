using System.Collections.Generic;
using System.Linq;
using SagaServer.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;
using SagaUtil;

namespace SagaUtil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthorController : ControllerBase
    {
        private BookCommands _bookCommands;

        public AuthorController()
        {
            this._bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
        }

        public List<AuthorDto> Get()
        {
            var _authors = _bookCommands.GetAuthors();
            var _result = new List<AuthorDto>();

            foreach (var author in _authors)
            {
                var _authorDto = new AuthorDto();
                _authorDto.Name = author.AuthorName;
                _authorDto.Id = author.AuthorId;
                _result.Add(_authorDto);
            }
            return _result;
        }

        // GET: api/author/5
        [HttpGet("{id}", Name = "GetAuthor")]
        public AuthorDto Get(string id)
        {
            var _author = _bookCommands.GetAuthor(id);
            var _authorDto = new AuthorDto();
            _authorDto.Name = _author.AuthorName;
            _authorDto.Id = _author.AuthorId;
            return _authorDto;
        }

        // GET: api/author/5/details
        [HttpGet("{id}/Details", Name = "GetAuthorDetails")]
        public AuthorDetailsDto GetDetails(string id)
        {
            var _authorDetailsDto = new AuthorDetailsDto();

            var _author = this._bookCommands.GetAuthor(id);
            var _books = this._bookCommands.GetBooksByAuthorId(id);

            _authorDetailsDto.Name = _author.AuthorName;
            _authorDetailsDto.Id = _author.AuthorId;
            _authorDetailsDto.HtmlDescription = _author.AuthorDescription;
            _authorDetailsDto.TextDescription = HtmlHelper.HtmlToPlainText(_author.AuthorDescription);
            _authorDetailsDto.ImageId = _author.AuthorId;
            _authorDetailsDto.WebsiteLink = _author.AuthorWebsite;
            _authorDetailsDto.Born = _author.Born;
            _authorDetailsDto.Died = _author.Died;
            _authorDetailsDto.Genre = _author.Genre;
            _authorDetailsDto.Influences = _author.Influences;
            _authorDetailsDto.Twitter = _author.Twitter;
            _authorDetailsDto.BookLinks = _books.Select(b => new BookLinkDto(){BookTitle = b.GoodReadsTitle != null? b.GoodReadsTitle : b.BookTitle, BookId = b.BookId, }).ToList();

            return _authorDetailsDto;
        }
    }
}
