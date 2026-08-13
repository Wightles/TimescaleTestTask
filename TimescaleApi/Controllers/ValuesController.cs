using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimescaleApi.Data;
using TimescaleApi.Entities;

namespace TimescaleApi.Controllers;

[ApiController]
[Route("api/values")]
public class ValuesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ValuesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<List<MeasurementValue>>> GetLatest(
        [FromQuery] string fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new
            {
                error = "Необходимо указать имя файла."
            });
        }

        var values = await _db.Values
            .AsNoTracking()
            .Where(x => x.FileName == fileName)
            .OrderByDescending(x => x.Date)
            .Take(10)
            .ToListAsync(cancellationToken);

        return Ok(values);
    }
}
