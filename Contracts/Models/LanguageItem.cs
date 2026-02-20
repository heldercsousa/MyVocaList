using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.Contracts.Models
{
    public class LanguageItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;

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

        // Propriedades combinadas para exibição
        public string DisplayName => Name;
        public string Countries => Region;
        public string FlagIcon => Flag;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
