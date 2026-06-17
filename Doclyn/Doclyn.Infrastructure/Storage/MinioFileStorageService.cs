using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Doclyn.Infrastructure.Storage;

public sealed class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly StorageOptions _options;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(
        IMinioClient minioClient,
        IOptions<StorageOptions> options,
        ILogger<MinioFileStorageService> logger)
    {
        _minioClient = minioClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        // O MinIO precisa de um stream seekable para o PutObjectAsync.
        // IFormFile.OpenReadStream() geralmente é seekable, mas garantimos aqui.
        if (!fileStream.CanSeek)
        {
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            fileStream = memoryStream;
        }

        try
        {
            await EnsureBucketExistsAsync(cancellationToken);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            _logger.LogInformation(
                "Uploading object {ObjectName} to bucket {BucketName}",
                objectName,
                _options.BucketName);

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Object {ObjectName} uploaded successfully to bucket {BucketName}",
                objectName,
                _options.BucketName);

            return objectName;
        }
        catch (DocumentStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object {ObjectName} to bucket {BucketName}", objectName, _options.BucketName);
            throw new DocumentStorageException(ex);
        }
    }

    public async Task<Stream> DownloadAsync(
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        try
        {
            await EnsureBucketExistsAsync(cancellationToken);

            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (DocumentStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download object {ObjectName} from bucket {BucketName}", objectName, _options.BucketName);
            throw new DocumentStorageException(ex);
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (exists)
        {
            return;
        }

        _logger.LogInformation("Creating bucket {BucketName}", _options.BucketName);

        var makeBucketArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
        await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
    }
}
