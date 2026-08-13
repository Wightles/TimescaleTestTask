using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TimescaleApi.Data;
using TimescaleApi.Entities;
using TimescaleApi.Exceptions;

namespace TimescaleApi.Services;

public class CsvProcessingService : ICsvProcessingService
{
    private const int MaxRows = 10_000;

    private static readonly DateTimeOffset MinAllowedDate =
        new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _db;

    public CsvProcessingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProcessingResult> ProcessAsync(IFormFile file)
    {
        var fileName = Path.GetFileName(file.FileName);

        if (!string.Equals(
                Path.GetExtension(fileName),
                ".csv",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new CsvValidationException(
                "Допускаются только CSV-файлы.");
        }

        if (fileName.Length > 255)
        {
            throw new CsvValidationException(
                "Имя файла слишком длинное.");
        }

        using var reader = new StreamReader(file.OpenReadStream());

        var header = await reader.ReadLineAsync();

        if (header is null)
        {
            throw new CsvValidationException("Файл пуст.");
        }

        if (header.Trim() != "Date;ExecutionTime;Value")
        {
            throw new CsvValidationException(
                "Некорректный заголовок CSV. Ожидается: Date;ExecutionTime;Value.");
        }

        var values = new List<MeasurementValue>();

        string? line;
        var lineNumber = 1;

        var now = DateTimeOffset.UtcNow;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            lineNumber++;

            if (values.Count >= MaxRows)
            {
                throw new CsvValidationException(
                    $"Количество записей не может превышать {MaxRows}.");
            }

            var columns = line.Split(';');

            if (columns.Length != 3)
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: ожидается ровно 3 значения.");
            }

            var dateText = columns[0].Trim();
            var executionTimeText = columns[1].Trim();
            var valueText = columns[2].Trim();

            if (string.IsNullOrWhiteSpace(dateText) ||
                string.IsNullOrWhiteSpace(executionTimeText) ||
                string.IsNullOrWhiteSpace(valueText))
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: все значения обязательны.");
            }

            if (!dateText.EndsWith('Z') ||
                !DateTimeOffset.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal |
                    DateTimeStyles.AdjustToUniversal,
                    out var date))
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: некорректный формат Date.");
            }

            if (date < MinAllowedDate)
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: Date не может быть раньше 01.01.2000.");
            }

            if (date > now)
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: Date не может быть позже текущего времени.");
            }

            if (!double.TryParse(
                    executionTimeText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var executionTime) ||
                !double.IsFinite(executionTime))
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: ExecutionTime должен быть числом.");
            }

            if (executionTime < 0)
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: ExecutionTime не может быть меньше 0.");
            }

            if (!double.TryParse(
                    valueText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value) ||
                !double.IsFinite(value))
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: Value должен быть числом.");
            }

            if (value < 0)
            {
                throw new CsvValidationException(
                    $"Строка {lineNumber}: Value не может быть меньше 0.");
            }

            values.Add(new MeasurementValue
            {
                FileName = fileName,
                Date = date,
                ExecutionTime = executionTime,
                Value = value
            });
        }

        if (values.Count == 0)
        {
            throw new CsvValidationException(
                "CSV должен содержать минимум одну запись.");
        }

        var minDate = values.Min(x => x.Date);
        var maxDate = values.Max(x => x.Date);

        var result = new ProcessingResult
        {
            FileName = fileName,

            TimeDeltaSeconds =
                (maxDate - minDate).TotalSeconds,

            FirstOperationDate =
                minDate,

            AverageExecutionTime =
                values.Average(x => x.ExecutionTime),

            AverageValue =
                values.Average(x => x.Value),

            MedianValue =
                CalculateMedian(values.Select(x => x.Value)),

            MaxValue =
                values.Max(x => x.Value),

            MinValue =
                values.Min(x => x.Value)
        };

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            await _db.Values
                .Where(x => x.FileName == fileName)
                .ExecuteDeleteAsync();

            await _db.Results
                .Where(x => x.FileName == fileName)
                .ExecuteDeleteAsync();

            await _db.Values.AddRangeAsync(values);

            await _db.Results.AddAsync(result);

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }

        return result;
    }

    private static double CalculateMedian(IEnumerable<double> source)
    {
        var numbers = source
            .OrderBy(x => x)
            .ToArray();

        var middle = numbers.Length / 2;

        if (numbers.Length % 2 == 0)
        {
            return (numbers[middle - 1] + numbers[middle]) / 2.0;
        }

        return numbers[middle];
    }
}