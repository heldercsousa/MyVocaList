using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Rules;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.EntityEFConfig;

public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ToTable("Songs", tb => tb.UseSqlReturningClause(false));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.ArtistId).IsRequired();

        // D3 (design.md § D3): trim-on-save enforced here, not in SongService Create/Update.
        builder.Property(s => s.Title)
               .IsRequired()
               .HasMaxLength(250)
               .HasConversion(TrimValueConverters.Required)
               .UseCollation(CollationConstants.Default);

        // D3 (design.md § D3): trim-on-save enforced here, not in SongService Create/Update.
        // Version is non-nullable ("" = canonical version) — TrimForStorage never nulls it.
        builder.Property(s => s.Version)
               .IsRequired()
               .HasMaxLength(60)
               .HasConversion(TrimValueConverters.Required)
               .UseCollation(CollationConstants.Default);

        // D3 (design.md § D3): trim-on-save enforced here, not in SongService Create/Update.
        builder.Property(s => s.FeaturedArtists)
               .IsRequired(false)
               .HasConversion(TrimValueConverters.Optional);

        // D3 (design.md § D3): trim-on-save enforced here, not in SongService Create/Update.
        builder.Property(s => s.ExternalId)
               .IsRequired(false)
               .HasMaxLength(100)
               .HasConversion(TrimValueConverters.Optional);

        builder.Property(s => s.ExternalProvider)
               .IsRequired(false)
               .HasMaxLength(50);

        // REQ-ART-05: URL only, nullable, never searched (no index, no collation).
        builder.Property(s => s.ImageUrl)
               .IsRequired(false)
               .HasMaxLength(ImageUrlRules.MaxLength);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.Property(s => s.Lyrics)
               .IsRequired(false)
               .HasColumnType("TEXT")
               .HasMaxLength(10000);

        builder.HasIndex(s => new { s.ArtistId, s.Title, s.Version })
               .IsUnique()
               .HasDatabaseName("IX_Songs_ArtistId_Title_Version");

        builder.HasIndex(s => new { s.ExternalProvider, s.ExternalId })
               .IsUnique();
    }
}
