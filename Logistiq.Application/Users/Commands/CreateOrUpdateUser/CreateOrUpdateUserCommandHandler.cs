using MediatR;
using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Common.Models;
using Logistiq.Domain.Entities;

namespace Logistiq.Application.Users.Commands.CreateOrUpdateUser;

public class CreateOrUpdateUserCommandHandler : IRequestHandler<CreateOrUpdateUserCommand, Result<UserResult>>
{
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrUpdateUserCommandHandler(
        IRepository<ApplicationUser> userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserResult>> Handle(CreateOrUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var clerkUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(clerkUserId))
        {
            return Result<UserResult>.Failure("User not authenticated");
        }

        // Check if user already exists
        var existingUser = await _userRepository.GetQueryable()
            .Include(u => u.CompanyUsers)
                .ThenInclude(cu => cu.Company)
            .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId, cancellationToken);

        if (existingUser != null)
        {
            // User exists - update their info if needed
            existingUser.Email = request.Email;
            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.Phone = request.Phone;

            await _userRepository.UpdateAsync(existingUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var currentCompanyId = _currentUserService.CompanyId;
            var activeCompanyUser = existingUser.CompanyUsers.FirstOrDefault(cu => cu.IsActive && cu.CompanyId == currentCompanyId);

            return Result<UserResult>.Success(new UserResult
            {
                UserId = existingUser.Id,
                Email = existingUser.Email,
                FullName = $"{existingUser.FirstName} {existingUser.LastName}",
                IsNewUser = false,
                HasActiveCompany = existingUser.CompanyUsers.Any(cu => cu.IsActive),
                CurrentCompanyId = activeCompanyUser?.CompanyId,
                CurrentCompanyName = activeCompanyUser?.Company?.Name
            });
        }

        // Create new user
        var newUser = new ApplicationUser
        {
            ClerkUserId = clerkUserId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = true
        };

        await _userRepository.AddAsync(newUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserResult>.Success(new UserResult
        {
            UserId = newUser.Id,
            Email = newUser.Email,
            FullName = $"{newUser.FirstName} {newUser.LastName}",
            IsNewUser = true,
            HasActiveCompany = false,
            CurrentCompanyId = null,
            CurrentCompanyName = null
        });
    }
}