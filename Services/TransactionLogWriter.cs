using MyVocaList.Domain.ServicesInterfaces;
using System.Text.Json;

namespace MyVocaList.Services;

/// <inheritdoc />
public class TransactionLogWriter : ITransactionLogWriter
{
    private readonly string _logDirectory;
    private readonly string _sessionFileName;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public TransactionLogWriter(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
        _sessionFileName = $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jsonl";
    }

    /// <inheritdoc />
    public string CurrentSessionLogPath => Path.Combine(_logDirectory, _sessionFileName);

    /// <inheritdoc />
    public async Task AppendAsync(LogEntry entry, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new
        {
            ts = entry.Ts.ToString("o"),
            op = entry.Op,
            entity = entry.Entity,
            id = entry.Id,
            before = entry.Before,
            after = entry.After
        });

        await _lock.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(CurrentSessionLogPath, json + Environment.NewLine, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task PruneLogsOlderThanAsync(DateTime snapshotTs, CancellationToken ct)
    {
        if (!Directory.Exists(_logDirectory))
            return;

        foreach (var file in Directory.GetFiles(_logDirectory, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            var lastLine = await ReadLastLineAsync(file, ct);
            if (lastLine is null)
            {
                File.Delete(file);
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(lastLine);
                if (doc.RootElement.TryGetProperty("ts", out var tsProp) &&
                    DateTime.TryParse(tsProp.GetString(), out var lastTs) &&
                    lastTs < snapshotTs)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Corrupt log file — delete it
                File.Delete(file);
            }
        }
    }

    private static async Task<string?> ReadLastLineAsync(string path, CancellationToken ct)
    {
        string? last = null;
        using var reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (!string.IsNullOrWhiteSpace(line))
                last = line;
        }
        return last;
    }
}
