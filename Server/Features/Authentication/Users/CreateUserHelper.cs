using ChinookDatabase.DataModel;
using FluentValidation;

namespace Server.Features.Authentication.Users;

public static class CreateUserHelper
{
    public static async Task<UserItem> HandleAsync(
        ChinookContext dbContext,
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        await UserCatalogCommonHelper.EnsureSupportRepExistsAsync(dbContext, request.SupportRepId, cancellationToken);
        await UserCatalogCommonHelper.EnsureEmailIsUniqueAsync(dbContext, request.Email, null, cancellationToken);

        var entity = new Customer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Company = UserCatalogCommonHelper.NormalizeOptional(request.Company),
            Address = UserCatalogCommonHelper.NormalizeOptional(request.Address),
            City = UserCatalogCommonHelper.NormalizeOptional(request.City),
            State = UserCatalogCommonHelper.NormalizeOptional(request.State),
            Country = UserCatalogCommonHelper.NormalizeOptional(request.Country),
            PostalCode = UserCatalogCommonHelper.NormalizeOptional(request.PostalCode),
            Phone = UserCatalogCommonHelper.NormalizeOptional(request.Phone),
            Fax = UserCatalogCommonHelper.NormalizeOptional(request.Fax),
            Email = request.Email.Trim(),
            SupportRepId = request.SupportRepId
        };

        dbContext.Customers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetUserByIdHelper.HandleAsync(
            dbContext,
            new GetUserByIdRequest { CustomerId = entity.CustomerId },
            cancellationToken);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
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
