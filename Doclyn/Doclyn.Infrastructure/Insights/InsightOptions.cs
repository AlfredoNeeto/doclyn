namespace Doclyn.Infrastructure.Insights;

public sealed class InsightOptions
{
    public const string Section = "Insights";

    public bool Enabled { get; init; } = true;
    public int ContractExpiringSoonDays { get; init; } = 30;
    public decimal LowConfidenceThreshold { get; init; } = 0.70m;
    public decimal HighValueThreshold { get; init; } = 50000m;
    public bool EnableAiInsights { get; init; } = true;
}
