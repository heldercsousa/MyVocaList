using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig
{
    /// <summary>
    /// Entity Framework configuration for SystemConfiguration entity
    /// </summary>
    public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
    {
        public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id).ValueGeneratedOnAdd();

            builder.Property(p => p.Key)
                .HasColumnType("varchar(50)")
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Value)
                .HasColumnType("varchar(200)")
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}
