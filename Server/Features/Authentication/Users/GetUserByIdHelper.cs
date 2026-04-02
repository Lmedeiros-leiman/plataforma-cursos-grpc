using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Users;

public static class GetUserByIdHelper
{
    public static async Task<UserItem> HandleAsync(
        ChinookContext dbContext,
        GetUserByIdRequest request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .Where(entity => entity.CustomerId == request.CustomerId)
            .Select(entity => UserCatalogCommonHelper.ToItem(entity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
    }
}

public class GetUserByIdRequestValidator : AbstractValidator<GetUserByIdRequest>
{
    public GetUserByIdRequestValidator()
    {
        RuleFor(request => request.CustomerId).GreaterThan(0);
    }
}
