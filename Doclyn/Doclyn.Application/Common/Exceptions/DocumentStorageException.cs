namespace Doclyn.Application.Common.Exceptions;

public sealed class DocumentStorageException : Exception
{
    public DocumentStorageException()
        : base("Could not store the document.")
    {
    }

    public DocumentStorageException(Exception innerException)
        : base("Could not store the document.", innerException)
    {
    }
}
