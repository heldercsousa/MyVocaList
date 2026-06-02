using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig;

public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ToTable("Songs");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.ArtistId).IsRequired();

        builder.Property(s => s.Title)
               .IsRequired()
               .HasMaxLength(100)
               .UseCollation("NOCASE_NOACCENT");

        builder.Property(s => s.FeaturedArtists)
               .IsRequired(false);

        builder.Property(s => s.ExternalId)
               .IsRequired(false)
               .HasMaxLength(100);

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

        builder.HasIndex(s => new { s.ArtistId, s.Title })
               .IsUnique()
               .HasDatabaseName("IX_Songs_ArtistId_Title");

        builder.HasIndex(s => s.ExternalId)
               .HasDatabaseName("IX_Songs_ExternalId");
    }
}
