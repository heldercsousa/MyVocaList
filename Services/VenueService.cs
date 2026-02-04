using MyVocaList.Domain;
using MyVocaList.Infra.Data.Repositories;
using MyVocaList.Infra.Utils;
using MyVocaList.Services.Mappers;
using MyVocaList.Contracts.DTOs.List;
using Microsoft.Extensions.Logging;

namespace MyVocaList.Services
{
    /// <summary>
    /// Service for business operations related to venues/establishments
    /// </summary>
    public class VenueService : IVenueService
    {
        private readonly IVenueRepository _venueRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ITextNormalizer _textNormalizer;
        private readonly ILogger<VenueService> _logger;

        // Validation constants
        public int MaxInputLength => 30;  // Limit according to EF configuration
        public int ShowCounterAt => 25;   // When to show counter

        public VenueService(
            IVenueRepository venueRepository,
            IEventRepository eventRepository,
            ITextNormalizer textNormalizer,
            ILogger<VenueService> logger)
        {
            _venueRepository = venueRepository;
            _eventRepository = eventRepository;
            _textNormalizer = textNormalizer;
            _logger = logger;
        }

        #region Validation

        public (bool isValid, string message) ValidateNameInput(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "Venue name is required");
            }

            name = name.Trim();

            if (name.Length > MaxInputLength)
            {
                return (false, $"Name is too long. Maximum {MaxInputLength} characters.");
            }

            if (name.Length < 2)
            {
                return (false, "Name is too short. Minimum is 2 characters.");
            }

            return (true, "");
        }

        #endregion

        #region CRUD Operations

        public async Task<(bool success, string message, Venue? venue)> CreateVenueAsync(string name)
        {
            // Validation
            var validation = ValidateNameInput(name);
            if (!validation.isValid)
            {
                return (false, validation.message, null);
            }

            name = name.Trim();

            // Check for duplicates
            var existing = await _venueRepository.GetByNameAsync(name);
            if (existing != null)
            {
                return (false, "There is another venue registered with this name", null);
            }

            // Create new venue
            var venue = new Venue { Name = name };

            await _venueRepository.AddAsync(venue);
            await _venueRepository.SaveChangesAsync();

            return (true, $"Venue '{name}' successfully created!", venue);
        }

        public async Task<(bool success, string message)> UpdateVenueAsync(int id, string newName)
        {
            Guard.AgainstNegativeOrZero(id, nameof(id));

            // Validation
            var validation = ValidateNameInput(newName);
            if (!validation.isValid)
            {
                return (false, validation.message);
            }

            newName = newName.Trim();

            // Find venue
            var venue = await _venueRepository.GetByIdAsync(id);
            if (venue == null)
            {
                return (false, "Venue not found");
            }

            // Check for duplicates (except itself)
            var existing = await _venueRepository.GetByNameAsync(newName);
            if (existing != null && existing.Id != id)
            {
                return (false, "There is another venue registered with this name");
            }

            // Update
            venue.Name = newName;
            await _venueRepository.UpdateAsync(venue);
            await _venueRepository.SaveChangesAsync();

            return (true, $"Venue name successfully updated to '{newName}'!");
        }

        public async Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids)
        {
            Guard.AgainstNull(ids, nameof(ids));

            if (!ids.Any())
            {
                return (false, "No venue was selected for removal.");
            }

            // Optimized query with EXISTS
            var venuesWithEvents = await _venueRepository.GetByIdsWithHasEventsAsync(ids);
            var validationResults = new List<(int id, string name, bool canDelete, string reason)>();

            foreach (var (venue, hasEvents) in venuesWithEvents)
            {
                validationResults.Add((venue.Id, venue.Name, !hasEvents,
                    hasEvents ? "has registered events" : ""));
            }

            var cannotDelete = validationResults.Where(v => !v.canDelete).ToList();
            var canDelete = validationResults.Where(v => v.canDelete).ToList();

            if (canDelete.Any())
            {
                var entitiesToDelete = venuesWithEvents
                    .Where(x => canDelete.Any(c => c.id == x.venue.Id))
                    .Select(x => x.venue);

                await _venueRepository.DeleteRangeAsync(entitiesToDelete);
                await _venueRepository.SaveChangesAsync();
            }

            return BuildDeleteResultMessage(canDelete, cannotDelete);
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
            else if (cannotDelete.Count > 0 && canDelete.Count > 0)
            {
                var deleted = canDelete.Count;
                var blocked = cannotDelete.Count;
                var total = deleted + blocked;

                return (true, $"{deleted} of {total} successfully {(total == 1 ? "removed venue" : "removed venues")}. " +
                             $"{blocked} {(blocked == 1 ? "venue couldn´t be removed" : "venues couldn´t be removed")} " +
                             $"({(blocked == 1 ? "has" : "have")} events).");
            }
            else
            {
                // None could be deleted (all blocked)
                var count = cannotDelete.Count;
                return (false, count == 1
                    ? "The venue couldn´t be removed (has events)."
                    : $"The {count} venues couldn´t be removed (have events).");
            }
        }

        public async Task<(bool success, string message)> DeleteVenueAsync(int id) => await DeleteVenuesAsync(new[] { id });

        public async Task<IEnumerable<Venue>> GetAllVenuesAsync() => await _venueRepository.GetAllAsync();

        public async Task<Venue?> GetVenueByIdAsync(int id)
        {
            Guard.AgainstNegativeOrZero(id, nameof(id));
            return await _venueRepository.GetByIdAsync(id);
        }

        #endregion

        public async Task<IEnumerable<VenueListItemDto>> GetAllVenuesForListAsync() =>
             (await _venueRepository.GetAllWithHasEventsAsync())
            .Select(x => VenueMapper.ToListDto(x.venue, x.hasEvents));

        public async Task<IEnumerable<VenueListItemDto>> SearchVenuesForListAsync(string query) =>
            (await _venueRepository.SearchWithHasEventsAsync(query))
            .Select(x => VenueMapper.ToListDto(x.venue, x.hasEvents));

        public async Task<(IEnumerable<VenueListItemDto> items, int totalCount)> GetPagedVenuesForListAsync(
            int pageNumber,
            int pageSize,
            string? query = null)
        {
            Guard.AgainstNegativeOrZero(pageNumber, nameof(pageNumber));
            Guard.AgainstNegativeOrZero(pageSize, nameof(pageSize));

            var (items, totalCount) = await _venueRepository.GetPagedWithEventInfoAsync(pageNumber, pageSize, query);

            var dtos = items.Select(x => VenueMapper.ToListDto(x.venue, x.hasEvents));

            return (dtos, totalCount);
        }

        #region Utilities

        public bool ShouldShowCharacterCounter(int currentLength)
        {
            return currentLength > ShowCounterAt;
        }

        public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
        {
            string text = $"{currentLength}/{MaxInputLength}";
            bool isWarning = currentLength > 27;
            bool isError = currentLength >= MaxInputLength;

            return (text, isWarning, isError);
        }

        #endregion
    }
}
