using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain;
using MyVocaList.Infra.Utils;

namespace MyVocaList.Infra.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Person entity operations
    /// </summary>
    public class PersonRepository : BaseRepository<Person>, IPersonRepository
    {
        private readonly ITextNormalizer _textNormalizer;

        public PersonRepository(AppDbContext context, ITextNormalizer textNormalizer)
            : base(context)
        {
            _textNormalizer = textNormalizer;
        }

        public async Task<Person> GetByFullNameAsync(string fullName)
        {
            Guard.AgainstNullOrWhiteSpace(fullName, nameof(fullName));
            return await _dbSet.FirstOrDefaultAsync(p => p.FullName == fullName);
        }

        /// <summary>
        /// Optimized search by normalized name
        /// </summary>
        public async Task<List<Person>> SearchByNameAsync(string searchTerm, int maxResults = 10)
        {
            // Return empty list for invalid search (no exception for user input)
            if (Guard.IsNullOrWhiteSpace(searchTerm))
                return new List<Person>();

            // Normalize search term
            var normalizedSearch = _textNormalizer.NormalizeName(searchTerm);

            Console.WriteLine($"Searching: '{searchTerm}' → normalized: '{normalizedSearch}'");

            // Optimized search using normalized column index
            var results = await _dbSet
                .Where(p => p.FullNameNormalized.Contains(normalizedSearch))
                .OrderBy(p => p.FullName)
                .Take(maxResults)
                .ToListAsync();

            Console.WriteLine($"Found {results.Count} results");

            return results;
        }

        /// <summary>
        /// Optimized search that starts with term
        /// </summary>
        public async Task<List<Person>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Person>();

            var normalizedSearch = _textNormalizer.NormalizeName(searchTerm);

            // Search that STARTS with the term (more precise for autocomplete)
            return await _dbSet
                .Where(p => p.FullNameNormalized.StartsWith(normalizedSearch))
                .OrderBy(p => p.FullName)
                .Take(maxResults)
                .ToListAsync();
        }

        /// <summary>
        /// Search by any word in name
        /// </summary>
        public async Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Person>();

            string normalizedSearch = _textNormalizer.NormalizeName(searchTerm);
            var searchWords = normalizedSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (searchWords.Length == 0)
                return new List<Person>();

            var query = _dbSet.AsQueryable();

            // Search people that contain ANY of the words
            foreach (var word in searchWords)
            {
                var currentWord = word; // Capture for lambda
                query = query.Where(p => p.FullNameNormalized.Contains(currentWord));
            }

            return await query
                .OrderBy(p => p.FullName)
                .Take(maxResults)
                .ToListAsync();
        }

        /// <summary>
        /// Check if person exists with normalized name
        /// </summary>
        public async Task<Person> GetByNormalizedNameAsync(string fullName)
        {
            var normalizedName = _textNormalizer.NormalizeName(fullName);

            return await _dbSet
                .FirstOrDefaultAsync(p => p.FullNameNormalized == normalizedName);
        }
    }
}
