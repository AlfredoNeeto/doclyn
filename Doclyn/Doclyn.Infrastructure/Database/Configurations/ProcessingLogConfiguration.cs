using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doclyn.Infrastructure.Database.Configurations;

public sealed class ProcessingLogConfiguration : IEntityTypeConfiguration<ProcessingLog>
{
    public void Configure(EntityTypeBuilder<ProcessingLog> builder)
    {
        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.Step)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pl => pl.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(pl => pl.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(pl => pl.CreatedAt)
            .IsRequired();

        builder.HasOne(pl => pl.Document)
            .WithMany()
            .HasForeignKey(pl => pl.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pl => pl.DocumentId);
    }
}
