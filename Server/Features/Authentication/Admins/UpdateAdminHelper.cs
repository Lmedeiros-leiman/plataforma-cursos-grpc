using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Server.Features.Authentication.Admins;

public static class UpdateAdminHelper
{
    public static async Task<AdminItem> HandleAsync(
        ChinookContext dbContext,
        UpdateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Employees
            .FirstOrDefaultAsync(item => item.EmployeeId == request.EmployeeId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Admin not found."));

        await AdminCatalogCommonHelper.EnsureReportsToIsValidAsync(dbContext, request.ReportsTo, request.EmployeeId, cancellationToken);
        await AdminCatalogCommonHelper.EnsureEmailIsUniqueAsync(dbContext, request.Email, request.EmployeeId, cancellationToken);

        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.Title = AdminCatalogCommonHelper.NormalizeOptional(request.Title);
        entity.ReportsTo = request.ReportsTo;
        entity.BirthDate = AdminCatalogCommonHelper.ParseDate(request.BirthDate, "birth_date");
        entity.HireDate = AdminCatalogCommonHelper.ParseDate(request.HireDate, "hire_date");
        entity.Address = AdminCatalogCommonHelper.NormalizeOptional(request.Address);
        entity.City = AdminCatalogCommonHelper.NormalizeOptional(request.City);
        entity.State = AdminCatalogCommonHelper.NormalizeOptional(request.State);
        entity.Country = AdminCatalogCommonHelper.NormalizeOptional(request.Country);
        entity.PostalCode = AdminCatalogCommonHelper.NormalizeOptional(request.PostalCode);
        entity.Phone = AdminCatalogCommonHelper.NormalizeOptional(request.Phone);
        entity.Fax = AdminCatalogCommonHelper.NormalizeOptional(request.Fax);
        entity.Email = request.Email.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminCatalogCommonHelper.ToItem(entity);
    }
}

public class UpdateAdminRequestValidator : AbstractValidator<UpdateAdminRequest>
{
    public UpdateAdminRequestValidator(ChinookContext dbContext)
    {
        RuleFor(request => request.EmployeeId)
            .GreaterThan(0)
            .MustAsync(async (employeeId, cancellationToken) =>
                await dbContext.Employees.AsNoTracking().AnyAsync(entity => entity.EmployeeId == employeeId, cancellationToken))
            .WithMessage("O employee_id informado nao existe.");

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
        RuleFor(request => request.BirthDate)
            .Must(value => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            .WithMessage("O birth_date precisa ser uma data valida.");
        RuleFor(request => request.HireDate)
            .Must(value => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            .WithMessage("O hire_date precisa ser uma data valida.");
    }
}
