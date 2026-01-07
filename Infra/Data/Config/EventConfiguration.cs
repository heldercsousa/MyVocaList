using MyVocaList.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyVocaList.Infra.Data.Config
{
    /// <summary>
    /// Entity Framework configuration for Event entity
    /// </summary>
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.EventDate)
                   .IsRequired();

            builder.Property(e => e.EventName)
                   .HasColumnType("nvarchar(50)")
                   .HasMaxLength(50);

            builder.Property(e => e.QueueActive)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.HasOne(e => e.Venue)
                   .WithMany(v => v.Events)
                   .HasForeignKey(e => e.VenueId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
