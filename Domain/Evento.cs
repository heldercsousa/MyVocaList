namespace MyVocaList.Domain;

public class Evento
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; } // Chave estrangeira
    public DateTime DataEvento { get; set; }
    public string NomeEvento { get; set; }
    public bool FilaAtiva { get; set; }

    public Estabelecimento Estabelecimento { get; set; } // Propriedade de navegação
    public ICollection<ParticipacaoEvento> Participacoes { get; set; } // Propriedade de navegação
}
