using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Contracts.Enums;
using MyVocaList.Infra.Collation;
using Event = MyVocaList.Domain.Entities.Event;

namespace MyVocaList.Infra.EntityEFConfig
{
    /// <summary>
    /// Entity Framework configuration for Queue Management Event entity
    /// </summary>
    public class QueueManagementEventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("QueueManagementEvents");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.Name)
                   .IsRequired()
                   .HasMaxLength(100)
                   .UseCollation(CollationConstants.Default);

            builder.Property(e => e.ScheduledStartTime);

            builder.Property(e => e.ScheduledEndTime);

            builder.Property(e => e.ActualStartTime);

            builder.Property(e => e.ActualEndTime);

            builder.Property(e => e.Status)
                   .IsRequired()
                   .HasDefaultValue(EventStatus.Created);

            builder.Property(e => e.Mode)
                   .HasDefaultValue("VideoKaraoke")
                   .HasMaxLength(50);

            builder.Property(e => e.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(e => e.ModifiedAt)
                   .IsRequired()
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique index: Event name per venue per date (only for non-finished events)
            builder.HasIndex(e => new { e.VenueId, e.Name, e.ScheduledStartTime })
                   .IsUnique()
                   .HasDatabaseName("IX_QueueManagementEvents_UniqueName")
                   .HasFilter("[Status] <> 3"); // Filter out FINISHED status

            // Foreign key to Venue
            builder.HasOne(e => e.Venue)
                   .WithMany()
                   .HasForeignKey(e => e.VenueId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Event has many QueueEntries
            builder.HasMany(e => e.QueueEntries)
                   .WithOne(qe => qe.Event)
                   .HasForeignKey(qe => qe.EventId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
