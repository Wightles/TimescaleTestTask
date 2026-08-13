namespace TimescaleApi.Services;

public interface ICsvProcessingService
{
    Task ProcessAsync(IFormFile file);
}