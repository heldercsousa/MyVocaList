using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MyVocaList.Contracts.Models
{
    public class PersonListItemDto : INotifyPropertyChanged
    {
        public int Id { get; set; } // The ID of the Person from the domain
        public string BirthdayDayMonth { get; set; }
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
