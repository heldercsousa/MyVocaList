using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.EntityEFConfig
{
    /// <summary>
    /// Entity Framework configuration for Venue entity
    /// </summary>
    public class VenueConfiguration : IEntityTypeConfiguration<Venue>
    {
        public void Configure(EntityTypeBuilder<Venue> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.Name)
                   .HasColumnType("TEXT")
                   .IsRequired()
                   .HasMaxLength(30) // Multilingual support: EN, PT, ES, FR, JA, KO
                   .UseCollation(CollationConstants.Default);
        }
    }
}
