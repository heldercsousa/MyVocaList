using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Infra.Interceptor;

/// <summary>
/// Captures before/after state of every write and appends entries to the transaction log.
/// Registered as Singleton alongside CollationInterceptor.
/// </summary>
public class TransactionLogInterceptor : SaveChangesInterceptor
{
    private readonly ITransactionLogWriter _logWriter;
    private readonly ConcurrentDictionary<int, List<LogEntry>> _pendingByContext = new();

    public TransactionLogInterceptor(ITransactionLogWriter logWriter)
    {
        _logWriter = logWriter;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e =>
            {
                var op = e.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    _ => "Delete"
                };

                var id = e.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "0";

                var before = e.State != EntityState.Added
                    ? JsonSerializer.Serialize(e.OriginalValues.Properties
                        .ToDictionary(p => p.Name, p => e.OriginalValues[p]))
                    : null;

                var after = e.State != EntityState.Deleted
                    ? JsonSerializer.Serialize(e.CurrentValues.Properties
                        .ToDictionary(p => p.Name, p => e.CurrentValues[p]))
                    : null;

                return new LogEntry(DateTime.UtcNow, op, e.Entity.GetType().Name, id, before, after);
            })
            .ToList();

        _pendingByContext[eventData.Context.GetHashCode()] = entries;
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null &&
            _pendingByContext.TryRemove(eventData.Context.GetHashCode(), out var entries))
        {
            // Skip BackupHistory writes to avoid infinite loop — logging a backup entry
            // would itself trigger another log entry, causing unbounded recursion.
            var filtered = entries.Where(e => e.Entity != nameof(Domain.Entity.BackupHistory)).ToList();
            foreach (var entry in filtered)
                await _logWriter.AppendAsync(entry, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is not null)
            _pendingByContext.TryRemove(eventData.Context.GetHashCode(), out _);

        base.SaveChangesFailed(eventData);
    }
}
