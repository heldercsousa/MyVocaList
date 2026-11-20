using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Exceptions;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// BUG-088 REQ-DUP-06, the Infra half: the unit of work turns a SQLite constraint failure (error 19) raised by the
/// save into <see cref="ConstraintViolationException"/> and rolls back. Uses the EnsureCreated unique index
/// IX_Persons_Email, so it does not depend on the migration-only identity index.
/// </summary>
public class PersonIdentityConstraintTranslationTests
{
    // [AC] REQ-DUP-06: a unique violation on save surfaces as ConstraintViolationException, with the SQLite error kept, and nothing is committed
    [Fact]
    public async Task ExecuteAsync_UniqueViolationOnSave_ThrowsConstraintViolationAndRollsBack()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var ex = await Assert.ThrowsAsync<ConstraintViolationException>(() =>
            uow.ExecuteAsync<(bool success, string message)>(async sp =>
            {
                var repo = sp.GetRequiredService<IPersonRepository>();
                await repo.AddAsync(new Person("Ana Lima") { Email = "same@example.com" });
                await repo.AddAsync(new Person("Bea Costa") { Email = "same@example.com" });
                return (true, "saved");
            }));

        var dbUpdate = Assert.IsType<DbUpdateException>(ex.InnerException);
        var sqlite = Assert.IsType<SqliteException>(dbUpdate.InnerException);
        Assert.Equal(19, sqlite.SqliteErrorCode);

        // The failed save rolled back: neither row was committed.
        Assert.Equal(0, await host.Db.Set<Person>().CountAsync());
    }
}
