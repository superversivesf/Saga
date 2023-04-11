using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;

namespace SagaUtil.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImageController : ControllerBase
{
    private readonly BookCommands _bookCommands;

    public ImageController()
    {
        _bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
    }

    // GET: api/Image/5
    [HttpGet("{id}", Name = "Get")]
    public IActionResult Get(string id)
    {
        var _imageDb = _bookCommands.GetImage(id);

        if (_imageDb == null)
            return NotFound();

        return File(_imageDb.ImageData, "image/jpeg");
    }
}