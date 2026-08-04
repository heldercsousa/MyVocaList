using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.EntityEFConfig;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.ExternalId)
               .IsRequired(false);

        // D3 (design.md § D3): trim-on-save enforced here, not in PersonService Create/Update.
        builder.Property(p => p.FullName)
               .HasColumnType("TEXT").IsRequired().HasMaxLength(250)
               .HasConversion(TrimValueConverters.Required)
               .UseCollation(CollationConstants.Default);

        builder.Property(p => p.BirthdayDayMonth)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(5);

        // D3 (design.md § D3): trim-on-save enforced here, not in PersonService Create/Update.
        builder.Property(p => p.Email)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(100)
               .HasConversion(TrimValueConverters.Optional);

        builder.Property(p => p.Participations)
               .IsRequired().HasDefaultValue(0);

        builder.Property(p => p.Absences)
               .IsRequired().HasDefaultValue(0);

        builder.HasIndex(p => p.FullName)
               .HasDatabaseName("IX_Persons_FullName");

        builder.HasIndex(p => p.Email)
               .IsUnique()
               .HasDatabaseName("IX_Persons_Email");

        builder.HasIndex(p => p.ExternalId)
               .IsUnique()
               .HasDatabaseName("IX_Persons_ExternalId");

        builder.HasIndex(p => new { p.FullName, p.BirthdayDayMonth })
               .IsUnique()
               .HasFilter("[BirthdayDayMonth] IS NOT NULL")
               .HasDatabaseName("IX_Persons_Name_Birthday");
    }
}
