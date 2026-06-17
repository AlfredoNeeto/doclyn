using Doclyn.Application.Common.Interfaces;

namespace Doclyn.UnitTests.Common;

public sealed class CurrentUserServiceMock : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
}
