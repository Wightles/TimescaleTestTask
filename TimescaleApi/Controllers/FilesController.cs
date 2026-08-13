using Microsoft.AspNetCore.Mvc;
using TimescaleApi.Services;
using TimescaleApi.Exceptions;

namespace TimescaleApi.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly ICsvProcessingService _csvProcessingService;

    public FilesController(ICsvProcessingService csvProcessingService)
    {
        _csvProcessingService = csvProcessingService;
    }

    [HttpPost("upload")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> Upload(IFormFile file)
{
    if (file.Length == 0)
    {
        return BadRequest("Файл пуст.");
    }

    try
    {
        await _csvProcessingService.ProcessAsync(file);
    }
    catch (CsvValidationException exception)
    {
        return BadRequest(new
        {
            error = exception.Message
        });
    }

    return Ok(new
    {
        message = "CSV успешно прошёл валидацию.",
        fileName = file.FileName
    });
}
}