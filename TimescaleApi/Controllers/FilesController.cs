using Microsoft.AspNetCore.Mvc;

namespace TimescaleApi.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public IActionResult Upload([FromForm] IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("Файл пуст.");
        }

        return Ok(new
        {
            fileName = file.FileName,
            size = file.Length
        });
    }
}