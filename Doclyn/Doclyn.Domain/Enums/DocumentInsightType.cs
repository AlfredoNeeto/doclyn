namespace Doclyn.Domain.Enums;

public enum DocumentInsightType
{
    ContractExpired = 1,
    ContractExpiringSoon = 2,
    MissingRequiredField = 3,
    LowConfidenceField = 4,
    HighValueDocument = 5,
    InvalidIdentifier = 6,
    RiskMentioned = 7,
    PaymentSuspended = 8,
    LegalDeadlineMentioned = 9,
    ActionRequired = 10,
    Summary = 11,
    GenericObservation = 12
}
