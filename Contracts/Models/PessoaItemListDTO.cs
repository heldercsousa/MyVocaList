using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MyVocaList.Contracts.Models
{
    public class PessoaListItemDto : INotifyPropertyChanged
    {
        public int Id { get; set; } // O ID da Pessoa do domínio
        private string _nomeCompleto;
        private int _participacoes;
        private int _ausencias;

        public event PropertyChangedEventHandler PropertyChanged;

        // Construtor sem argumentos para deserialização de JSON (Preferences)
        public PessoaListItemDto() { }

        public string NomeCompleto
        {
            get => _nomeCompleto;
            set
            {
                if (_nomeCompleto != value)
                {
                    _nomeCompleto = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Participacoes
        {
            get => _participacoes;
            set
            {
                if (_participacoes != value)
                {
                    _participacoes = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NumeroParticipacoesAusencias));
                }
            }
        }

        public int Ausencias
        {
            get => _ausencias;
            set
            {
                if (_ausencias != value)
                {
                    _ausencias = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NumeroParticipacoesAusencias));
                }
            }
        }

        [JsonIgnore] // Esta é uma propriedade calculada para a UI, não precisa ser serializada.
        public string NumeroParticipacoesAusencias => $"Participações: {Participacoes} / Ausências: {Ausencias}";

        // Métodos de incremento que a UI chamará, atualizando o DTO
        public void IncrementarParticipacoes()
        {
            Participacoes++;
        }

        public void IncrementarAusencias()
        {
            Ausencias++;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
