using System.Globalization;

namespace MyVocaList.Domain.Entity;

public class Person
{
    public int Id { get; set; } // Primary key for the database
    public Guid? ExternalId { get; set; }   // Reserved for future device/account identity
    public string FullName { get; set; }
    public int Participations { get; set; } = 0; // Participation counter
    public int Absences { get; set; } = 0; // Absence counter

    // Fields for intelligent differentiation
    public int? BirthDay { get; set; } // Day of month 1-31 (optional, no year; REQ-BDAY-07)
    public int? BirthMonth { get; set; } // Month 1-12 (optional; both null or both set)
    public string? Email { get; set; } // Optional for marketing/differentiation

    public Person(string fullName)
    {
        FullName = fullName;
    }

    public Person() { } // Parameterless constructor for EF Core

    // ONLY DOMAIN METHODS (no validation or normalization)

    /// <summary>
    /// Generates unique identifier for display
    /// </summary>
    public string GetDisplayIdentifier()
    {
        // Priority: Email > Birthday > Fallback
        if (!string.IsNullOrWhiteSpace(Email))
        {
            return Email.ToLowerInvariant();
        }

        if (BirthDay.HasValue && BirthMonth.HasValue)
        {
            // English three-letter month regardless of the current culture (REQ-BDAY-10)
            var monthAbbreviation = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(BirthMonth.Value);
            return $"({BirthDay.Value} {monthAbbreviation})";
        }

        return string.Empty; // No identifier shown; the database Id is for developers only (REQ-DUP-15)
    }

    /// <summary>
    /// Formats name for display in suggestions
    /// </summary>
    public string GetDisplayName()
    {
        var identifier = GetDisplayIdentifier();

        // If it's an email, show it below the name
        if (!string.IsNullOrWhiteSpace(Email) && identifier == Email.ToLowerInvariant())
        {
            return FullName; // Email goes on a separate line
        }

        // No identifier (REQ-DUP-15): the name alone, with no trailing space
        if (string.IsNullOrEmpty(identifier))
        {
            return FullName;
        }

        // If it's birthday, show it with the name
        return $"{FullName} {identifier}";
    }

}
