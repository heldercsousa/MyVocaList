using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 1.3 — proves the post-swap composition: <c>AddDbContextFactory&lt;AppDbContext&gt;(…,
/// ServiceLifetime.Scoped)</c> plus a singleton <see cref="IUnitOfWork"/>.
/// <para>
/// <b>Why these tests build the collection rather than calling <c>MauiProgram.CreateMauiApp()</c>:</b>
/// <c>MauiProgram.cs</c> is compiled out of the plain <c>net10.0</c> TFM that this test project
/// consumes (<c>MyVocaList.csproj</c>: <c>&lt;Compile Remove="MauiProgram.cs" /&gt;</c> under
/// <c>'$(TargetFramework)' == 'net10.0'</c>), and it reads <c>FileSystem.AppDataDirectory</c>, which
/// has no value off-device. The real composition root is therefore unreachable from a unit-test
/// process. It is covered in two halves instead: the behavioural half here runs against
/// <see cref="UnitOfWorkTestHost.Create"/>, which mirrors the production registration shape
/// line-for-line, and the drift half (<see cref="MauiProgram_RegistrationShape_MatchesTestHost"/>)
/// reads <c>MauiProgram.cs</c> as source text so the two cannot silently diverge.
/// </para>
/// </summary>
public class UnitOfWorkCompositionTests
{
    [Fact]
    // [AC] REQ-UOW-01: a DI-composition test asserts IDbContextFactory<AppDbContext> and IUnitOfWork
    // are both registered exactly once.
    public async Task Composition_RegistersFactoryAndUnitOfWork_ExactlyOnce()
    {
        var descriptors = await CaptureDescriptorsAsync();

        Assert.Single(descriptors.Where(d => d.ServiceType == typeof(IDbContextFactory<AppDbContext>)));
        Assert.Single(descriptors.Where(d => d.ServiceType == typeof(IUnitOfWork)));

        await using var host = UnitOfWorkTestHost.Create();
        Assert.NotNull(host.Resolve<IUnitOfWork>());
        Assert.NotNull(host.Resolve<IDbContextFactory<AppDbContext>>());
    }

    [Fact]
    // [AC] REQ-UOW-01: "AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped) registers
    // AppDbContext itself as an ordinary scoped ServiceDescriptor by design — repositories keep
    // constructor-injecting it." This is the load-bearing claim that makes the swap non-breaking.
    public async Task Composition_AppDbContext_StillResolvesDirectlyFromAScope()
    {
        var descriptors = await CaptureDescriptorsAsync();
        var contextDescriptor = Assert.Single(descriptors.Where(d => d.ServiceType == typeof(AppDbContext)));
        Assert.Equal(ServiceLifetime.Scoped, contextDescriptor.Lifetime);

        await using var host = UnitOfWorkTestHost.Create();
        Assert.NotNull(host.Resolve<AppDbContext>());
        // Repositories inject AppDbContext directly; if the claim were false this would throw.
        Assert.NotNull(host.Resolve<IArtistRepository>());
    }

    [Fact]
    // [AC] REQ-UOW-02: every AppDbContext instance is scoped to the unit of work that created it —
    // N scopes yield N distinct instances (compared by reference), one instance within a scope.
    public async Task Composition_EachScopeGetsADistinctAppDbContext_SameInstanceWithinAScope()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var factory = host.Resolve<IDbContextFactory<AppDbContext>>();

        using var scopeA = host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        using var scopeB = host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var a1 = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var a2 = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var b1 = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Same(a1, a2);          // one context per scope
        Assert.NotSame(a1, b1);       // distinct context per scope

        // The factory hands out a fresh, caller-owned instance on every call, in any scope.
        await using var f1 = await factory.CreateDbContextAsync();
        await using var f2 = await factory.CreateDbContextAsync();
        Assert.NotSame(f1, f2);
        Assert.NotSame(a1, f1);
    }

    [Fact]
    // [AC] REQ-UOW-14: CollationInterceptor and TransactionLogInterceptor remain registered on every
    // AppDbContext produced by the new pattern — a collated query still works and a transaction-log
    // entry is still written for a save.
    public async Task Composition_InterceptorsSurviveTheSwap_CollatedQueryAndTransactionLog()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();

        var (created, _, artist) = await artists.CreateArtistAsync("Céline Dion");
        Assert.True(created);
        Assert.NotNull(artist);

        // NOCASE_NOACCENT is registered by CollationInterceptor on connection open. Without it SQLite
        // raises "no such collation sequence"; with it, this accent- and case-insensitive lookup hits.
        var repo = host.Resolve<IArtistRepository>();
        var found = await repo.GetByNameAsync("celine dion", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(artist!.Id, found!.Id);

        // TransactionLogInterceptor observed the save through the factory-produced context.
        Assert.Contains(host.Log.Entries, e => e.Contains("Artist", StringComparison.Ordinal));
    }

    [Fact]
    // [AC] REQ-UOW-01 / REQ-UOW-21: MauiProgram.cs is excluded from the net10.0 TFM (see class
    // remarks), so its registration shape is asserted as source text: the factory swap and the
    // IUnitOfWork registration are present, plain AddDbContext<AppDbContext> is gone, and exactly one
    // IAppInfo registration remains.
    public void MauiProgram_RegistrationShape_MatchesTestHost()
    {
        var source = File.ReadAllText(LocateMauiProgram());

        Assert.Contains("AddDbContextFactory<AppDbContext>", source, StringComparison.Ordinal);
        Assert.Contains("ServiceLifetime.Scoped", source, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IUnitOfWork,", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddDbContext<AppDbContext>", source, StringComparison.Ordinal);
        Assert.Contains("UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)", source, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<CollationInterceptor>()", source, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<TransactionLogInterceptor>()", source, StringComparison.Ordinal);

        // REQ-UOW-21: the duplicate IAppInfo registration is removed, leaving one.
        var appInfoRegistrations = source
            .Split('\n')
            .Count(l => l.Contains("AddSingleton<IAppInfo>", StringComparison.Ordinal));
        Assert.Equal(1, appInfoRegistrations);
    }

    /// <summary>The production registration shape, captured off the same collection the host builds.</summary>
    private static async Task<List<ServiceDescriptor>> CaptureDescriptorsAsync()
    {
        var captured = new List<ServiceDescriptor>();
        await using var host = UnitOfWorkTestHost.Create(services => captured.AddRange(services));
        return captured;
    }

    private static string LocateMauiProgram()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "MyVocaList", "MauiProgram.cs");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("MyVocaList/MauiProgram.cs not found walking up from the test output directory.");
    }
}
