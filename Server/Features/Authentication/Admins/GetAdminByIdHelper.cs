using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Admins;

public static class GetAdminByIdHelper
{
    public static async Task<AdminItem> HandleAsync(
        ChinookContext dbContext,
        GetAdminByIdRequest request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Where(entity => entity.EmployeeId == request.EmployeeId)
            .Select(entity => AdminCatalogCommonHelper.ToItem(entity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Admin not found."));
    }
}

public class GetAdminByIdRequestValidator : AbstractValidator<GetAdminByIdRequest>
{
    public GetAdminByIdRequestValidator()
    {
        RuleFor(request => request.EmployeeId).GreaterThan(0);
    }
}
