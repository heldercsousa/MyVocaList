using Microsoft.EntityFrameworkCore;

namespace MyVocaList.Services
{
    public interface IDatabaseService
    {
        Task InitializeDatabaseAsync();
        Task<bool> IsDatabaseAvailableAsync();
        Task<bool> HasPendingMigrationsAsync();
    }
}
