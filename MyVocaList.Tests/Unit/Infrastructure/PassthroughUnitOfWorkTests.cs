using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Infrastructure;

public class PassthroughUnitOfWorkTests
{
    public interface IProbe
    {
        int Value { get; }
    }

    private sealed class Probe(int value) : IProbe
    {
        public int Value { get; } = value;
    }

    [Fact]
    public async Task ExecuteAsync_generic_invokes_body_and_returns_its_value()
    {
        IUnitOfWork uow = PassthroughUnitOfWork.Over(new Probe(42));
        var invoked = false;

        var result = await uow.ExecuteAsync(sp =>
        {
            invoked = true;
            return Task.FromResult(sp.GetService(typeof(IProbe)) is IProbe p ? p.Value : -1);
        });

        Assert.True(invoked);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_no_signal_invokes_body()
    {
        IUnitOfWork uow = PassthroughUnitOfWork.Over(new Probe(7));
        var invoked = false;

        await uow.ExecuteAsync(sp =>
        {
            invoked = true;
            Assert.NotNull(sp.GetService(typeof(IProbe)));
            return Task.CompletedTask;
        });

        Assert.True(invoked);
    }

    [Fact]
    public async Task ExecuteReadAsync_invokes_body_and_returns_its_value()
    {
        IUnitOfWork uow = PassthroughUnitOfWork.Over(new Probe(99));

        var result = await uow.ExecuteReadAsync(sp =>
            Task.FromResult(((IProbe)sp.GetService(typeof(IProbe))!).Value));

        Assert.Equal(99, result);
    }

    [Fact]
    public async Task Over_accepts_Mock_instances_and_unwraps_to_Object()
    {
        var mock = new Mock<IProbe>();
        mock.Setup(p => p.Value).Returns(5);

        IUnitOfWork uow = PassthroughUnitOfWork.Over(mock);

        var result = await uow.ExecuteReadAsync(sp =>
            Task.FromResult(((IProbe)sp.GetService(typeof(IProbe))!).Value));

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task Resolving_an_unregistered_type_throws_named_InvalidOperationException()
    {
        IUnitOfWork uow = PassthroughUnitOfWork.Over(new Probe(1));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync(sp => Task.FromResult((string)sp.GetService(typeof(string))!)));

        Assert.Contains("PassthroughUnitOfWork was not given an instance of String", ex.Message);
    }
}
