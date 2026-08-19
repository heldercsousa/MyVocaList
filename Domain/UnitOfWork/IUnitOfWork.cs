namespace MyVocaList.Domain.UnitOfWork;

/// <summary>Single primitive expressing "one short-lived <c>AppDbContext</c> per unit of work"
/// (`design.md § 6`, Candidate C). A service method wraps its body in
/// <c>_uow.ExecuteAsync(async sp => { ... })</c> and resolves every repository/nested service it
/// needs from the lambda's own <see cref="IServiceProvider"/> — never from a constructor-injected
/// field — so each call gets its own DI scope and its own <c>AppDbContext</c> instance, closing the
/// app-lifetime-context tracking conflict (BUG-068/BUG-071).
/// <para><b>Ratified shape (D13):</b> this four-member surface was confirmed final by Helder on
/// 2026-08-18 at Phase 3's VERIFY gate, after the pilot's real call sites (Phase 2) exercised it.</para></summary>
public interface IUnitOfWork
{
    /// <summary>Runs <paramref name="body"/> inside a fresh (or ambiently-joined) DI scope owning one
    /// <c>AppDbContext</c>, resolving whatever repositories/services the body needs from the supplied
    /// <see cref="IServiceProvider"/>. Disposes the scope on exit.
    /// <para><b>Save-skip:</b> after <paramref name="body"/> returns without throwing,
    /// <c>ExecuteAsync</c> inspects <typeparamref name="TResult"/> for this codebase's failure-tuple
    /// convention (<c>code-style-reference.md § Service Return Patterns</c>) and saves only when it
    /// signals success. Service code is unchanged — it keeps returning its ordinary tuple; the save is
    /// skipped automatically when the tuple's leading <c>bool</c> is <c>false</c>.
    /// <b>Fail-closed:</b> a <typeparamref name="TResult"/> that carries no recognised success signal
    /// is refused, not guessed — this overload throws <see cref="InvalidOperationException"/> naming
    /// the two valid fixes: implement <see cref="IUnitOfWorkOutcome"/>, or use the no-signal overload
    /// below if the method has no failure mode.</para></summary>
    Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>No-signal form. Use for service methods that return bare <see cref="Task"/> — the only
    /// IN-SCOPE example is <c>SongKaraokeUrlService.RecordPlayAsync</c> — which have no failure tuple
    /// to inspect. <b>Always saves</b> when <paramref name="body"/> returns without throwing; this is
    /// the safe default for a method with no success/failure signal at all.</summary>
    Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default);

    /// <summary>Same as <see cref="ExecuteAsync{TResult}"/> but NEVER saves. Use for read-only service
    /// methods so the method name itself carries the intent — a reviewer reading the call site does
    /// not need to open the body to know whether it writes.</summary>
    Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>Persists the pending changes of the CURRENT unit of work <b>without committing its
    /// transaction</b> (REQ-UOW-35). Use it when a body needs a database-generated value — typically
    /// an identity key — materialised <b>before</b> the body returns; the canonical case is
    /// <c>ArtistResolutionService.CommitAsync</c>'s CreateNew branch, which returns <c>created.Id</c>
    /// from inside the lambda. This is REQ-UOW-09's explicitly sanctioned "two saves inside one unit
    /// of work" branch: two saves, one <c>AppDbContext</c>, one transaction, one atomic outcome.
    /// <para><b>Not a commit:</b> the explicit transaction opened by
    /// <see cref="ExecuteAsync{TResult}(Func{IServiceProvider, Task{TResult}}, CancellationToken)"/>
    /// stays open, so a later failure tuple or a later exception still rolls the flushed rows back.
    /// <b>Fail-closed:</b> calling this outside a unit of work (no ambient scope) throws
    /// <see cref="InvalidOperationException"/> rather than silently doing nothing — there is no
    /// context to persist to, and a silent no-op would let the caller believe otherwise.</para></summary>
    Task FlushAsync(CancellationToken ct = default);
}
