using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository
{
    /// <summary>
    /// Repository implementation for Person entity operations
    /// </summary>
    public class PersonRepository : BaseRepository<Person>, IPersonRepository
    {

        public PersonRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<Person> GetByFullNameAsync(string fullName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
            return await _dbSet.FirstOrDefaultAsync(p => p.FullName == fullName);
        }

        /// <summary>
        /// Optimized search by normalized name
        /// </summary>
        public async Task<List<Person>> SearchByNameAsync(string searchTerm, int maxResults = 10)
        {
            // Return empty list for invalid search (no exception for user input)
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Person>();


            // Optimized search using normalized column index
            var results = await _dbSet
                .Where(p => p.FullName.StartsWith(searchTerm))
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

            // Search that STARTS with the term (more precise for autocomplete)
            return await _dbSet
                .Where(p => p.FullName.StartsWith(searchTerm))
                .OrderBy(p => p.FullName)
                .Take(maxResults)
                .ToListAsync();
        }

        /// <summary>
        /// Search by any word in name
        /// </summary>
        public async Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10)
        {
            throw new NotImplementedException("Search by any word is not implemented yet. Consider implementing a full-text search solution for this feature.");
        }
    }
}
