using CommunityToolkit.Mvvm.ComponentModel;
using MyVocaList.Contracts.Enums;

namespace MyVocaList.UI.Models;

/// <summary>Display-friendly view model for a queue entry.</summary>
public partial class QueueEntryViewModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int eventId;
    [ObservableProperty] private int position;
    [ObservableProperty] private string personName;
    [ObservableProperty] private string songTitle;
    [ObservableProperty] private string artistName;
    [ObservableProperty] private QueueEntryStatus status;
    [ObservableProperty] private int? performanceDurationMinutes;
    [ObservableProperty] private DateTime? performanceStartTime;
    [ObservableProperty] private bool isCurrentSinger;
    [ObservableProperty] private bool canDrag;

    /// <summary>Extracts first letter of first and last name (e.g., "João Silva" → "JS").</summary>
    public string PersonInitials
    {
        get
        {
            var parts = PersonName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }

    /// <summary>Performance time in MM:SS format (e.g., "02:15").</summary>
    public string PerformanceTimeDisplay
    {
        get
        {
            if (PerformanceDurationMinutes == null) return "--:--";
            var minutes = PerformanceDurationMinutes.Value / 60;
            var seconds = PerformanceDurationMinutes.Value % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
