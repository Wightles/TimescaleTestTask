using Microsoft.AspNetCore.Mvc;
using TimescaleApi.Exceptions;
using TimescaleApi.Services;

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
            return BadRequest(new
            {
                error = "Файл пуст."
            });
        }

        try
        {
            var result =
                await _csvProcessingService.ProcessAsync(file);

            return Ok(new
            {
                message = "CSV успешно обработан.",
                result
            });
        }
        catch (CsvValidationException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }
}