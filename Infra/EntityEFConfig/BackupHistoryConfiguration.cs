using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig;

public class BackupHistoryConfiguration : IEntityTypeConfiguration<BackupHistory>
{
    public void Configure(EntityTypeBuilder<BackupHistory> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.TriggerType).IsRequired().HasConversion<string>();
        builder.Property(b => b.BackupType).IsRequired().HasConversion<string>();
        builder.Property(b => b.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(b => b.FileSizeBytes).IsRequired();
        builder.Property(b => b.MirrorStatus).IsRequired().HasConversion<string>();
        builder.HasIndex(b => b.CreatedAt);
    }
}
