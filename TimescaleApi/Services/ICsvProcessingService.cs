using TimescaleApi.Entities;

namespace TimescaleApi.Services;

public interface ICsvProcessingService
{
    Task<ProcessingResult> ProcessAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);
}
