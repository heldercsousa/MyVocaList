namespace MyVocaList.Domain.UnitOfWork;

/// <summary>Opt-in marker for named result records/types that carry a success signal but are not a
/// ValueTuple. Implement this instead of relying on structural tuple detection when a mutating
/// service method's natural return type is a named type, e.g.
/// <c>public record BackupResult(bool Success, string Message, ...) : IUnitOfWorkOutcome;</c>
/// <b>Fail-closed:</b> a named result type passed to the value-returning
/// <see cref="IUnitOfWork.ExecuteAsync{TResult}"/> that does NOT implement this interface is not
/// assumed successful — it throws <see cref="InvalidOperationException"/> instead. Implementing this
/// interface is therefore mandatory, not optional, for any named result type used with that
/// overload.</summary>
public interface IUnitOfWorkOutcome
{
    bool Success { get; }
}
