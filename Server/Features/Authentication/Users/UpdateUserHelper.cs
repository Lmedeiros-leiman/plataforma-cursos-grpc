using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Users;

public static class UpdateUserHelper
{
    public static async Task<UserItem> HandleAsync(
        ChinookContext dbContext,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers
            .FirstOrDefaultAsync(item => item.CustomerId == request.CustomerId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "User not found."));

        await UserCatalogCommonHelper.EnsureSupportRepExistsAsync(dbContext, request.SupportRepId, cancellationToken);
        await UserCatalogCommonHelper.EnsureEmailIsUniqueAsync(dbContext, request.Email, request.CustomerId, cancellationToken);

        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.Company = UserCatalogCommonHelper.NormalizeOptional(request.Company);
        entity.Address = UserCatalogCommonHelper.NormalizeOptional(request.Address);
        entity.City = UserCatalogCommonHelper.NormalizeOptional(request.City);
        entity.State = UserCatalogCommonHelper.NormalizeOptional(request.State);
        entity.Country = UserCatalogCommonHelper.NormalizeOptional(request.Country);
        entity.PostalCode = UserCatalogCommonHelper.NormalizeOptional(request.PostalCode);
        entity.Phone = UserCatalogCommonHelper.NormalizeOptional(request.Phone);
        entity.Fax = UserCatalogCommonHelper.NormalizeOptional(request.Fax);
        entity.Email = request.Email.Trim();
        entity.SupportRepId = request.SupportRepId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return UserCatalogCommonHelper.ToItem(entity);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator(ChinookContext dbContext)
    {
        RuleFor(request => request.CustomerId)
            .GreaterThan(0)
            .MustAsync(async (customerId, cancellationToken) =>
                await dbContext.Customers.AsNoTracking().AnyAsync(entity => entity.CustomerId == customerId, cancellationToken))
            .WithMessage("O customer_id informado nao existe.");

        RuleFor(request => request.FirstName).NotEmpty().MaximumLength(20);
        RuleFor(request => request.LastName).NotEmpty().MaximumLength(20);
        RuleFor(request => request.Company).MaximumLength(80);
        RuleFor(request => request.Address).MaximumLength(70);
        RuleFor(request => request.City).MaximumLength(40);
        RuleFor(request => request.State).MaximumLength(40);
        RuleFor(request => request.Country).MaximumLength(40);
        RuleFor(request => request.PostalCode).MaximumLength(10);
        RuleFor(request => request.Phone).MaximumLength(24);
        RuleFor(request => request.Fax).MaximumLength(24);
        RuleFor(request => request.Email).NotEmpty().MaximumLength(60).EmailAddress();
        RuleFor(request => request.SupportRepId).GreaterThan(0);
    }
}
