using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig;

public class CatalogConfiguration : IEntityTypeConfiguration<Catalog>
{
    public void Configure(EntityTypeBuilder<Catalog> builder)
    {
        builder.ToTable("Catalog");
        builder.HasKey(c => new { c.ArtistId, c.SongId });

        builder.HasOne(c => c.Artist)
               .WithMany(a => a.CatalogEntries)
               .HasForeignKey(c => c.ArtistId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Song)
               .WithMany(s => s.CatalogEntries)
               .HasForeignKey(c => c.SongId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
