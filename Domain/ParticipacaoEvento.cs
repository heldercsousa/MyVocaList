namespace MyVocaList.Domain;

public class ParticipacaoEvento
{
    public int Id { get; set; }
    public int PessoaId { get; set; } // Chave estrangeira
    public int EventoId { get; set; } // Chave estrangeira
    public DateTime Timestamp { get; set; } // Quando a ação ocorreu
    public ParticipacaoStatus Status { get; set; } // Status da participação

    public Pessoa Pessoa { get; set; } // Propriedade de navegação
    public Evento Evento { get; set; } // Propriedade de navegação
}

public enum ParticipacaoStatus
{
    Ausente = 0,
    Presente = 1
}
