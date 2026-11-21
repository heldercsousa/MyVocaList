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

        // REQ-BDAY-07: optional day and month, stored as INTEGER. Both null, or both set to a valid day/month.
        builder.Property(p => p.BirthDay)
               .HasColumnType("INTEGER").IsRequired(false);

        builder.Property(p => p.BirthMonth)
               .HasColumnType("INTEGER").IsRequired(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Persons_Birthday",
                "(BirthDay IS NULL AND BirthMonth IS NULL) OR (BirthDay IS NOT NULL AND BirthMonth IS NOT NULL AND BirthDay BETWEEN 1 AND 31 AND BirthMonth BETWEEN 1 AND 12)");
        });

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

        // BUG-088 identity: unique index IX_Persons_Identity (FullName, COALESCE(Email), COALESCE(BirthDay/BirthMonth)).
        // It is an expression index that EF HasIndex cannot express, so it is created ONLY by raw SQL in the newest
        // migration that creates it, PersonIdentityUniqueIndexRestore (the older PersonIdentityUniqueIndex created it
        // before a later table rebuild). Do not re-add an equivalent model index here. Any later migration
        // that rebuilds the People table drops it and must re-create it (the PersonIdentityIndexTests guard checks).
    }
}
