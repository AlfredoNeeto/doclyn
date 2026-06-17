using Doclyn.Application.Common.DTOs.Auth;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Auth.Register;

public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResponse> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // User é exposto como DbSet via IApplicationDbContext
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
            throw new InvalidOperationException("Email already registered.");

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.Name,
            normalizedEmail,
            passwordHash,
            request.Role);

        _context.Users.Add(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new RegisterUserResponse(user.ToDto());
    }
}
