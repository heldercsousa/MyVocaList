using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig;

public class SongKaraokeUrlConfiguration : IEntityTypeConfiguration<SongKaraokeUrl>
{
    public void Configure(EntityTypeBuilder<SongKaraokeUrl> builder)
    {
        builder.ToTable("SongKaraokeUrls");
        builder.HasKey(u => new { u.SongId, u.VideoId });

        builder.Property(u => u.VideoId)
               .HasColumnType("TEXT")
               .IsRequired()
               .HasMaxLength(11);

        builder.Property(u => u.SongId).IsRequired();

        builder.Property(u => u.PlayCount)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(u => u.DurationSeconds).IsRequired(false);
        builder.Property(u => u.LastUsedAt).IsRequired(false);

        builder.Property(u => u.AddedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.Label)
               .HasColumnType("TEXT")
               .IsRequired(false)
               .HasMaxLength(100);

        builder.HasOne(u => u.Song)
               .WithMany()
               .HasForeignKey(u => u.SongId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
