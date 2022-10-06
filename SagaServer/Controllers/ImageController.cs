using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;

namespace SagaUtil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private BookCommands _bookCommands;

        public ImageController()
        {
            this._bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
        }

        // GET: api/Image/5
        [HttpGet("{id}", Name = "Get")]
        public IActionResult Get(string id)
        {
            var _imageDb = this._bookCommands.GetImage(id);

            if (_imageDb == null)
                return NotFound();

            return File(_imageDb.ImageData, "image/jpeg");
        }
    }
}
