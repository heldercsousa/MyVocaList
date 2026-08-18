using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.UnitOfWork;

namespace MyVocaList.Infra.UnitOfWork;

/// <summary>The single implementation of <see cref="IUnitOfWork"/> — scope creation, explicit
/// transaction, <see cref="AsyncLocal{T}"/> ambient join, save-skip signal detection, fail-closed
/// throw (`design.md § 6`, Candidate C). Copied verbatim from `design.md § 6` Revision 12 — do not
/// re-derive.</summary>
public sealed class UnitOfWork(IServiceScopeFactory scopeFactory) : IUnitOfWork
{
    // AsyncLocal flag joins an already-open unit of work instead of nesting a second scope —
    // ships now per Revision 2, not deferred. Holds across the 3-level chain found in § 6a
    // (SongResolutionService.CommitAsync -> ArtistResolutionService.CommitAsync -> ArtistService.CreateArtistAsync).
    //
    // Revision 12 (§ 8, supersedes Revision 11 -- Helder's decision 2026-08-04): ONLY a write
    // publishes an ambient scope. A read never does. This closes 4th-pass finding BL-E (a write
    // nested in a read joined a scope that never saves, silently discarding the mutation) without
    // a guard, a flag, or an exception -- the write simply opens its own scope and saves.
    //   write -> read : the read JOINS the write's scope (the lookup-before-persist case; the
    //                   outer write still saves normally).
    //   read  -> write: the write opens its OWN scope and saves. No silent loss, no throw.
    private static readonly AsyncLocal<IServiceProvider?> _ambientScope = new();

    // Explicit transaction (Helder's decision 2026-08-04, replaces the REQ-UOW-33 carve-out --
    // see § 8 "Decision: ExecuteAsync opens an explicit transaction"). EF's automatic per-SaveChanges
    // transaction covers only a single SaveChangesAsync call, and ExecuteUpdateAsync/ExecuteDeleteAsync
    // do NOT implicitly start a transaction -- each runs as its own immediate SQL statement. An
    // explicit BeginTransactionAsync pulls both the ordinary tracked-write save AND any
    // ExecuteUpdateAsync/ExecuteDeleteAsync call made inside body under the same commit/rollback
    // boundary. EF automatically creates a savepoint before SaveChangesAsync when a transaction is
    // already in progress, and rolls back to it on a SaveChangesAsync failure.
    public async Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
    {
        // Only a write ever publishes an ambient scope (Revision 12), so joining one is always
        // joining another write -- it will save. The joined branch does NOT open a second
        // transaction: it participates in the outer ExecuteAsync's transaction.
        if (_ambientScope.Value is { } joined)
            return await body(joined);   // join, don't nest -- both are writes, one transaction

        await using var scope = scopeFactory.CreateAsyncScope();
        _ambientScope.Value = scope.ServiceProvider;
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await body(scope.ServiceProvider);
            // Save-skip (Revision 8, § 6b): save only when the result signals success.
            if (ResultSignalsSuccess(result))
            {
                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            else
            {
                // Failure signal: roll back -- this also undoes any ExecuteUpdateAsync/
                // ExecuteDeleteAsync call the body already ran, which the old carve-out
                // (REQ-UOW-33) said was impossible.
                await transaction.RollbackAsync(ct);
            }
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        finally { _ambientScope.Value = null; }
    }

    // No-signal overload (Revision 8, § 6b) -- always saves. For bodies with nothing to inspect
    // (bare Task): the only IN-SCOPE example is RecordPlayAsync (SongKaraokeUrlService).
    // RecordParticipationAsync/SetActiveEventAsync are EXCLUDED QueueService methods (D12) -- not
    // wrapped by this spec (corrected 2026-08-04, non-blocking #4).
    public async Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
    {
        if (_ambientScope.Value is { } joined) { await body(joined); return; }

        await using var scope = scopeFactory.CreateAsyncScope();
        _ambientScope.Value = scope.ServiceProvider;
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            await body(scope.ServiceProvider);
            await context.SaveChangesAsync(ct);   // always -- no signal to inspect
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        finally { _ambientScope.Value = null; }
    }

    public async Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
    {
        // A read JOINS an ambient write scope when there is one -- this is the common
        // lookup-before-persist case, and the outer write still saves normally.
        if (_ambientScope.Value is { } joined) return await body(joined);

        // Standalone read: open a scope but do NOT publish it as ambient (Revision 12). A read
        // never saves, so anything nested inside it must not be lured into joining it.
        await using var scope = scopeFactory.CreateAsyncScope();
        return await body(scope.ServiceProvider);
        // no SaveChangesAsync -- read-only, per Revision 6.
    }

    // Flush (REQ-UOW-35, added 2026-08-18 -- Helder's decision on Task 2.3's spec gap). Saves the
    // AMBIENT scope's context and stops there: no CommitAsync, no DisposeAsync on the transaction.
    // Because the explicit transaction is still open, the failure-signal RollbackAsync above and the
    // catch-block RollbackAsync both still undo everything this flush wrote -- atomicity unchanged.
    //
    // NOTE: this method only READS _ambientScope. It is deliberately NOT a third assignment site --
    // the two write paths remain the only publishers (Revision 12 / REQ-UOW-34).
    public async Task FlushAsync(CancellationToken ct = default)
    {
        // Fail-closed, consistent with ResultSignalsSuccess's refusal to guess (REQ-UOW-27): no
        // ambient scope means no unit of work is in progress, so there is nothing to flush and
        // no transaction to protect the flushed rows. Silently returning would let the caller
        // believe its changes were persisted.
        if (_ambientScope.Value is not { } scope)
            throw new InvalidOperationException(
                "IUnitOfWork.FlushAsync was called outside a unit of work. " +
                "A flush persists the pending changes of the CURRENT unit of work without " +
                "committing it, so it is only valid inside an ExecuteAsync body. " +
                "Note that ExecuteReadAsync does not open one: a read never saves (REQ-UOW-34).");

        var context = scope.GetRequiredService<AppDbContext>();
        await context.SaveChangesAsync(ct);
        // No transaction.CommitAsync and no transaction disposal -- see the note above.
    }

    // Save-skip signal detection (Revision 9, § 6b). Exhaustive by construction -- every branch is
    // either a recognised signal or the explicit, fail-closed refusal below (never a silent guess,
    // and never a silent commit).
    private static bool ResultSignalsSuccess<TResult>(TResult result)
    {
        // 1) This codebase's universal Service Return Pattern: (bool success, string message, ...).
        //    Every C# ValueTuple, of every arity, implements System.Runtime.CompilerServices.ITuple --
        //    this is a real structural type check, not reflection over field names.
        if (result is System.Runtime.CompilerServices.ITuple t && t.Length > 0 && t[0] is bool tupleSuccess)
            return tupleSuccess;

        // 2) Named result types opt in explicitly by implementing IUnitOfWorkOutcome
        //    (e.g. BackupResult -- appending ": IUnitOfWorkOutcome" is a blocking prerequisite of
        //    wrapping BackupService in Wave 5, § 10, not a later or optional step; see § 6b).
        if (result is IUnitOfWorkOutcome outcome)
            return outcome.Success;

        // 3) No recognised signal -> refuse to guess. Defaulting to true would commit a failed
        //    operation; defaulting to false would silently discard a successful one (REQ-UOW-27).
        throw new InvalidOperationException(
            $"{typeof(TResult).Name} carries no success signal. " +
            "Implement IUnitOfWorkOutcome, or use the " +
            "no-signal ExecuteAsync overload if this method " +
            "has no failure mode.");
    }
}
