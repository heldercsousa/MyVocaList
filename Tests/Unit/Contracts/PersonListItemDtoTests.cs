using System.Globalization;
using MyVocaList.Contracts.Models;

namespace MyVocaList.Tests.Unit.Contracts;

public class PersonListItemDtoTests
{
    // [AC] REQ-PLIST-01: birthday and email both present → "{day} {Mon} · {email}"
    [Fact]
    public void IdentityLine_BirthdayAndEmail_JoinsWithMiddleDot()
    {
        var dto = new PersonListItemDto { BirthDay = 14, BirthMonth = 3, Email = "helder@example.com" };

        Assert.Equal("14 Mar · helder@example.com", dto.IdentityLine);
    }

    // [AC] REQ-PLIST-01: birthday only → "{day} {Mon}"
    [Fact]
    public void IdentityLine_BirthdayOnly_IsBirthday()
    {
        var dto = new PersonListItemDto { BirthDay = 14, BirthMonth = 3, Email = null };

        Assert.Equal("14 Mar", dto.IdentityLine);
    }

    // [AC] REQ-PLIST-01: email only → the email alone
    [Fact]
    public void IdentityLine_EmailOnly_IsEmail()
    {
        var dto = new PersonListItemDto { BirthDay = null, BirthMonth = null, Email = "helder@example.com" };

        Assert.Equal("helder@example.com", dto.IdentityLine);
    }

    // [AC] REQ-PLIST-01: neither present → empty string
    [Fact]
    public void IdentityLine_NeitherBirthdayNorEmail_IsEmpty()
    {
        var dto = new PersonListItemDto { BirthDay = null, BirthMonth = null, Email = null };

        Assert.Equal(string.Empty, dto.IdentityLine);
    }

    // [AC] REQ-PLIST-01: a whitespace email counts as absent
    [Fact]
    public void IdentityLine_WhitespaceEmail_TreatedAsAbsent()
    {
        var dto = new PersonListItemDto { BirthDay = null, BirthMonth = null, Email = "   " };

        Assert.Equal(string.Empty, dto.IdentityLine);
    }

    // [AC] REQ-PLIST-01: whitespace email with a birthday → birthday alone
    [Fact]
    public void IdentityLine_WhitespaceEmailWithBirthday_IsBirthdayOnly()
    {
        var dto = new PersonListItemDto { BirthDay = 14, BirthMonth = 3, Email = "   " };

        Assert.Equal("14 Mar", dto.IdentityLine);
    }

    // [AC] REQ-PLIST-01: single-digit day has no leading zero
    [Fact]
    public void IdentityLine_SingleDigitDay_HasNoLeadingZero()
    {
        var dto = new PersonListItemDto { BirthDay = 5, BirthMonth = 12, Email = null };

        Assert.Equal("5 Dec", dto.IdentityLine);
    }

    // [AC] REQ-PLIST-01: month abbreviation is English (InvariantCulture), not the current culture
    [Fact]
    public void IdentityLine_MonthAbbreviation_IgnoresCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var dto = new PersonListItemDto { BirthDay = 14, BirthMonth = 3, Email = null };

            Assert.Equal("14 Mar", dto.IdentityLine);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
