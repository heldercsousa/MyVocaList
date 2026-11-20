namespace MyVocaList.Domain.Exceptions;

/// <summary>
/// A database integrity constraint (unique index or CHECK) rejected a save. Thrown by the Infra
/// unit of work so that Services can map it to a business tuple without referencing EF Core or SQLite
/// (BUG-088 REQ-DUP-06). The original provider exception is kept as <see cref="Exception.InnerException"/>.
/// </summary>
public sealed class ConstraintViolationException : Exception
{
    public ConstraintViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
