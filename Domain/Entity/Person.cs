namespace MyVocaList.Domain.Entity;

public class Person
{
    public int Id { get; set; } // Primary key for the database
    public string FullName { get; set; }
    public string FullNameNormalized { get; set; } // For optimized search
    public int Participations { get; set; } = 0; // Participation counter
    public int Absences { get; set; } = 0; // Absence counter

    // Fields for intelligent differentiation
    public string BirthdayDayMonth { get; set; } // Format: "15/03" (required)
    public string Email { get; set; } // Optional for marketing/differentiation

    public Person(string fullName)
    {
        FullName = fullName;
        FullNameNormalized = string.Empty; // Will be set by the service
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

        if (!string.IsNullOrWhiteSpace(BirthdayDayMonth))
        {
            return $"({BirthdayDayMonth})";
        }

        return $"(ID: {Id})"; // Extreme fallback
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

        // If it's birthday or ID, show it with the name
        return $"{FullName} {identifier}";
    }

    /// <summary>
    /// Updates normalization when name changes (will be called by the service)
    /// </summary>
    public void SetNormalizedName(string normalizedName)
    {
        FullNameNormalized = normalizedName;
    }
}
