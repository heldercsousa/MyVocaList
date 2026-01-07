using MyVocaList.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyVocaList.Infra.Data.Config
{
    /// <summary>
    /// Entity Framework configuration for EventParticipation entity
    /// </summary>
    public class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
    {
        public void Configure(EntityTypeBuilder<EventParticipation> builder)
        {
            builder.HasKey(pe => pe.Id);
            builder.Property(pe => pe.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(pe => pe.Timestamp)
                   .IsRequired();

            builder.Property(pe => pe.Status)
                   .IsRequired();

            builder.HasOne(pe => pe.Person)
                   .WithMany()
                   .HasForeignKey(pe => pe.PersonId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pe => pe.Event)
                   .WithMany(e => e.Participations)
                   .HasForeignKey(pe => pe.EventId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
