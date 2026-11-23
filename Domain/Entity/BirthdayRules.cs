namespace MyVocaList.Domain.Entity;

/// <summary>
/// Calendar rules for the optional birthday (day + month, no year, Gregorian). Pure functions, no state.
/// </summary>
public static class BirthdayRules
{
    /// <summary>
    /// Number of days selectable for a birthday in <paramref name="month"/> (1 to 12).
    /// February returns 29, because a birthday carries no year (REQ-BDAY-05).
    /// </summary>
    public static int DaysInMonth(int month) => month switch
    {
        2 => 29,
        4 or 6 or 9 or 11 => 30,
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        _ => throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12")
    };
}
