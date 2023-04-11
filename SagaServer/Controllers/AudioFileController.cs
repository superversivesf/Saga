using System.IO;
using Microsoft.AspNetCore.Mvc;
using SagaDb.Database;

namespace SagaUtil.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AudioFileController : ControllerBase
{
    private readonly BookCommands _bookCommands;

    public AudioFileController()
    {
        _bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
    }

    // GET: api/AudioFile/5
    [HttpGet("{id}", Name = "GetAudioFile")]
    public IActionResult Get(string id)
    {
        var _audioFile = _bookCommands.GetAudioFile(id);

        if (_audioFile == null)
            return NotFound();

        var _filePath = Path.Combine(_audioFile.AudioFileFolder, _audioFile.AudioFileName);
        var _fileBytes = System.IO.File.ReadAllBytes(_filePath);
        var _memoryStream = new MemoryStream(_fileBytes);

        return File(_memoryStream, "audio/mpeg", _audioFile.AudioFileName);
    }
}