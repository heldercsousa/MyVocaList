using MyVocaList.Domain;

namespace MyVocaList.Infra.Data.Repositories
{
    /// <summary>
    /// Repository interface for Person entity operations
    /// </summary>
    public interface IPersonRepository : IBaseRepository<Person>
    {
        // Basic methods
        Task<Person> GetByFullNameAsync(string fullName);

        // Optimized search methods
        Task<List<Person>> SearchByNameAsync(string searchTerm, int maxResults = 10);
        Task<List<Person>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10);
        Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10);

        // Method to check for duplicates
        Task<Person> GetByNormalizedNameAsync(string fullName);
    }
}
