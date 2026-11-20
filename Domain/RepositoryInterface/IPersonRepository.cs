using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

/// <summary>Repository interface for Person entity operations.</summary>
public interface IPersonRepository : IBaseRepository<Person>
{
    /// <summary>Returns the first person whose normalized name exactly matches (case-insensitive).</summary>
    Task<Person> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default);

    /// <summary>Prefix search on normalized name. Used by autocomplete suggestion list.</summary>
    Task<List<Person>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>Search by name prefix OR email contains. Used by the list page search bar.</summary>
    Task<List<Person>> SearchByNameOrEmailAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>Paged query for the list page. Searches name OR email when query is non-null.</summary>
    Task<(IEnumerable<Person> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default);

    /// <summary>Search by any word in name (not implemented in v1).</summary>
    Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>Returns true if any person (other than excludeId) has this email.</summary>
    Task<bool> IsEmailTakenAsync(string email, int? excludePersonId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// BUG-088 identity: true if another person (not excludeId) has the same FullName, Email and
    /// birthday day/month. Default collation, no C# normalization; null email and null day/month count as empty.
    /// </summary>
    Task<bool> IdentityExistsAsync(string fullName, string? email, int? birthDay, int? birthMonth, int? excludePersonId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// BUG-088 soft warning: persons with the same FullName (default collation) whose identity differs
    /// (Email or birthday). Excludes excludeId and any exact identity match.
    /// </summary>
    Task<IReadOnlyList<Person>> GetSameNameDifferentIdentityAsync(string fullName, string? email, int? birthDay, int? birthMonth, int? excludePersonId = null, CancellationToken cancellationToken = default);
}
