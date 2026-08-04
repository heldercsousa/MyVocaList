using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Contracts.Enums;
using QueueEntry = MyVocaList.Domain.Entities.QueueEntry;

namespace MyVocaList.Infra.EntityEFConfig
{
    /// <summary>
    /// Entity Framework configuration for QueueEntry entity
    /// </summary>
    public class QueueEntryConfiguration : IEntityTypeConfiguration<QueueEntry>
    {
        public void Configure(EntityTypeBuilder<QueueEntry> builder)
        {
            builder.HasKey(qe => qe.Id);

            builder.Property(qe => qe.Id).ValueGeneratedOnAdd();

            builder.Property(qe => qe.EventId)
                   .IsRequired();

            builder.Property(qe => qe.PersonId)
                   .IsRequired();

            builder.Property(qe => qe.SongId);

            builder.Property(qe => qe.Position)
                   .IsRequired();

            builder.Property(qe => qe.Status)
                   .IsRequired()
                   .HasDefaultValue(QueueEntryStatus.Pending);

            builder.Property(qe => qe.PerformanceStartTime);

            builder.Property(qe => qe.PerformanceEndTime);

            builder.Property(qe => qe.PerformanceDurationMinutes);

            builder.Property(qe => qe.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(qe => qe.ModifiedAt)
                   .IsRequired()
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Foreign key to Event
            builder.HasOne(qe => qe.Event)
                   .WithMany(e => e.QueueEntries)
                   .HasForeignKey(qe => qe.EventId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Person
            builder.HasOne(qe => qe.Person)
                   .WithMany()
                   .HasForeignKey(qe => qe.PersonId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to Song (optional)
            builder.HasOne(qe => qe.Song)
                   .WithMany()
                   .HasForeignKey(qe => qe.SongId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);

            // Unique index: Person can only appear once per event in non-cancelled entries
            builder.HasIndex(qe => new { qe.EventId, qe.PersonId })
                   .IsUnique()
                   .HasDatabaseName("IX_QueueEntries_UniquePersonPerEvent")
                   .HasFilter("[Status] <> 4"); // Filter out CANCELLED status

            // Index for querying by event and ordering by position
            builder.HasIndex(qe => new { qe.EventId, qe.Position })
                   .HasDatabaseName("IX_QueueEntries_EventPosition");

            // Index for performance tracking queries
            builder.HasIndex(qe => new { qe.EventId, qe.Status })
                   .HasDatabaseName("IX_QueueEntries_EventStatus");
        }
    }
}
