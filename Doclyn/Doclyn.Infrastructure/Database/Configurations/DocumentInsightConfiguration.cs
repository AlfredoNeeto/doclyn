using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doclyn.Infrastructure.Database.Configurations;

public sealed class DocumentInsightConfiguration : IEntityTypeConfiguration<DocumentInsight>
{
    public void Configure(EntityTypeBuilder<DocumentInsight> builder)
    {
        builder.HasKey(di => di.Id);

        builder.Property(di => di.DocumentId)
            .IsRequired();

        builder.Property(di => di.Type)
            .IsRequired();

        builder.Property(di => di.Severity)
            .IsRequired();

        builder.Property(di => di.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(di => di.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(di => di.Confidence)
            .IsRequired();

        builder.Property(di => di.Source)
            .IsRequired();

        builder.Property(di => di.RelatedFieldName)
            .HasMaxLength(100);

        builder.Property(di => di.CreatedAt)
            .IsRequired();

        builder.HasOne(di => di.Document)
            .WithMany()
            .HasForeignKey(di => di.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(di => di.DocumentId);
        builder.HasIndex(di => di.Type);
        builder.HasIndex(di => di.Severity);
        builder.HasIndex(di => di.CreatedAt);
    }
}
