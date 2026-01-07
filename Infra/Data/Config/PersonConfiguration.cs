using MyVocaList.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyVocaList.Infra.Data.Config
{
    /// <summary>
    /// Entity Framework configuration for Person entity
    /// </summary>
    public class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                   .ValueGeneratedOnAdd();

            // HYBRID APPROACH: Super safe database with TEXT(250)
            builder.Property(p => p.FullName)
                   .HasColumnType("TEXT")
                   .IsRequired()
                   .HasMaxLength(250); // Super safe for extremely long names

            // Normalized column also TEXT(250) for maximum safety
            builder.Property(p => p.FullNameNormalized)
                   .HasColumnType("TEXT")
                   .HasMaxLength(250) // Same capacity as original column
                   .IsRequired(false); // Can be null during migration

            builder.Property(p => p.Participations)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(p => p.Absences)
                   .IsRequired()
                   .HasDefaultValue(0);

            // Optimized index for fast search by normalized name
            builder.HasIndex(p => p.FullNameNormalized)
                   .HasDatabaseName("IX_People_FullNameNormalized");
        }
    }
}
