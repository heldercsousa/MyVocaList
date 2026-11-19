using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteWindowClassTests
{
    [Fact]
    public void IsCompactWindow_PhoneIdiom_ReturnsTrue()
    {
        var deviceInfoMock = new Mock<IDeviceInfo>();
        deviceInfoMock.Setup(d => d.Idiom).Returns(DeviceIdiom.Phone);

        var result = AutocompleteWindowClass.IsCompactWindow(deviceInfoMock.Object);

        Assert.True(result);
    }

    [Fact]
    public void IsCompactWindow_DesktopIdiom_ReturnsFalse()
    {
        var deviceInfoMock = new Mock<IDeviceInfo>();
        deviceInfoMock.Setup(d => d.Idiom).Returns(DeviceIdiom.Desktop);

        var result = AutocompleteWindowClass.IsCompactWindow(deviceInfoMock.Object);

        Assert.False(result);
    }

    [Fact]
    public void IsCompactWindow_TabletIdiom_ReturnsFalse()
    {
        var deviceInfoMock = new Mock<IDeviceInfo>();
        deviceInfoMock.Setup(d => d.Idiom).Returns(DeviceIdiom.Tablet);

        var result = AutocompleteWindowClass.IsCompactWindow(deviceInfoMock.Object);

        Assert.False(result);
    }

    [Fact]
    public void IsCompactWindow_NullDeviceInfo_ReturnsFalse()
    {
        var result = AutocompleteWindowClass.IsCompactWindow(null);

        Assert.False(result);
    }
}
