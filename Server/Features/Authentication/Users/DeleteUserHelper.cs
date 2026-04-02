using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Users;

public static class DeleteUserHelper
{
    public static async Task<DeleteUserResponse> HandleAsync(
        ChinookContext dbContext,
        UserItem request,
        CancellationToken cancellationToken)
    {
        var current = await GetUserByIdHelper.HandleAsync(
            dbContext,
            new GetUserByIdRequest { CustomerId = request.CustomerId },
            cancellationToken);

        if (!UserCatalogCommonHelper.Matches(current, request))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "User data does not match the current database state."));
        }

        var hasInvoices = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(entity => entity.CustomerId == request.CustomerId, cancellationToken);

        if (hasInvoices)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "User has invoices and cannot be deleted."));
        }

        var entity = await dbContext.Customers
            .FirstOrDefaultAsync(item => item.CustomerId == request.CustomerId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "User not found."));

        dbContext.Customers.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteUserResponse
        {
            CustomerId = request.CustomerId,
            Message = "User deleted successfully."
        };
    }
}

public class DeleteUserRequestValidator : AbstractValidator<UserItem>
{
    public DeleteUserRequestValidator(ChinookContext dbContext)
    {
        RuleFor(request => request.CustomerId).GreaterThan(0);
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

        RuleFor(request => request)
            .MustAsync(async (request, cancellationToken) =>
            {
                var current = await dbContext.Customers
                    .AsNoTracking()
                    .Where(entity => entity.CustomerId == request.CustomerId)
                    .Select(entity => new UserItem
                    {
                        CustomerId = entity.CustomerId,
                        FirstName = entity.FirstName ?? string.Empty,
                        LastName = entity.LastName ?? string.Empty,
                        Company = entity.Company ?? string.Empty,
                        Address = entity.Address ?? string.Empty,
                        City = entity.City ?? string.Empty,
                        State = entity.State ?? string.Empty,
                        Country = entity.Country ?? string.Empty,
                        PostalCode = entity.PostalCode ?? string.Empty,
                        Phone = entity.Phone ?? string.Empty,
                        Fax = entity.Fax ?? string.Empty,
                        Email = entity.Email ?? string.Empty,
                        SupportRepId = entity.SupportRepId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return current is not null && UserCatalogCommonHelper.Matches(current, request);
            })
            .WithMessage("O usuario informado precisa existir e ser identico ao salvo no banco.");
    }
}
