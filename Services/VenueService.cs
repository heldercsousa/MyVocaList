using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;
using MyVocaList.Services.Mappers;

namespace MyVocaList.Services
{
    /// <summary>
    /// Service for business operations related to venues/establishments
    /// </summary>
    public class VenueService : IVenueService
    {
        private readonly IVenueRepository _venueRepository;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<VenueService> _logger;

        // Validation constants
        public int MaxInputLength => 30;  // Limit according to EF configuration
        public int ShowCounterAt => 25;   // When to show counter

        public VenueService(
            IVenueRepository venueRepository,
            IUnitOfWork uow,
            ILogger<VenueService> logger)
        {
            _venueRepository = venueRepository;
            _uow = uow;
            _logger = logger;
        }

        #region Validation

        public (bool isValid, string message) ValidateNameInput(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Venue name is required");

            name = name.Trim();

            if (name.Length > MaxInputLength)
                return (false, $"Name is too long. Maximum {MaxInputLength} characters.");

            if (name.Length < 2)
                return (false, "Name is too short. Minimum is 2 characters.");

            return (true, "");
        }

        #endregion

        #region CRUD Operations

        public async Task<(bool success, string message)> CreateVenueAsync(string name)
        {
            var validation = ValidateNameInput(name);
            if (!validation.isValid)
                return (false, validation.message);

            return await _uow.ExecuteAsync<(bool success, string message)>(async sp =>
            {
                // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
                var venueRepository = sp.GetRequiredService<IVenueRepository>();

                // Venue.Name trimming is enforced by the EF Core ValueConverter configured in
                // VenueConfiguration (design.md § D3) — not here. EF applies the same converter to
                // this WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
                var existing = await venueRepository.GetByNameAsync(name);
                if (existing != null)
                    return (false, "There is another venue registered with this name");

                var venue = new Venue { Name = name };
                await venueRepository.AddAsync(venue);
                // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).

                return (true, $"Venue '{name.Trim()}' successfully created!");
            });
        }

        public async Task<(bool success, string message)> UpdateVenueAsync(int id, string newName)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id, nameof(id));

            var validation = ValidateNameInput(newName);
            if (!validation.isValid)
                return (false, validation.message);

            return await _uow.ExecuteAsync<(bool success, string message)>(async sp =>
            {
                // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
                var venueRepository = sp.GetRequiredService<IVenueRepository>();

                var venue = await venueRepository.GetByIdAsync(id);
                if (venue == null)
                    return (false, "Venue not found");

                // Venue.Name trimming is enforced by the EF Core ValueConverter configured in
                // VenueConfiguration (design.md § D3) — not here. EF applies the same converter to
                // this WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
                var existing = await venueRepository.GetByNameAsync(newName);
                if (existing != null && existing.Id != id)
                    return (false, "There is another venue registered with this name");

                venue.Name = newName;
                // Explicit UpdateAsync: GetByIdAsync's FindAsync only returns an already-tracked
                // instance when one exists in THIS unit of work's local cache. Since each unit of
                // work gets its own freshly-scoped AppDbContext (REQ-UOW-28), a venue fetched here
                // was never previously tracked, and the DbContext-wide QueryTrackingBehavior.NoTracking
                // default means FindAsync returns it detached — mutating a detached instance is a
                // silent no-op at SaveChangesAsync time without this call (mirrors
                // ArtistService.UpdateArtistAsync / PersonService.UpdatePersonAsync). This call was
                // already present before the unit-of-work wrap.
                await venueRepository.UpdateAsync(venue);
                // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).

                return (true, $"Venue name successfully updated to '{newName.Trim()}'!");
            });
        }

        public async Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids)
        {
            ArgumentNullException.ThrowIfNull(ids, nameof(ids));

            if (!ids.Any())
                return (false, "No venue was selected for removal.");

            return await _uow.ExecuteAsync<(bool success, string message)>(async sp =>
            {
                // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
                var venueRepository = sp.GetRequiredService<IVenueRepository>();

                var venuesWithEvents = await venueRepository.GetByIdsWithHasEventsAsync(ids);
                var validationResults = new List<(int id, string name, bool canDelete, string reason)>();

                foreach (var (venue, eventCount) in venuesWithEvents)
                    validationResults.Add((venue.Id, venue.Name, eventCount == 0, eventCount > 0 ? "has registered events" : ""));

                var cannotDelete = validationResults.Where(v => !v.canDelete).ToList();
                var canDelete = validationResults.Where(v => v.canDelete).ToList();

                if (canDelete.Count != 0)
                {
                    var entitiesToDelete = venuesWithEvents
                        .Where(x => canDelete.Any(c => c.id == x.venue.Id))
                        .Select(x => x.venue);

                    await venueRepository.DeleteRangeAsync(entitiesToDelete);
                    // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
                }

                return BuildDeleteResultMessage(canDelete, cannotDelete);
            });
        }

        private (bool success, string message) BuildDeleteResultMessage(
            List<(int id, string name, bool canDelete, string reason)> canDelete,
            List<(int id, string name, bool canDelete, string reason)> cannotDelete)
        {
            if (cannotDelete.Count == 0 && canDelete.Count > 0)
            {
                var count = canDelete.Count;
                return (true, count == 1
                    ? "1 venue successfully removed!"
                    : $"{count} venues successfully removed!");
            }

            if (cannotDelete.Count > 0 && canDelete.Count > 0)
            {
                var deleted = canDelete.Count;
                var blocked = cannotDelete.Count;
                var total = deleted + blocked;
                return (true, $"{deleted} of {total} successfully {(total == 1 ? "removed venue" : "removed venues")}. " +
                             $"{blocked} {(blocked == 1 ? "venue couldn't be removed" : "venues couldn't be removed")} " +
                             $"({(blocked == 1 ? "has" : "have")} events).");
            }

            // None could be deleted (all blocked)
            var blockedCount = cannotDelete.Count;
            return (false, blockedCount == 1
                ? "The venue couldn't be removed (has events)."
                : $"The {blockedCount} venues couldn't be removed (have events).");
        }

        #endregion

        public async Task<(IEnumerable<VenueListItemDto> items, int totalCount)> GetPagedVenuesForListAsync(
            int pageNumber,
            int pageSize,
            string query = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber, nameof(pageNumber));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize, nameof(pageSize));

            query = query.NormalizeSearchQuery();

            var (items, totalCount) = await _venueRepository.GetPagedWithEventInfoAsync(pageNumber, pageSize, query);
            var dtos = items.Select(x => VenueMapper.ToListDto(x.venue, x.eventCount));

            return (dtos, totalCount);
        }

        #region Utilities

        public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

        public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
        {
            string text = $"{currentLength}/{MaxInputLength}";
            bool isWarning = currentLength > 27;
            // Error only when ValidateNameInput would reject the same length (counter threshold
            // alignment, Form Validation Standard) — exactly MaxInputLength is still valid.
            bool isError = currentLength > MaxInputLength;
            return (text, isWarning, isError);
        }

        #endregion
    }
}
