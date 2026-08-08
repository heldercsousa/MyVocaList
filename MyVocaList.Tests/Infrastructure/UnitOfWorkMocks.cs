using MyVocaList.Domain.UnitOfWork;

namespace MyVocaList.Tests.Infrastructure;

/// <summary>
/// Test-only <see cref="IUnitOfWork"/> that runs the supplied body immediately against a stub
/// <see cref="IServiceProvider"/> built from a fixed set of instances — no DI scope, no
/// transaction, no save. This lets an existing Moq-based unit test keep constructing its service
/// directly with mocked repositories, wrap the call in <c>_uow.ExecuteAsync(...)</c>, and assert on
/// those same mocks exactly as it did before the service gained an <see cref="IUnitOfWork"/>
/// constructor parameter (Task 1.4, `plan.md § Task 1.4`).
/// </summary>
public sealed class PassthroughUnitOfWork(IServiceProvider serviceProvider) : IUnitOfWork
{
    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TResult>(
        Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        => body(serviceProvider);

    /// <inheritdoc />
    public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
        => body(serviceProvider);

    /// <inheritdoc />
    public Task<TResult> ExecuteReadAsync<TResult>(
        Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        => body(serviceProvider);

    /// <summary>Builds a <see cref="PassthroughUnitOfWork"/> over a fixed set of instances. Accepts
    /// plain instances (repositories, services, anything a lambda body might resolve) and/or
    /// <see cref="Mock{T}"/> objects — a <c>Mock&lt;T&gt;</c> is unwrapped to its <c>.Object</c>
    /// automatically, so tests can pass their existing <c>Mock&lt;ISongRepository&gt;</c> fields
    /// straight through without calling <c>.Object</c> themselves.</summary>
    public static PassthroughUnitOfWork Over(params object[] instances)
        => new(new StubServiceProvider(instances.Select(Unwrap).ToArray()));

    private static object Unwrap(object instance)
    {
        var type = instance.GetType();
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Mock<>))
            return instance;

        // Mock<T>.Object is redeclared with `new` at every level of the Mock<T> hierarchy, so a plain
        // Type.GetProperty("Object") throws AmbiguousMatchException. Reading it via the base non-generic
        // Mock type returns the single unambiguous object.
        var objectProperty = typeof(Mock).GetProperty(nameof(Mock.Object))
            ?? throw new InvalidOperationException(
                "PassthroughUnitOfWork.Over could not locate Mock.Object via reflection.");
        return objectProperty.GetValue(instance)
            ?? throw new InvalidOperationException(
                $"PassthroughUnitOfWork.Over received a {type.Name} whose Object property returned null.");
    }

    /// <summary>Resolves the first supplied instance assignable to the requested type. Throws a
    /// clear, named <see cref="InvalidOperationException"/> rather than returning null — a silent
    /// null here would otherwise surface later as an inscrutable <see cref="NullReferenceException"/>
    /// deep inside a lambda body.</summary>
    private sealed class StubServiceProvider(object[] instances) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => instances.FirstOrDefault(serviceType.IsInstanceOfType)
               ?? throw new InvalidOperationException(
                   $"PassthroughUnitOfWork was not given an instance of {serviceType.Name}.");
    }
}
