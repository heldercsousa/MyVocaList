namespace MyVocaList.Domain
{
    public interface IDatabaseInit
    {
        Task InitializeDatabaseAsync(string appRootPath);
        Task<bool> IsDatabaseAvailableAsync();
        Task<bool> HasPendingMigrationsAsync();
    }
}
