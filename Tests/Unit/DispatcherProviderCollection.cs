namespace MyVocaList.Tests.Unit;

/// <summary>
/// Shared collection for tests that install a process-wide <c>DispatcherProvider</c>. The provider is static, so these
/// classes never run beside each other; each restores the previous provider when it finishes.
/// </summary>
[CollectionDefinition(DispatcherProviderCollection.Name, DisableParallelization = true)]
public sealed class DispatcherProviderCollection
{
    public const string Name = "DispatcherProvider";
}
