using System.Security.Cryptography;
using Doclyn.Application.Common.Interfaces;

namespace Doclyn.Infrastructure.Storage;

public sealed class FileHashService : IFileHashService
{
    public async Task<string> ComputeSha256Async(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var originalPosition = fileStream.CanSeek ? fileStream.Position : 0;

        try
        {
            var hash = await SHA256.HashDataAsync(fileStream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        finally
        {
            if (fileStream.CanSeek)
            {
                fileStream.Position = originalPosition;
            }
        }
    }
}
