using MediatR;
using Logistiq.Application.Common.Interfaces;
using Logistiq.Application.Common.Models;
using Logistiq.Domain.Entities;

namespace Logistiq.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly ITenantRepository<Product> _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        ITenantRepository<Product> productRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var companyId = await _currentUserService.GetCurrentCompanyIdAsync();
        if (companyId == null)
        {
            return Result<Guid>.Failure("Company not found");
        }

        // Check if SKU already exists
        var existingProduct = await _productRepository.FirstOrDefaultAsync(
            p => p.Sku == request.Sku,
            companyId.Value,
            cancellationToken);

        if (existingProduct != null)
        {
            return Result<Guid>.Failure("Product with this SKU already exists");
        }

        var product = new Product
        {
            CompanyId = companyId.Value,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,

            CategoryId = request.CategoryId,
            Price = request.Price,
            CostPrice = request.CostPrice,
            StockQuantity = request.StockQuantity,
            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,
            CreatedBy = _currentUserService.UserId
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}