using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Admin;

public static class SignInAdminHelper
{
    public static async Task<AdminSignInReply> HandleAsync(
        ChinookContext dbContext,
        ILogger<AdminLoginService> logger,
        AdminSignInRequest request,
        CancellationToken cancellationToken)
    {
        var credential = request.Credential.Trim();

        var employee = await dbContext.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.EmployeeId)
            .FirstOrDefaultAsync(employee =>
                employee.Email == credential ||
                employee.Phone == credential ||
                employee.Fax == credential,
                cancellationToken);

        if (employee is null)
        {
            logger.LogInformation("No employee found for credential {Credential}", credential);
            throw new RpcException(new Status(StatusCode.NotFound, "Employee not found."));
        }

        logger.LogInformation("Employee {EmployeeId} authenticated with credential {Credential}", employee.EmployeeId, credential);

        return new AdminSignInReply
        {
            EmployeeId = employee.EmployeeId,
            FirstName = employee.FirstName ?? string.Empty,
            LastName = employee.LastName ?? string.Empty,
            Title = employee.Title ?? string.Empty,
            Email = employee.Email ?? string.Empty,
            Phone = employee.Phone ?? string.Empty,
            Fax = employee.Fax ?? string.Empty
        };
    }
}

public class AdminSignInRequestValidator : AbstractValidator<AdminSignInRequest>
{
    public AdminSignInRequestValidator()
    {
        RuleFor(request => request.Credential)
            .NotEmpty()
            .WithMessage("A credencial e obrigatoria.");
    }
}
