using Doclyn.Application.Documents;
using Doclyn.Application.Documents.Upload;
using Microsoft.Extensions.Options;

namespace Doclyn.UnitTests.Documents;

public sealed class UploadDocumentValidatorTests
{
    private readonly UploadDocumentValidator _validator;

    public UploadDocumentValidatorTests()
    {
        var options = Options.Create(new DocumentOptions { MaxUploadSizeInMb = 10 });
        _validator = new UploadDocumentValidator(options);
    }

    [Fact]
    public void Should_Reject_Null_File_Stream()
    {
        var command = new UploadDocumentCommand(null!, "file.pdf", "application/pdf", 1024);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.FileStream));
    }

    [Fact]
    public void Should_Reject_Empty_File_Name()
    {
        var command = new UploadDocumentCommand(Stream.Null, "", "application/pdf", 1024);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.FileName));
    }

    [Theory]
    [InlineData("file.txt")]
    [InlineData("file.docx")]
    [InlineData("file.pdf.exe")]
    public void Should_Reject_Non_Pdf_Extension(string fileName)
    {
        var command = new UploadDocumentCommand(Stream.Null, fileName, "application/pdf", 1024);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.FileName));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("image/png")]
    [InlineData("application/octet-stream")]
    public void Should_Reject_Non_Pdf_Content_Type(string contentType)
    {
        var command = new UploadDocumentCommand(Stream.Null, "file.pdf", contentType, 1024);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.ContentType));
    }

    [Fact]
    public void Should_Reject_Empty_File()
    {
        var command = new UploadDocumentCommand(Stream.Null, "file.pdf", "application/pdf", 0);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Length));
    }

    [Fact]
    public void Should_Reject_File_Above_Max_Size()
    {
        var command = new UploadDocumentCommand(Stream.Null, "file.pdf", "application/pdf", 11 * 1024 * 1024);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Length));
    }

    [Fact]
    public void Should_Accept_Valid_Pdf()
    {
        var command = new UploadDocumentCommand(Stream.Null, "file.pdf", "application/pdf", 1024);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
