using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.UnitTests.Common;

public sealed class TestDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ExtractedData> ExtractedData => Set<ExtractedData>();
    public DbSet<ProcessingLog> ProcessingLogs => Set<ProcessingLog>();
    public DbSet<DocumentClass> DocumentClasses => Set<DocumentClass>();
    public DbSet<DocumentClassExample> DocumentClassExamples => Set<DocumentClassExample>();
    public DbSet<DocumentClassIndexer> DocumentClassIndexers => Set<DocumentClassIndexer>();
    public DbSet<DocumentInsight> DocumentInsights => Set<DocumentInsight>();

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DoclynDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
