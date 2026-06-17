namespace Doclyn.Application.Common.Interfaces;

public interface IFileHashService
{
    Task<string> ComputeSha256Async(
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
