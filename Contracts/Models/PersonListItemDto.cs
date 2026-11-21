using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MyVocaList.Contracts.Models
{
    public class PersonListItemDto : INotifyPropertyChanged
    {
        public int Id { get; set; } // The ID of the Person from the domain
        public int? BirthDay { get; set; } // Day 1-31, null when no birthday (REQ-BDAY-07)
        public int? BirthMonth { get; set; } // Month 1-12, null when no birthday
        public string Email { get; set; }
        private string _fullName;
        private int _participations;
        private int _absences;

        public event PropertyChangedEventHandler PropertyChanged;

        // Parameterless constructor for JSON deserialization (Preferences)
        public PersonListItemDto() { }

        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Participations
        {
            get => _participations;
            set
            {
                if (_participations != value)
                {
                    _participations = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ParticipationsAbsencesNumber));
                }
            }
        }

        public int Absences
        {
            get => _absences;
            set
            {
                if (_absences != value)
                {
                    _absences = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ParticipationsAbsencesNumber));
                }
            }
        }

        [JsonIgnore] // This is a calculated property for the UI, it doesn't need to be serialized.
        public string ParticipationsAbsencesNumber => $"Participations: {Participations} / Absences: {Absences}";

        /// <summary>
        /// Second line of a Singers-list row: "{day} {Mon} · {email}", or only the part that exists, or "" when neither.
        /// Month abbreviation is English (InvariantCulture). A blank email counts as absent.
        /// </summary>
        [JsonIgnore] // Calculated for the UI, not serialized.
        public string IdentityLine
        {
            get
            {
                string birthday = BirthDay is int day && BirthMonth is int month
                    ? $"{day} {CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month)}"
                    : string.Empty;
                string email = string.IsNullOrWhiteSpace(Email) ? string.Empty : Email;

                if (birthday.Length == 0) return email;
                if (email.Length == 0) return birthday;
                return $"{birthday} · {email}";
            }
        }

        /// <summary>Initials for the leading monogram (e.g. "JD" for "John Doe").</summary>
        [JsonIgnore]
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName)) return "?";
                var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                    : parts[0][0].ToString().ToUpperInvariant();
            }
        }

        // Increment methods that the UI will call, updating the DTO
        public void IncrementParticipations()
        {
            Participations++;
        }

        public void IncrementAbsences()
        {
            Absences++;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
