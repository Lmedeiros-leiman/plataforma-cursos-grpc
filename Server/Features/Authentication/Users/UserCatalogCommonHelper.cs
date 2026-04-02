using ChinookDatabase.DataModel;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Users;

public static class UserCatalogCommonHelper
{
    public static async Task EnsureSupportRepExistsAsync(
        ChinookContext dbContext,
        int supportRepId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(entity => entity.EmployeeId == supportRepId, cancellationToken);

        if (!exists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Support representative not found."));
        }
    }

    public static async Task EnsureEmailIsUniqueAsync(
        ChinookContext dbContext,
        string email,
        int? currentCustomerId,
        CancellationToken cancellationToken)
    {
        var trimmedEmail = email.Trim();
        var duplicated = await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(entity =>
                entity.CustomerId != currentCustomerId &&
                entity.Email == trimmedEmail,
                cancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Another user with the same email already exists."));
        }
    }

    public static UserItem ToItem(Customer entity)
    {
        return new UserItem
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
        };
    }

    public static bool Matches(UserItem left, UserItem right)
    {
        return left.CustomerId == right.CustomerId &&
            left.FirstName == right.FirstName &&
            left.LastName == right.LastName &&
            left.Company == right.Company &&
            left.Address == right.Address &&
            left.City == right.City &&
            left.State == right.State &&
            left.Country == right.Country &&
            left.PostalCode == right.PostalCode &&
            left.Phone == right.Phone &&
            left.Fax == right.Fax &&
            left.Email == right.Email &&
            left.SupportRepId == right.SupportRepId;
    }

    public static string NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
