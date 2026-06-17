using System.Text;
using Doclyn.Infrastructure.Storage;

namespace Doclyn.UnitTests.Storage;

public sealed class FileHashServiceTests
{
    private readonly FileHashService _service = new();

    [Fact]
    public async Task Should_Generate_Sha256_In_Lowercase_Hex()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));

        var hash = await _service.ComputeSha256Async(stream, CancellationToken.None);

        Assert.Equal("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", hash);
    }

    [Fact]
    public async Task Should_Reset_Stream_Position_After_Hash_Calculation()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hash me"));
        stream.Position = 2;

        await _service.ComputeSha256Async(stream, CancellationToken.None);

        Assert.Equal(2, stream.Position);
    }
}
