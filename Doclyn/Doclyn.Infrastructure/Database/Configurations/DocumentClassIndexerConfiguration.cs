using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doclyn.Infrastructure.Database.Configurations;

public sealed class DocumentClassIndexerConfiguration : IEntityTypeConfiguration<DocumentClassIndexer>
{
    public void Configure(EntityTypeBuilder<DocumentClassIndexer> builder)
    {
        builder.HasKey(dci => dci.Id);

        builder.Property(dci => dci.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(dci => dci.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(dci => dci.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(dci => dci.DataType)
            .IsRequired();

        builder.Property(dci => dci.IsRequired)
            .IsRequired();

        builder.Property(dci => dci.IsMultiple)
            .IsRequired();

        builder.Property(dci => dci.ExtractionHint)
            .HasMaxLength(500);

        builder.Property(dci => dci.RegexPattern)
            .HasMaxLength(2000);

        builder.Property(dci => dci.IsActive)
            .IsRequired();

        builder.Property(dci => dci.CreatedAt)
            .IsRequired();

        builder.HasOne(dci => dci.DocumentClass)
            .WithMany(dc => dc.Indexers)
            .HasForeignKey(dci => dci.DocumentClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dci => dci.DocumentClassId);
        builder.HasIndex(dci => dci.Name);
        builder.HasIndex(dci => dci.IsActive);

        builder.HasIndex(dci => new { dci.DocumentClassId, dci.Name, dci.IsActive })
            .IsUnique();
    }
}
