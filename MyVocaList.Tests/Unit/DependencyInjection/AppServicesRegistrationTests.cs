using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Extensions;
using MyVocaList.Infra;
using MyVocaList.UI.Components;

namespace MyVocaList.Tests.Unit.DependencyInjection;

/// <summary>
/// DI-resolution regression tests for BUG-021 (SongsPage FAB crash):
/// "Unable to resolve service for type 'MyVocaList.Domain.ServicesInterfaces.ISimilarityScorer'
/// while attempting to activate 'MyVocaList.Services.ArtistResolutionService'."
/// Verifies the app's real registration graph (via ServiceCollectionExtensions.AddAppServices)
/// can activate every service in the SongFormViewModel dependency chain.
/// </summary>
public class AppServicesRegistrationTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Repositories need an AppDbContext registration; it is never queried here.
        services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=:memory:"));
        services.AddAppServices();
        extra?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    // [BUG] BUG-021: FAB on SongsPage crashes — ISimilarityScorer not registered in DI
    public void AddAppServices_ResolvingArtistResolutionService_Succeeds()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IArtistResolutionService>();

        Assert.NotNull(service);
    }

    [Fact]
    // [BUG] BUG-021: SongFormViewModel depends on ISongResolutionService, which transitively needs ISimilarityScorer
    public void AddAppServices_ResolvingSongResolutionService_Succeeds()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ISongResolutionService>();

        Assert.NotNull(service);
    }

    [Fact]
    // [BUG] BUG-021: full SongFormViewModel dependency graph must activate (MAUI-platform-only deps mocked)
    public void AddAppServices_ResolvingSongFormViewModelGraph_Succeeds()
    {
        using var provider = BuildProvider(services =>
        {
            // MAUI-platform-only dependencies, registered in MauiProgram.cs — mocked here.
            services.AddSingleton(Mock.Of<ISnackbarComponent>());
            services.AddSingleton(Mock.Of<ISecureStorageWrapper>());
            services.AddSingleton<IMessenger>(new WeakReferenceMessenger());
            services.AddTransient<SongFormViewModel>();
        });
        using var scope = provider.CreateScope();

        var viewModel = scope.ServiceProvider.GetRequiredService<SongFormViewModel>();

        Assert.NotNull(viewModel);
    }
}
