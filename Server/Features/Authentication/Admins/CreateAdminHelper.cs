using ChinookDatabase.DataModel;
using FluentValidation;
using System.Globalization;

namespace Server.Features.Authentication.Admins;

public static class CreateAdminHelper
{
    public static async Task<AdminItem> HandleAsync(
        ChinookContext dbContext,
        CreateAdminRequest request,
        CancellationToken cancellationToken)
    {
        await AdminCatalogCommonHelper.EnsureReportsToIsValidAsync(dbContext, request.ReportsTo, null, cancellationToken);
        await AdminCatalogCommonHelper.EnsureEmailIsUniqueAsync(dbContext, request.Email, null, cancellationToken);

        var entity = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Title = AdminCatalogCommonHelper.NormalizeOptional(request.Title),
            ReportsTo = request.ReportsTo,
            BirthDate = AdminCatalogCommonHelper.ParseDate(request.BirthDate, "birth_date"),
            HireDate = AdminCatalogCommonHelper.ParseDate(request.HireDate, "hire_date"),
            Address = AdminCatalogCommonHelper.NormalizeOptional(request.Address),
            City = AdminCatalogCommonHelper.NormalizeOptional(request.City),
            State = AdminCatalogCommonHelper.NormalizeOptional(request.State),
            Country = AdminCatalogCommonHelper.NormalizeOptional(request.Country),
            PostalCode = AdminCatalogCommonHelper.NormalizeOptional(request.PostalCode),
            Phone = AdminCatalogCommonHelper.NormalizeOptional(request.Phone),
            Fax = AdminCatalogCommonHelper.NormalizeOptional(request.Fax),
            Email = request.Email.Trim()
        };

        dbContext.Employees.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAdminByIdHelper.HandleAsync(
            dbContext,
            new GetAdminByIdRequest { EmployeeId = entity.EmployeeId },
            cancellationToken);
    }
}

public class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
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
