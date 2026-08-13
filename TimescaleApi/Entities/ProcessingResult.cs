namespace TimescaleApi.Entities;

public class ProcessingResult
{
    public long Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public double TimeDeltaSeconds { get; set; }

    public DateTimeOffset FirstOperationDate { get; set; }

    public double AverageExecutionTime { get; set; }

    public double AverageValue { get; set; }

    public double MedianValue { get; set; }

    public double MaxValue { get; set; }

    public double MinValue { get; set; }
}