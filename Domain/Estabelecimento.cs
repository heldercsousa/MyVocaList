namespace MyVocaList.Domain;

public class Estabelecimento
{
    public int Id { get; set; }
    public string Nome { get; set; }

    public ICollection<Evento> Eventos { get; set; } // Propriedade de navegação
}
