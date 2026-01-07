using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.Contracts.DTOs.List
{
    public class VenueListItemDto : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool HasEvents { get; set; }
        public string StatusText => HasEvents ? "COM EVENTOS" : "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
