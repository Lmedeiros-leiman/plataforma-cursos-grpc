using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Admins;

public static class DeleteAdminHelper
{
    public static async Task<DeleteAdminResponse> HandleAsync(
        ChinookContext dbContext,
        AdminItem request,
        CancellationToken cancellationToken)
    {
        var current = await GetAdminByIdHelper.HandleAsync(
            dbContext,
            new GetAdminByIdRequest { EmployeeId = request.EmployeeId },
            cancellationToken);

        if (!AdminCatalogCommonHelper.Matches(current, request))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Admin data does not match the current database state."));
        }

        var hasCustomers = await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(entity => entity.SupportRepId == request.EmployeeId, cancellationToken);

        if (hasCustomers)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Admin supports users and cannot be deleted."));
        }

        var managesOthers = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(entity => entity.ReportsTo == request.EmployeeId, cancellationToken);

        if (managesOthers)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Admin manages other admins and cannot be deleted."));
        }

        var entity = await dbContext.Employees
            .FirstOrDefaultAsync(item => item.EmployeeId == request.EmployeeId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Admin not found."));

        dbContext.Employees.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteAdminResponse
        {
            EmployeeId = request.EmployeeId,
            Message = "Admin deleted successfully."
        };
    }
}

public class DeleteAdminRequestValidator : AbstractValidator<AdminItem>
{
    public DeleteAdminRequestValidator(ChinookContext dbContext)
    {
        RuleFor(request => request.EmployeeId).GreaterThan(0);
        RuleFor(request => request.FirstName).NotEmpty().MaximumLength(20);
        RuleFor(request => request.LastName).NotEmpty().MaximumLength(20);
        RuleFor(request => request.Title).MaximumLength(30);
        RuleFor(request => request.BirthDate).NotEmpty();
        RuleFor(request => request.HireDate).NotEmpty();
        RuleFor(request => request.Address).MaximumLength(70);
        RuleFor(request => request.City).MaximumLength(40);
        RuleFor(request => request.State).MaximumLength(40);
        RuleFor(request => request.Country).MaximumLength(40);
        RuleFor(request => request.PostalCode).MaximumLength(10);
        RuleFor(request => request.Phone).MaximumLength(24);
        RuleFor(request => request.Fax).MaximumLength(24);
        RuleFor(request => request.Email).NotEmpty().MaximumLength(60).EmailAddress();

        RuleFor(request => request)
            .MustAsync(async (request, cancellationToken) =>
            {
                var current = await dbContext.Employees
                    .AsNoTracking()
                    .Where(entity => entity.EmployeeId == request.EmployeeId)
                    .Select(entity => new AdminItem
                    {
                        EmployeeId = entity.EmployeeId,
                        FirstName = entity.FirstName ?? string.Empty,
                        LastName = entity.LastName ?? string.Empty,
                        Title = entity.Title ?? string.Empty,
                        ReportsTo = entity.ReportsTo,
                        BirthDate = entity.BirthDate.ToString("O"),
                        HireDate = entity.HireDate.ToString("O"),
                        Address = entity.Address ?? string.Empty,
                        City = entity.City ?? string.Empty,
                        State = entity.State ?? string.Empty,
                        Country = entity.Country ?? string.Empty,
                        PostalCode = entity.PostalCode ?? string.Empty,
                        Phone = entity.Phone ?? string.Empty,
                        Fax = entity.Fax ?? string.Empty,
                        Email = entity.Email ?? string.Empty
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return current is not null && AdminCatalogCommonHelper.Matches(current, request);
            })
            .WithMessage("O admin informado precisa existir e ser identico ao salvo no banco.");
    }
}
