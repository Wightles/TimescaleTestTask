namespace TimescaleApi.DTOs;

public class ResultFilterDto
{
    public string? FileName { get; set; }

    public DateTimeOffset? StartDateFrom { get; set; }

    public DateTimeOffset? StartDateTo { get; set; }

    public double? AverageValueFrom { get; set; }

    public double? AverageValueTo { get; set; }

    public double? AverageExecutionTimeFrom { get; set; }

    public double? AverageExecutionTimeTo { get; set; }
}