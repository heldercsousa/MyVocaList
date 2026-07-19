using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyVocaList.Services.Text;

namespace MyVocaList.Infra.EntityEFConfig;

/// <summary>
/// Shared EF Core <see cref="ValueConverter{TModel,TProvider}"/> instances for persisted-string
/// trimming (REQ-TRIM-05/06/07). Persistence-layer enforcement point for the D3 decision —
/// see <c>Docs/Management/DevCycleCraft/persisted-string-trimming/design.md § Decision points → D3</c>
/// for the full "why Infra, not Services" rationale. ToProvider delegates to
/// <see cref="StringNormalization"/> (Services) so the trimming algorithm itself stays owned by
/// Services; FromProvider is the identity function — reads are zero-cost passthrough.
/// </summary>
public static class TrimValueConverters
{
    /// <summary>Required name-like properties (non-nullable string). Applies <see cref="StringNormalization.TrimForStorage"/>.</summary>
    public static readonly ValueConverter<string, string> Required =
        new(v => StringNormalization.TrimForStorage(v), v => v);

    /// <summary>Optional name-like properties (nullable string). Applies <see cref="StringNormalization.TrimForStorageOrNull"/> — empty/whitespace-only → null.</summary>
    public static readonly ValueConverter<string?, string?> Optional =
        new(v => StringNormalization.TrimForStorageOrNull(v), v => v);
}
