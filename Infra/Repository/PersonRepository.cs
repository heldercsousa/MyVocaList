using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class PersonRepository : BaseRepository<Person>, IPersonRepository
{
    public PersonRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Person> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        return await _dbSet.FirstOrDefaultAsync(p =>
            EF.Functions.Like(
                EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                EF.Functions.Collate(fullName.Trim(), "NOCASE")),
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
                EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                EF.Functions.Collate(pattern, "NOCASE")))
            .OrderBy(p => p.FullNameNormalized)
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
                    EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                    EF.Functions.Collate(namePattern, "NOCASE"))
                ||
                (p.Email != null && EF.Functions.Like(
                    EF.Functions.Collate(p.Email, "NOCASE"),
                    EF.Functions.Collate(emailPattern, "NOCASE"))))
            .OrderBy(p => p.FullNameNormalized)
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
                    EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                    EF.Functions.Collate(namePattern, "NOCASE"))
                ||
                (p.Email != null && EF.Functions.Like(
                    EF.Functions.Collate(p.Email, "NOCASE"),
                    EF.Functions.Collate(emailPattern, "NOCASE"))));
        }

        var totalCount = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(p => p.FullNameNormalized)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10)
        => throw new NotImplementedException("Full-text word search is not implemented in v1.");

    /// <inheritdoc />
    public async Task<bool> IsEmailTakenAsync(string email, int? excludePersonId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbSet.AnyAsync(p =>
            p.Email != null &&
            p.Email.ToLower() == normalizedEmail &&
            (excludePersonId == null || p.Id != excludePersonId.Value),
            cancellationToken);
    }
}
