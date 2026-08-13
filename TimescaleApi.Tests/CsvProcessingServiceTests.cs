using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TimescaleApi.Data;
using TimescaleApi.Entities;
using TimescaleApi.Exceptions;
using TimescaleApi.Services;

namespace TimescaleApi.Tests;

public class CsvProcessingServiceTests
{
    [Fact]
    public async Task ProcessAsync_WithValidCsv_SavesValuesAndReturnsResult()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "valid.csv",
            """
            Date;ExecutionTime;Value
            2024-01-01T00:00:00Z;10;100
            2024-01-01T00:01:30Z;20;200
            """);

        var result = await ProcessAsync(database, file);

        await using var context = database.CreateContext();
        var savedValuesCount = await context.Values.CountAsync();
        var savedResultsCount = await context.Results.CountAsync();

        Assert.Equal("valid.csv", result.FileName);
        Assert.Equal(90, result.TimeDeltaSeconds);
        Assert.Equal(2, savedValuesCount);
        Assert.Equal(1, savedResultsCount);
    }

    [Fact]
    public async Task ProcessAsync_WithNegativeValue_ThrowsCsvValidationException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "negative-value.csv",
            """
            Date;ExecutionTime;Value
            2024-01-01T00:00:00Z;10;-1
            """);

        await Assert.ThrowsAsync<CsvValidationException>(
            () => ProcessAsync(database, file));
    }

    [Fact]
    public async Task ProcessAsync_WithNegativeExecutionTime_ThrowsCsvValidationException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "negative-execution-time.csv",
            """
            Date;ExecutionTime;Value
            2024-01-01T00:00:00Z;-1;10
            """);

        await Assert.ThrowsAsync<CsvValidationException>(
            () => ProcessAsync(database, file));
    }

    [Fact]
    public async Task ProcessAsync_WithDateBeforeAllowedMinimum_ThrowsCsvValidationException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "old-date.csv",
            """
            Date;ExecutionTime;Value
            1999-12-31T23:59:59Z;10;10
            """);

        await Assert.ThrowsAsync<CsvValidationException>(
            () => ProcessAsync(database, file));
    }

    [Fact]
    public async Task ProcessAsync_WithFutureDate_ThrowsCsvValidationException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var futureDate = DateTimeOffset.UtcNow
            .AddDays(1)
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var file = CreateCsvFile(
            "future-date.csv",
            $"""
             Date;ExecutionTime;Value
             {futureDate};10;10
             """);

        await Assert.ThrowsAsync<CsvValidationException>(
            () => ProcessAsync(database, file));
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyCsvValue_ThrowsCsvValidationException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "empty-value.csv",
            """
            Date;ExecutionTime;Value
            2024-01-01T00:00:00Z;10;
            """);

        await Assert.ThrowsAsync<CsvValidationException>(
            () => ProcessAsync(database, file));
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidNumberType_ThrowsCsvValidationException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "invalid-number.csv",
            """
            Date;ExecutionTime;Value
            2024-01-01T00:00:00Z;abc;10
            """);

        await Assert.ThrowsAsync<CsvValidationException>(
            () => ProcessAsync(database, file));
    }

    [Fact]
    public async Task ProcessAsync_CalculatesMedianValue()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "median.csv",
            """
            Date;ExecutionTime;Value
            2024-01-01T00:00:00Z;10;10
            2024-01-01T00:01:00Z;10;40
            2024-01-01T00:02:00Z;10;20
            2024-01-01T00:03:00Z;10;30
            """);

        var result = await ProcessAsync(database, file);

        Assert.Equal(25, result.MedianValue, precision: 5);
    }

    [Fact]
    public async Task ProcessAsync_CalculatesAverageValue()
    {
        await using var database = await TestDatabase.CreateAsync();
        var file = CreateCsvFile(
            "average.csv",
            """
            Date;ExecutionTime;Value
            2024-01-01T00:00:00Z;10;10
            2024-01-01T00:01:00Z;20;20
            2024-01-01T00:02:00Z;30;30
            """);

        var result = await ProcessAsync(database, file);

        Assert.Equal(20, result.AverageValue, precision: 5);
    }

    [Fact]
    public async Task ProcessAsync_WithExistingFileName_ReplacesOldValuesAndResult()
    {
        await using var database = await TestDatabase.CreateAsync();

        await ProcessAsync(
            database,
            CreateCsvFile(
                "same-name.csv",
                """
                Date;ExecutionTime;Value
                2024-01-01T00:00:00Z;10;10
                2024-01-01T00:01:00Z;20;20
                2024-01-01T00:02:00Z;30;30
                """));

        var updatedResult = await ProcessAsync(
            database,
            CreateCsvFile(
                "same-name.csv",
                """
                Date;ExecutionTime;Value
                2024-02-01T00:00:00Z;100;100
                2024-02-01T00:01:00Z;200;200
                """));

        await using var context = database.CreateContext();
        var values = await context.Values
            .OrderBy(x => x.Value)
            .ToListAsync();
        var resultsCount = await context.Results.CountAsync();

        Assert.Equal(150, updatedResult.AverageValue, precision: 5);
        Assert.Equal(2, values.Count);
        Assert.Equal(1, resultsCount);
        Assert.Equal(new[] { 100.0, 200.0 }, values.Select(x => x.Value));
    }

    private static async Task<ProcessingResult> ProcessAsync(
        TestDatabase database,
        IFormFile file)
    {
        await using var context = database.CreateContext();
        var service = new CsvProcessingService(context);

        return await service.ProcessAsync(file);
    }

    private static FormFile CreateCsvFile(
        string fileName,
        string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        private TestDatabase(SqliteConnection connection)
        {
            _connection = connection;
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var database = new TestDatabase(connection);
            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();

            return database;
        }

        public AppDbContext CreateContext()
        {
            return new AppDbContext(_options);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
