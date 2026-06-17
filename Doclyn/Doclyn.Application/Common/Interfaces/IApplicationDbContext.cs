using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Common.Interfaces;

/// <summary>
/// Contrato do contexto de banco de dados da aplicação.
/// Expõe apenas o necessário para os casos de uso — sem vazar EF Core para o Domain.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetRequest> PasswordResetRequests { get; }
    DbSet<Document> Documents { get; }
    DbSet<ExtractedData> ExtractedData { get; }
    DbSet<ProcessingLog> ProcessingLogs { get; }
    DbSet<DocumentClass> DocumentClasses { get; }
    DbSet<DocumentClassExample> DocumentClassExamples { get; }
    DbSet<DocumentClassIndexer> DocumentClassIndexers { get; }
    DbSet<DocumentInsight> DocumentInsights { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
