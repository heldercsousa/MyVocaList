using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Infrastructure;

/// <summary>
/// Test-only <see cref="IImageStore"/> mocks for ViewModel tests. <see cref="PassThrough"/> resolves every
/// reference to itself, so a test that asserts an option's image URL keeps seeing the URL it supplied
/// (the ViewModels pass option images through <see cref="IImageStore.ResolveDisplaySource"/>, REQ-PIC-39).
/// </summary>
public static class TestImageStore
{
    /// <summary>A loose mock whose <c>ResolveDisplaySource</c> returns its input for both sizes.</summary>
    public static Mock<IImageStore> PassThrough()
    {
        var store = new Mock<IImageStore>();
        store.Setup(s => s.ResolveDisplaySource(It.IsAny<string?>(), It.IsAny<ImageSize>()))
             .Returns((string? reference, ImageSize _) => reference);
        return store;
    }
}
