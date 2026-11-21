using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class PersonRepository : BaseRepository<Person>, IPersonRepository
{
    public PersonRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Person> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        var trimmedName = fullName.Trim();
        return await _dbSet.FirstOrDefaultAsync(p =>
            EF.Functions.Collate(p.FullName, CollationConstants.Default) == EF.Functions.Collate(trimmedName, CollationConstants.Default),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Person>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return [];

        var pattern = searchTerm.Trim() + "%";
        return await _dbSet
            .Where(p => EF.Functions.Like(
                EF.Functions.Collate(p.FullName, CollationConstants.Default),
                EF.Functions.Collate(pattern, CollationConstants.Default)))
            .OrderBy(p => p.FullName)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Person>> SearchByNameOrEmailAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return [];

        var term = searchTerm.Trim();
        var namePattern = term + "%";
        var emailPattern = "%" + term + "%";

        return await _dbSet
            .Where(p =>
                EF.Functions.Like(
                    EF.Functions.Collate(p.FullName, CollationConstants.Default),
                    EF.Functions.Collate(namePattern, CollationConstants.Default))
                ||
                (p.Email != null && EF.Functions.Like(
                    EF.Functions.Collate(p.Email, CollationConstants.Email),
                    EF.Functions.Collate(emailPattern, CollationConstants.Email))))
            .OrderBy(p => p.FullName)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Person> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default)
    {
        var q = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var namePattern = term + "%";
            var emailPattern = "%" + term + "%";

            q = q.Where(p =>
                EF.Functions.Like(
                    EF.Functions.Collate(p.FullName, CollationConstants.Default),
                    EF.Functions.Collate(namePattern, CollationConstants.Default))
                ||
                (p.Email != null && EF.Functions.Like(
                    EF.Functions.Collate(p.Email, CollationConstants.Email),
                    EF.Functions.Collate(emailPattern, CollationConstants.Email))));
        }

        var totalCount = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(p => p.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Full-text word search is not implemented in v1.");

    /// <inheritdoc />
    public async Task<bool> IsEmailTakenAsync(string email, int? excludePersonId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var trimmedEmail = email.Trim();
        return await _dbSet.AnyAsync(p =>
            p.Email != null &&
            EF.Functions.Collate(p.Email, CollationConstants.Email) == EF.Functions.Collate(trimmedEmail, CollationConstants.Email) &&
            (excludePersonId == null || p.Id != excludePersonId.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IdentityExistsAsync(string fullName, string? email, int? birthDay, int? birthMonth, int? excludePersonId = null, CancellationToken cancellationToken = default)
    {
        return await IdentityMatches(fullName, email, birthDay, birthMonth)
            .Where(p => excludePersonId == null || p.Id != excludePersonId.Value)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SameNameExistsAsync(string fullName, int? excludePersonId = null, CancellationToken cancellationToken = default)
    {
        return await SameNameQuery(fullName, null, null, null, excludePersonId, includeIdentityMatch: true)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountSameNameAsync(string fullName, string? email, int? birthDay, int? birthMonth, int? excludePersonId, bool includeIdentityMatch, CancellationToken cancellationToken = default)
    {
        return await SameNameQuery(fullName, email, birthDay, birthMonth, excludePersonId, includeIdentityMatch)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Person>> GetSameNameAsync(string fullName, string? email, int? birthDay, int? birthMonth, int? excludePersonId, bool includeIdentityMatch, int take, CancellationToken cancellationToken = default)
    {
        return await SameNameQuery(fullName, email, birthDay, birthMonth, excludePersonId, includeIdentityMatch)
            .OrderBy(p => p.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// BUG-088 same-name query: persons with the same FullName (default collation), NoTracking, excluding excludePersonId.
    /// When <paramref name="includeIdentityMatch"/> is false, the exact identity match is excluded (the same predicate
    /// as <see cref="IdentityExistsAsync"/>, as a subquery).
    /// </summary>
    private IQueryable<Person> SameNameQuery(string fullName, string? email, int? birthDay, int? birthMonth, int? excludePersonId, bool includeIdentityMatch)
    {
        var name = fullName.Trim();
        var query = _dbSet
            .AsNoTracking()
            .Where(p => EF.Functions.Collate(p.FullName, CollationConstants.Default) == EF.Functions.Collate(name, CollationConstants.Default))
            .Where(p => excludePersonId == null || p.Id != excludePersonId.Value);

        if (includeIdentityMatch)
            return query;

        var identityIds = IdentityMatches(fullName, email, birthDay, birthMonth).Select(p => p.Id);
        return query.Where(p => !identityIds.Contains(p.Id));
    }

    /// <summary>
    /// BUG-088 identity predicate: FullName + Email + birthday day/month, default collation, no C# normalization.
    /// A missing email is matched as <c>Email IS NULL</c> (the trim converter stores blank as NULL, so the empty
    /// string is never a stored value). A missing day or month compares as 0, matching the COALESCE index.
    /// </summary>
    private IQueryable<Person> IdentityMatches(string fullName, string? email, int? birthDay, int? birthMonth)
    {
        var name = fullName.Trim();
        var mail = (email ?? string.Empty).Trim();
        var day = birthDay ?? 0;
        var month = birthMonth ?? 0;

        var byName = _dbSet.AsNoTracking().Where(p =>
            EF.Functions.Collate(p.FullName, CollationConstants.Default) == EF.Functions.Collate(name, CollationConstants.Default) &&
            (p.BirthDay ?? 0) == day &&
            (p.BirthMonth ?? 0) == month);

        return string.IsNullOrEmpty(mail)
            ? byName.Where(p => p.Email == null)
            : byName.Where(p => p.Email != null && EF.Functions.Collate(p.Email, CollationConstants.Default) == EF.Functions.Collate(mail, CollationConstants.Default));
    }
}
