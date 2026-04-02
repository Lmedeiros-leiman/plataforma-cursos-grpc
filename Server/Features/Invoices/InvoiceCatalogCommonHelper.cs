using System.Globalization;
using ChinookDatabase.DataModel;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Invoices;

public static class InvoiceCatalogCommonHelper
{
    public static async Task EnsureCustomerExistsAsync(
        ChinookContext dbContext,
        int customerId,
        CancellationToken cancellationToken)
    {
        var customerExists = await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(entity => entity.CustomerId == customerId, cancellationToken);

        if (!customerExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Customer not found."));
        }
    }

    public static InvoiceItem ToItem(Invoice entity)
    {
        var customerName = entity.Customer is null
            ? string.Empty
            : $"{entity.Customer.FirstName} {entity.Customer.LastName}".Trim();

        return new InvoiceItem
        {
            InvoiceId = entity.InvoiceId,
            CustomerId = entity.CustomerId,
            CustomerName = customerName,
            InvoiceDate = entity.InvoiceDate.ToString("O", CultureInfo.InvariantCulture),
            BillingAddress = entity.BillingAddress ?? string.Empty,
            BillingCity = entity.BillingCity ?? string.Empty,
            BillingState = entity.BillingState ?? string.Empty,
            BillingCountry = entity.BillingCountry ?? string.Empty,
            BillingPostalCode = entity.BillingPostalCode ?? string.Empty,
            Total = (double)entity.Total
        };
    }

    public static bool Matches(InvoiceItem left, InvoiceItem right)
    {
        return left.InvoiceId == right.InvoiceId &&
               left.CustomerId == right.CustomerId &&
               left.InvoiceDate == right.InvoiceDate &&
               left.BillingAddress == right.BillingAddress &&
               left.BillingCity == right.BillingCity &&
               left.BillingState == right.BillingState &&
               left.BillingCountry == right.BillingCountry &&
               left.BillingPostalCode == right.BillingPostalCode &&
               left.Total == right.Total;
    }

    public static DateTime ParseDate(string value, string fieldName)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        throw new RpcException(new Status(StatusCode.InvalidArgument, $"The field {fieldName} must be a valid date."));
    }

    public static string NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
