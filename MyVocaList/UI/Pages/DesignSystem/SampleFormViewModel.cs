using System.ComponentModel.DataAnnotations;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Sample ViewModel demonstrating AutoFormView capabilities with DataAnnotations
/// </summary>
public class SampleFormViewModel
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email Address")]
    public string Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; }

    [Range(18, 100, ErrorMessage = "Age must be between 18 and 100")]
    [Display(Name = "Age")]
    public int? Age { get; set; }

    [Required]
    [Display(Name = "Birth Date")]
    public DateTime? BirthDate { get; set; }

    [Display(Name = "Preferred Contact Time")]
    public TimeSpan? PreferredContactTime { get; set; }

    [Required]
    [Display(Name = "Music Genre")]
    public MusicGenre PreferredGenre { get; set; }

    [Display(Name = "Subscribe to Newsletter")]
    public bool SubscribeToNewsletter { get; set; }

    [Display(Name = "Number of Songs")]
    [Range(1, 1000, ErrorMessage = "Must be between 1 and 1000")]
    public int NumberOfSongs { get; set; } = 10;
}

/// <summary>
/// Sample enum for demonstration
/// </summary>
public enum MusicGenre
{
    Rock,
    Pop,
    Jazz,
    Classical,
    Electronic,
    HipHop,
    Country,
    Other
}