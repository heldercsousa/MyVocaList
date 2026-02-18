using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using Microsoft.Extensions.Logging;
using MyVocaList.Domain;

namespace MyVocaList.Infra
{
    public class DatabaseInit : IDatabaseInit
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseInit> _logger;

        public DatabaseInit(AppDbContext context, ILogger<DatabaseInit> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeDatabaseAsync(string appRootPath)
        {
            try
            {
                var dbPath = Path.Combine(appRootPath, "MyVocaList.db");
                var directory = Path.GetDirectoryName(dbPath);

                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogDebug("Directory created: {Directory}", directory);
                }

                // Verify write permissions
                await VerifyWritePermissionsAsync(directory);

                // Apply migrations - Database service responsibility
                _logger.LogDebug("Starting migration application");
                await _context.Database.MigrateAsync();
                _logger.LogDebug("Migrations applied successfully");

                _logger.LogDebug("Database initialized successfully at: {DbPath}", dbPath);

                // Test connection
                await TestConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        private async Task VerifyWritePermissionsAsync(string directory)
        {
            var testFile = Path.Combine(directory, "test.txt");
            try
            {
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);
                _logger.LogDebug("Write permissions verified");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Write permission error in directory");
                throw;
            }
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                _logger.LogDebug("Database connection tested successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                throw;
            }
        }

        public async Task<bool> IsDatabaseAvailableAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasPendingMigrationsAsync()
        {
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                return pendingMigrations.Any();
            }
            catch
            {
                return false;
            }
        }
    }
}
