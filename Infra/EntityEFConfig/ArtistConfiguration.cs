using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.EntityEFConfig;

public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("Artists");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        // D3 (design.md § D3): trim-on-save enforced here, not in ArtistService Create/Update.
        builder.Property(a => a.Name)
               .IsRequired()
               .HasMaxLength(60)
               .HasConversion(TrimValueConverters.Required)
               .UseCollation(CollationConstants.Default);

        // D3 (design.md § D3): trim-on-save enforced here, not in ArtistService Create/Update.
        builder.Property(a => a.ExternalId)
               .IsRequired(false)
               .HasMaxLength(100)
               .HasConversion(TrimValueConverters.Optional);

        builder.Property(a => a.ExternalProvider)
               .IsRequired(false)
               .HasMaxLength(50);

        builder.Property(a => a.HasManualEdits)
               .IsRequired();

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => a.Name)
               .IsUnique()
               .HasDatabaseName("IX_Artists_Name");

        builder.HasIndex(a => a.ExternalId)
               .IsUnique()
               .HasDatabaseName("IX_Artists_ExternalId");

        builder.HasMany(a => a.OriginalSongs)
               .WithOne(s => s.OriginalArtist)
               .HasForeignKey(s => s.ArtistId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
