using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.EntityEFConfig;

public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ToTable("Songs");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.ArtistId).IsRequired();

        // D3 (design.md § D3): trim-on-save enforced here, not in SongService Create/Update.
        builder.Property(s => s.Title)
               .IsRequired()
               .HasMaxLength(100)
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

        builder.Property(s => s.HasManualEdits).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.Property(s => s.Lyrics)
               .IsRequired(false)
               .HasColumnType("TEXT")
               .HasMaxLength(10000);

        builder.HasIndex(s => new { s.ArtistId, s.Title, s.Version })
               .IsUnique()
               .HasDatabaseName("IX_Songs_ArtistId_Title_Version");

        builder.HasIndex(s => s.ExternalId)
               .HasDatabaseName("IX_Songs_ExternalId");
    }
}
