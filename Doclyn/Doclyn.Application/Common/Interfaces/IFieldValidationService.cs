using Doclyn.Domain.Enums;

namespace Doclyn.Application.Common.Interfaces;

public interface IFieldValidationService
{
    ValidationStatus DetermineStatus(decimal confidence);
}
