using MediatR;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Common.Models;
using Logistiq.Domain.Entities;
using Logistiq.Domain.Enums;

namespace Logistiq.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CreateCompanyResult>>
{
    private readonly IRepository<Company> _companyRepository;
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly IRepository<CompanyUser> _companyUserRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCompanyCommandHandler(
        IRepository<Company> companyRepository,
        IRepository<ApplicationUser> userRepository,
        IRepository<CompanyUser> companyUserRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _companyUserRepository = companyUserRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateCompanyResult>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var clerkUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(clerkUserId))
        {
            return Result<CreateCompanyResult>.Failure("User not authenticated");
        }

        // Get the current user
        var user = await _userRepository.FirstOrDefaultAsync(
            u => u.ClerkUserId == clerkUserId, cancellationToken);

        if (user == null)
        {
            return Result<CreateCompanyResult>.Failure("User not found");
        }

        // Create the company
        var company = new Company
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            Website = request.Website,
            IsActive = true
        };

        await _companyRepository.AddAsync(company, cancellationToken);

        // Create the company-user relationship (user becomes owner)
        var companyUser = new CompanyUser
        {
            ApplicationUserId = user.Id,
            CompanyId = company.Id,
            Role = CompanyUserRole.Owner,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        await _companyUserRepository.AddAsync(companyUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // TODO: Generate new JWT token with company claim
        // For now, return empty token - you'll need to implement JWT service
        var newToken = ""; // await _jwtTokenService.GenerateTokenWithCompanyClaim(user, company.Id);

        return Result<CreateCompanyResult>.Success(new CreateCompanyResult
        {
            CompanyId = company.Id,
            CompanyName = company.Name,
            NewToken = newToken
        });
    }
}