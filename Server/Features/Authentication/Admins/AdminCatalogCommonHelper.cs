using System.Globalization;
using ChinookDatabase.DataModel;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Admins;

public static class AdminCatalogCommonHelper
{
    public static async Task EnsureReportsToIsValidAsync(
        ChinookContext dbContext,
        int reportsTo,
        int? currentEmployeeId,
        CancellationToken cancellationToken)
    {
        if (reportsTo <= 0)
        {
            return;
        }

        if (currentEmployeeId == reportsTo)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "An admin cannot report to itself."));
        }

        var exists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(entity => entity.EmployeeId == reportsTo, cancellationToken);

        if (!exists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "ReportsTo admin not found."));
        }
    }

    public static async Task EnsureEmailIsUniqueAsync(
        ChinookContext dbContext,
        string email,
        int? currentEmployeeId,
        CancellationToken cancellationToken)
    {
        var trimmedEmail = email.Trim();
        var duplicated = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(entity =>
                entity.EmployeeId != currentEmployeeId &&
                entity.Email == trimmedEmail,
                cancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Another admin with the same email already exists."));
        }
    }

    public static AdminItem ToItem(Employee entity)
    {
        return new AdminItem
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
        };
    }

    public static bool Matches(AdminItem left, AdminItem right)
    {
        return left.EmployeeId == right.EmployeeId &&
            left.FirstName == right.FirstName &&
            left.LastName == right.LastName &&
            left.Title == right.Title &&
            left.ReportsTo == right.ReportsTo &&
            left.BirthDate == right.BirthDate &&
            left.HireDate == right.HireDate &&
            left.Address == right.Address &&
            left.City == right.City &&
            left.State == right.State &&
            left.Country == right.Country &&
            left.PostalCode == right.PostalCode &&
            left.Phone == right.Phone &&
            left.Fax == right.Fax &&
            left.Email == right.Email;
    }

    public static string NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static DateTime ParseDate(string value, string fieldName)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"The field {fieldName} must be a valid date."));
        }

        return parsed;
    }
}
