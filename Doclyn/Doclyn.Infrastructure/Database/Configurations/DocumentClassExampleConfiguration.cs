using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doclyn.Infrastructure.Database.Configurations;

public sealed class DocumentClassExampleConfiguration : IEntityTypeConfiguration<DocumentClassExample>
{
    public void Configure(EntityTypeBuilder<DocumentClassExample> builder)
    {
        builder.HasKey(dce => dce.Id);

        builder.Property(dce => dce.Confidence)
            .IsRequired()
            .HasPrecision(5, 4);

        builder.Property(dce => dce.CreatedAt)
            .IsRequired();

        builder.HasOne(dce => dce.DocumentClass)
            .WithMany()
            .HasForeignKey(dce => dce.DocumentClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dce => dce.Document)
            .WithMany()
            .HasForeignKey(dce => dce.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dce => dce.DocumentClassId);
        builder.HasIndex(dce => dce.DocumentId);
    }
}
