using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.User;

public static class SignInUserHelper
{
    public static async Task<UserSignInReply> HandleAsync(
        ChinookContext dbContext,
        ILogger<UserLoginService> logger,
        UserSignInRequest request,
        CancellationToken cancellationToken)
    {
        var credential = request.Credential.Trim();

        var customer = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.CustomerId)
            .FirstOrDefaultAsync(customer =>
                customer.Email == credential ||
                customer.Phone == credential ||
                customer.Fax == credential,
                cancellationToken);

        if (customer is null)
        {
            logger.LogInformation("No customer found for credential {Credential}", credential);
            throw new RpcException(new Status(StatusCode.NotFound, "Customer not found."));
        }

        logger.LogInformation("Customer {CustomerId} authenticated with credential {Credential}", customer.CustomerId, credential);

        return new UserSignInReply
        {
            CustomerId = customer.CustomerId,
            FirstName = customer.FirstName ?? string.Empty,
            LastName = customer.LastName ?? string.Empty,
            Company = customer.Company ?? string.Empty,
            Email = customer.Email ?? string.Empty,
            Phone = customer.Phone ?? string.Empty,
            Fax = customer.Fax ?? string.Empty
        };
    }
}

public class UserSignInRequestValidator : AbstractValidator<UserSignInRequest>
{
    public UserSignInRequestValidator()
    {
        RuleFor(request => request.Credential)
            .NotEmpty()
            .WithMessage("A credencial e obrigatoria.");
    }
}
