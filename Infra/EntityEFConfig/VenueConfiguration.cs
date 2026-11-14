using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Rules;
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

            // D3 (design.md § D3): trim-on-save enforced here, not in VenueService Create/Update.
            builder.Property(e => e.Name)
                   .HasColumnType("TEXT")
                   .IsRequired()
                   .HasMaxLength(60) // REQ-VEN-22: raised from 30 so a place name from the map fits; multilingual: EN, PT, ES, FR, JA, KO
                   .HasConversion(TrimValueConverters.Required)
                   .UseCollation(CollationConstants.Default);

            // REQ-VEN-01 / REQ-VEN-05: one nullable picture reference, same column as Songs and Artists (no index, no collation).
            builder.Property(e => e.ImageUrl)
                   .IsRequired(false)
                   .HasMaxLength(ImageUrlRules.MaxLength);

            // REQ-VEN-12: six optional address fields. Blank → null on save; default collation so that
            // duplicate checks compare case- and accent-insensitively (no index, design decision V7).
            builder.Property(e => e.AddressLine1)
                   .IsRequired(false)
                   .HasMaxLength(VenueAddressRules.Line1Max)
                   .HasConversion(TrimValueConverters.Optional)
                   .UseCollation(CollationConstants.Default);

            builder.Property(e => e.AddressLine2)
                   .IsRequired(false)
                   .HasMaxLength(VenueAddressRules.Line2Max)
                   .HasConversion(TrimValueConverters.Optional)
                   .UseCollation(CollationConstants.Default);

            builder.Property(e => e.City)
                   .IsRequired(false)
                   .HasMaxLength(VenueAddressRules.CityMax)
                   .HasConversion(TrimValueConverters.Optional)
                   .UseCollation(CollationConstants.Default);

            builder.Property(e => e.Region)
                   .IsRequired(false)
                   .HasMaxLength(VenueAddressRules.RegionMax)
                   .HasConversion(TrimValueConverters.Optional)
                   .UseCollation(CollationConstants.Default);

            builder.Property(e => e.PostalCode)
                   .IsRequired(false)
                   .HasMaxLength(VenueAddressRules.PostalCodeMax)
                   .HasConversion(TrimValueConverters.Optional)
                   .UseCollation(CollationConstants.Default);

            builder.Property(e => e.Country)
                   .IsRequired(false)
                   .HasMaxLength(VenueAddressRules.CountryMax)
                   .HasConversion(TrimValueConverters.Optional)
                   .UseCollation(CollationConstants.Default);

            // design § 2.3: optional coordinates of the pinned place, stored as REAL.
            builder.Property(e => e.Latitude)
                   .IsRequired(false)
                   .HasColumnType("REAL");

            builder.Property(e => e.Longitude)
                   .IsRequired(false)
                   .HasColumnType("REAL");
        }
    }
}
