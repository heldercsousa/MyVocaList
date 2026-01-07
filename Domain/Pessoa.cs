using System.Text;
using System.Text.RegularExpressions;

namespace MyVocaList.Domain;

public class Pessoa
{
    public int Id { get; set; } // Chave primária para o BD
    public string NomeCompleto { get; set; }
    public string NomeCompletoNormalizado { get; set; } // Para busca otimizada
    public int Participacoes { get; set; } = 0; // Contador de participações
    public int Ausencias { get; set; } = 0; // Contador de ausências

    // Campos para diferenciação inteligente
    public string DiaMesAniversario { get; set; } // Formato: "15/03" (obrigatório)
    public string Email { get; set; } // Opcional para marketing/diferenciação

    public Pessoa(string nomeCompleto)
    {
        NomeCompleto = nomeCompleto;
        NomeCompletoNormalizado = string.Empty; // Será definido pelo serviço
    }

    public Pessoa() { } // Construtor sem argumentos para EF Core

    // APENAS MÉTODOS DE DOMÍNIO (sem validação ou normalização)

    /// <summary>
    /// Gera identificador único para display
    /// </summary>
    public string GetDisplayIdentifier()
    {
        // Prioridade: E-mail > Aniversário > Fallback
        if (!string.IsNullOrWhiteSpace(Email))
        {
            return Email.ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(DiaMesAniversario))
        {
            return $"({DiaMesAniversario})";
        }

        return $"(ID: {Id})"; // Fallback extremo
    }

    /// <summary>
    /// Formata nome para exibição nas sugestões
    /// </summary>
    public string GetDisplayName()
    {
        var identifier = GetDisplayIdentifier();

        // Se for e-mail, mostra embaixo do nome
        if (!string.IsNullOrWhiteSpace(Email) && identifier == Email.ToLowerInvariant())
        {
            return NomeCompleto; // E-mail vai numa linha separada
        }

        // Se for aniversário ou ID, mostra junto do nome
        return $"{NomeCompleto} {identifier}";
    }

    /// <summary>
    /// Atualiza a normalização quando o nome muda (será chamado pelo serviço)
    /// </summary>
    public void SetNormalizedName(string normalizedName)
    {
        NomeCompletoNormalizado = normalizedName;
    }
}
