namespace Doclyn.Application.Common.Interfaces;

/// <summary>
/// Abstrai a transação de banco de dados.
/// Use para agrupar múltiplas operações em uma única transação atômica.
/// </summary>
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
