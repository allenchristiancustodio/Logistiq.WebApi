using MediatR;
using Microsoft.EntityFrameworkCore;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Common.Models;
using Logistiq.Domain.Entities;

namespace Logistiq.Application.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IRepository<ApplicationUser> userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var clerkUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(clerkUserId))
        {
            return Result<CurrentUserDto>.Failure("User not authenticated");
        }

        var user = await _userRepository.GetQueryable()
            .Include(u => u.CompanyUsers.Where(cu => cu.IsActive))
                .ThenInclude(cu => cu.Company)
            .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId, cancellationToken);

        if (user == null)
        {
            return Result<CurrentUserDto>.Failure("User not found");
        }

        var currentCompanyId = _currentUserService.CompanyId;
        var currentCompanyUser = user.CompanyUsers.FirstOrDefault(cu => cu.CompanyId == currentCompanyId);

        var userDto = new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            Phone = user.Phone,
            HasActiveCompany = user.CompanyUsers.Any(),
            CurrentCompanyId = currentCompanyUser?.CompanyId,
            CurrentCompanyName = currentCompanyUser?.Company?.Name,
            Companies = user.CompanyUsers
                .Where(cu => cu.Company.IsActive)
                .Select(cu => new UserCompanyInfo
                {
                    CompanyId = cu.CompanyId,
                    CompanyName = cu.Company.Name,
                    Role = cu.Role.ToString(),
                    IsActive = cu.IsActive
                })
                .ToList()
        };

        return Result<CurrentUserDto>.Success(userDto);
    }
}