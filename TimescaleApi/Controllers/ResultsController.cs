using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimescaleApi.Data;
using TimescaleApi.DTOs;
using TimescaleApi.Entities;

namespace TimescaleApi.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProcessingResult>>> Get(
        [FromQuery] ResultFilterDto filter)
    {
        var query = _db.Results
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.FileName))
        {
            query = query.Where(x =>
                x.FileName == filter.FileName);
        }

        if (filter.StartDateFrom.HasValue)
        {
            query = query.Where(x =>
                x.FirstOperationDate >= filter.StartDateFrom.Value);
        }

        if (filter.StartDateTo.HasValue)
        {
            query = query.Where(x =>
                x.FirstOperationDate <= filter.StartDateTo.Value);
        }

        if (filter.AverageValueFrom.HasValue)
        {
            query = query.Where(x =>
                x.AverageValue >= filter.AverageValueFrom.Value);
        }

        if (filter.AverageValueTo.HasValue)
        {
            query = query.Where(x =>
                x.AverageValue <= filter.AverageValueTo.Value);
        }

        if (filter.AverageExecutionTimeFrom.HasValue)
        {
            query = query.Where(x =>
                x.AverageExecutionTime >=
                filter.AverageExecutionTimeFrom.Value);
        }

        if (filter.AverageExecutionTimeTo.HasValue)
        {
            query = query.Where(x =>
                x.AverageExecutionTime <=
                filter.AverageExecutionTimeTo.Value);
        }

        var results = await query
            .OrderBy(x => x.FileName)
            .ToListAsync();

        return Ok(results);
    }
}