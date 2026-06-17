using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Infrastructure.Database.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Infrastructure.Database;

/// <summary>
/// DbContext principal da aplicação.
/// Implementa IApplicationDbContext (contrato da camada Application) e IUnitOfWork.
/// As configurações de entidade ficam em Database/Configurations/ como IEntityTypeConfiguration&lt;T&gt;.
/// </summary>
public sealed class DoclynDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    public DoclynDbContext(DbContextOptions<DoclynDbContext> options)
        : base(options)
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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Aplica a convenção global UPPER_SNAKE_CASE para tabelas, colunas, chaves e índices
        configurationBuilder.Conventions.Add(_ => new UpperSnakeCaseConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica automaticamente todas as IEntityTypeConfiguration<T> do assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DoclynDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    // IUnitOfWork
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => await SaveChangesAsync(cancellationToken);
}
