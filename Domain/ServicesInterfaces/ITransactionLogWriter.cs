namespace MyVocaList.Domain.ServicesInterfaces;

public record LogEntry(
    DateTime Ts,
    string Op,
    string Entity,
    string Id,
    string? Before,
    string? After);

/// <summary>Appends write operations to the current session transaction log file.</summary>
public interface ITransactionLogWriter
{
    /// <summary>Appends a single log entry to the current session log file.</summary>
    Task AppendAsync(LogEntry entry, CancellationToken ct);

    /// <summary>
    /// Deletes session log files whose last-entry timestamp is entirely before snapshotTs.
    /// Log files that straddle the boundary are kept.
    /// </summary>
    Task PruneLogsOlderThanAsync(DateTime snapshotTs, CancellationToken ct);

    /// <summary>Returns the path of the current session log file.</summary>
    string CurrentSessionLogPath { get; }
}
