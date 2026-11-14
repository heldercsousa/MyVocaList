using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Rules;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.Repository
{
    /// <summary>
    /// Repository implementation for Venue entity operations
    /// </summary>
    public class VenueRepository : BaseRepository<Venue>, IVenueRepository
    {
        public VenueRepository(AppDbContext context) : base(context) { }

        /// <summary>
        /// Gets venue by exact name match.
        /// Trimming handled automatically by DatabaseLoadingInterceptor
        /// </summary>
        public async Task<Venue?> GetByNameAsync(string name) => await (string.IsNullOrWhiteSpace(name) ?
            Task.FromResult<Venue?>(null) : _context.Venues.FirstOrDefaultAsync(v => v.Name == name));

        /// <summary>
        /// Searches venues by name starting with the search term (case and accent insensitive).
        /// SQLite-specific: LIKE ignores column collation, so we must explicitly COLLATE both operands.
        /// When migrating to SQL Server: replace with simple StartsWith() which respects column collation.
        /// Trimming handled automatically by DatabaseLoadingInterceptor
        /// </summary>
        public async Task<IEnumerable<Venue>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Venue>();

            var searchPattern = searchTerm + "%";

            // SQLite workaround: LIKE ignores collation, so we explicitly COLLATE both sides.
            // Pattern is pre-computed so EF.Functions.Collate applies to the full right-hand
            // expression, generating: col COLLATE X LIKE @pattern COLLATE X
            return await _context.Venues
                .Where(v => EF.Functions.Like(
                    EF.Functions.Collate(v.Name, CollationConstants.Default),
                    EF.Functions.Collate(searchPattern, CollationConstants.Default)))
                .Take(maxResults)
                .OrderBy(v => v.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Searches venues by name containing the search term (case and accent insensitive).
        /// SQLite-specific: LIKE ignores column collation, so we must explicitly COLLATE both operands.
        /// When migrating to SQL Server: replace with simple Contains() which respects column collation.
        /// Trimming handled automatically by DatabaseLoadingInterceptor
        /// </summary>
        public async Task<IEnumerable<Venue>> SearchByNameContainsAsync(string searchTerm, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Venue>();

            var searchPattern = "%" + searchTerm + "%";

            // SQLite workaround: LIKE ignores collation, so we explicitly COLLATE both sides.
            // Pattern is pre-computed so EF.Functions.Collate applies to the full right-hand
            // expression, generating: col COLLATE X LIKE @pattern COLLATE X
            return await _context.Venues
                .Where(v => EF.Functions.Like(
                    EF.Functions.Collate(v.Name, CollationConstants.Default),
                    EF.Functions.Collate(searchPattern, CollationConstants.Default)))
                .Take(maxResults)
                .OrderBy(v => v.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all venues ordered by name
        /// </summary>
        public override async Task<IEnumerable<Venue>> GetAllAsync() =>
            await _context.Venues
                .OrderBy(v => v.Name)
                .ToListAsync();

        /// <summary>
        /// Searches venues with event information (case and accent insensitive).
        /// SQLite-specific: LIKE ignores column collation, so we must explicitly COLLATE both operands.
        /// When migrating to SQL Server: replace with simple Contains() which respects column collation.
        /// Trimming handled automatically by DatabaseLoadingInterceptor
        /// </summary>
        public async Task<IEnumerable<(Venue venue, int eventCount)>> SearchWithHasEventsAsync(string? query)
        {
            var q = _context.Venues.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchPattern = "%" + query + "%";

                // SQLite workaround: LIKE ignores collation, so we explicitly COLLATE both sides.
                // Pattern is pre-computed so EF.Functions.Collate applies to the full right-hand
                // expression, generating: col COLLATE X LIKE @pattern COLLATE X
                q = q.Where(v => EF.Functions.Like(
                    EF.Functions.Collate(v.Name, CollationConstants.Default),
                    EF.Functions.Collate(searchPattern, CollationConstants.Default)));
            }

            return await q
                .Select(v => new
                {
                    Venue = v,
                    EventCount = v.Events.Count()
                })
                .OrderBy(x => x.Venue.Name)
                .Select(x => ValueTuple.Create(x.Venue, x.EventCount))
                .ToListAsync();
        }

        public async Task<IEnumerable<(Venue venue, int eventCount)>> GetAllWithHasEventsAsync() =>
            await _context.Venues
                .Select(v => new
                {
                    Venue = v,
                    EventCount = v.Events.Count()
                })
                .OrderBy(x => x.Venue.Name)
                .Select(x => ValueTuple.Create(x.Venue, x.EventCount))
                .ToListAsync();

        public async Task<IEnumerable<(Venue venue, int eventCount)>> GetByIdsWithHasEventsAsync(IEnumerable<int> ids) =>
            await _context.Venues
                .Where(v => ids.Contains(v.Id))
                .Select(v => new
                {
                    Venue = v,
                    EventCount = v.Events.Count()
                })
                .Select(x => ValueTuple.Create(x.Venue, x.EventCount))
                .ToListAsync();

        /// <summary>
        /// Gets a paginated list of venues with event information flag (case and accent insensitive search).
        /// SQLite-specific: LIKE ignores column collation, so we must explicitly COLLATE both operands.
        /// When migrating to SQL Server: replace with simple Contains() which respects column collation.
        /// Trimming handled automatically by DatabaseLoadingInterceptor
        /// </summary>
        public async Task<(IEnumerable<(Venue venue, int eventCount)> items, int totalCount)> GetPagedWithEventInfoAsync(
            int pageNumber,
            int pageSize,
            string? query = null)
        {
            var q = _context.Venues.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchPattern = "%" + query + "%";

                // SQLite workaround: LIKE ignores collation, so we explicitly COLLATE both sides.
                // Pattern is pre-computed so EF.Functions.Collate applies to the full right-hand
                // expression, generating: col COLLATE X LIKE @pattern COLLATE X
                q = q.Where(v => EF.Functions.Like(
                    EF.Functions.Collate(v.Name, CollationConstants.Default),
                    EF.Functions.Collate(searchPattern, CollationConstants.Default)));
            }

            var totalCount = await q.CountAsync();

            var rawItems = await q
                .OrderBy(v => v.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    Venue = v,
                    EventCount = v.Events.Count()
                })
                .ToListAsync();

            var items = rawItems.Select(x => (x.Venue, x.EventCount)).ToList();

            return (items, totalCount);
        }

        /// <summary>
        /// Checks whether another venue has all six address fields equal. Plain == on the six columns: EF Core's
        /// null semantics make a null value match only NULL, and the column collation NOCASE_NOACCENT ignores letter
        /// case and accents, so no C# normalisation is needed. The caller passes a normalised address; an empty
        /// address never collides and returns false without a query (REQ-VEN-20, 21).
        /// </summary>
        public async Task<bool> AddressExistsAsync(VenueAddress address, int? excludeId, CancellationToken ct = default)
        {
            if (VenueAddressRules.IsEmpty(address))
                return false;

            var line1 = address.Line1;
            var line2 = address.Line2;
            var city = address.City;
            var region = address.Region;
            var postalCode = address.PostalCode;
            var country = address.Country;

            return await _context.Venues.AnyAsync(v =>
                (excludeId == null || v.Id != excludeId)
                && v.AddressLine1 == line1
                && v.AddressLine2 == line2
                && v.City == city
                && v.Region == region
                && v.PostalCode == postalCode
                && v.Country == country, ct);
        }

        /// <summary>
        /// Gets every stored local image reference of venues (REQ-VEN-04).
        /// </summary>
        public Task<List<string>> GetLocalImageReferencesAsync(CancellationToken ct = default)
            // Prefix filter on a technical value, not search or de-duplication (design § 2.3).
            => _context.Venues.AsNoTracking()
                .Where(v => v.ImageUrl != null && v.ImageUrl.StartsWith("local:"))
                .Select(v => v.ImageUrl!)
                .ToListAsync(ct);
    }
}
