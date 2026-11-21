using MyVocaList.Domain.Entity;

namespace MyVocaList.Tests.Unit.Domain;

public class PersonBirthdayTests
{
    // [AC] REQ-BDAY-10: birthday renders as "(15 Mar)" — day number plus English three-letter month
    [Fact]
    public void GetDisplayIdentifier_BirthdayWithoutEmail_RendersDayAndAbbreviatedMonth()
    {
        var person = new Person("John Doe") { BirthDay = 15, BirthMonth = 3 };

        Assert.Equal("(15 Mar)", person.GetDisplayIdentifier());
    }

    // [AC] REQ-BDAY-10: single-digit day is not zero-padded
    [Fact]
    public void GetDisplayIdentifier_SingleDigitDay_IsNotPadded()
    {
        var person = new Person("John Doe") { BirthDay = 5, BirthMonth = 12 };

        Assert.Equal("(5 Dec)", person.GetDisplayIdentifier());
    }

    // [AC] REQ-BDAY-10: priority order (email, birthday, ID) is unchanged
    [Fact]
    public void GetDisplayIdentifier_EmailAndBirthday_EmailWins()
    {
        var person = new Person("John Doe") { Email = "John@Example.com", BirthDay = 15, BirthMonth = 3 };

        Assert.Equal("john@example.com", person.GetDisplayIdentifier());
    }

    // [AC] REQ-DUP-15: no email and no birthday shows no identifier
    [Fact]
    public void GetDisplayIdentifier_NoEmailNoBirthday_ReturnsEmpty()
    {
        var person = new Person("John Doe") { Id = 7 };

        Assert.Equal(string.Empty, person.GetDisplayIdentifier());
    }

    // [AC] REQ-DUP-15: no email and no birthday shows no identifier
    [Fact]
    public void GetDisplayName_NoEmailNoBirthday_ReturnsFullNameOnly()
    {
        var person = new Person("John Doe") { Id = 7 };

        Assert.Equal("John Doe", person.GetDisplayName());
    }

    // [AC] REQ-BDAY-02: day counts per month — February allows 29, 30-day months stop at 30
    [Theory]
    [InlineData(1, 31)]
    [InlineData(2, 29)]
    [InlineData(3, 31)]
    [InlineData(4, 30)]
    [InlineData(5, 31)]
    [InlineData(6, 30)]
    [InlineData(7, 31)]
    [InlineData(8, 31)]
    [InlineData(9, 30)]
    [InlineData(10, 31)]
    [InlineData(11, 30)]
    [InlineData(12, 31)]
    public void DaysInMonth_EachMonth_ReturnsCalendarMaximum(int month, int expected)
    {
        Assert.Equal(expected, BirthdayRules.DaysInMonth(month));
    }
}
