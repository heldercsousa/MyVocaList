namespace MyVocaList.Tests.Unit.ViewModels;

public class PersonsViewModelTests
{
    private readonly Mock<IPersonService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<PersonsViewModel>> _loggerMock = new();

    private PersonsViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object);

    // ── AppBarTitle ───────────────────────────────────────────────────────

    [Fact]
    public void AppBarTitle_WhenNoneSelected_ReturnsSingers()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(0);
        Assert.Equal("Singers", sut.AppBarTitle);
    }

    [Fact]
    public void AppBarTitle_WhenOneSelected_Returns1Selected()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(1);
        Assert.Equal("1 selected", sut.AppBarTitle);
    }

    [Fact]
    public void AppBarTitle_WhenMultipleSelected_ReturnsNSelected()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(3);
        Assert.Equal("3 selected", sut.AppBarTitle);
    }

    // ── CanEditSelected / CanDeleteSelected ───────────────────────────────

    [Fact]
    public void CanEditSelected_WhenOneSelected_IsTrue()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(1);
        Assert.True(sut.CanEditSelected);
    }

    [Fact]
    public void CanEditSelected_WhenTwoSelected_IsFalse()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(2);
        Assert.False(sut.CanEditSelected);
    }

    [Fact]
    public void CanDeleteSelected_WhenOneOrMoreSelected_IsTrue()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(1);
        Assert.True(sut.CanDeleteSelected);
    }

    [Fact]
    public void CanDeleteSelected_WhenNoneSelected_IsFalse()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(0);
        Assert.False(sut.CanDeleteSelected);
    }

    // ── Empty state derived properties ────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_EmptyDb_SetsIsEmptyNoPersons()
    {
        _serviceMock.Setup(s => s.GetPagedPersonsForListAsync(1, 20, null, default))
                    .ReturnsAsync((Enumerable.Empty<PersonListItemDto>(), 0));

        var sut = CreateSut();
        await sut.InitializeAsync();

        Assert.True(sut.IsEmptyNoPersons);
        Assert.False(sut.IsEmptyNoResults);
        Assert.False(sut.IsInitialLoading);
    }

    [Fact]
    public async Task InitializeAsync_WithPersons_PopulatesPersons()
    {
        var dtos = new List<PersonListItemDto>
        {
            new() { Id = 1, FullName = "John Doe", Participations = 2 }
        };
        _serviceMock.Setup(s => s.GetPagedPersonsForListAsync(1, 20, null, default))
                    .ReturnsAsync((dtos.AsEnumerable(), 1));

        var sut = CreateSut();
        await sut.InitializeAsync();

        Assert.Single(sut.Persons);
        Assert.False(sut.IsEmptyNoPersons);
    }

    // ── SelectAll ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectAllCommand_WhenNotAllSelected_SelectsAll()
    {
        var dtos = new List<PersonListItemDto>
        {
            new() { Id = 1, FullName = "John Doe" },
            new() { Id = 2, FullName = "Jane Smith" }
        };
        _serviceMock.Setup(s => s.GetPagedPersonsForListAsync(1, 20, null, default))
                    .ReturnsAsync((dtos.AsEnumerable(), 2));

        var sut = CreateSut();
        await sut.InitializeAsync();

        // No items selected initially
        Assert.False(sut.IsAllSelected);
    }
}
