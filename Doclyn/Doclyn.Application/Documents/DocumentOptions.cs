namespace Doclyn.Application.Documents;

/// <summary>
/// Configurações do módulo de documentos lidas de appsettings.json → seção "Documents".
/// </summary>
public sealed class DocumentOptions
{
    public const string Section = "Documents";

    public int MaxUploadSizeInMb { get; init; } = 10;
}
