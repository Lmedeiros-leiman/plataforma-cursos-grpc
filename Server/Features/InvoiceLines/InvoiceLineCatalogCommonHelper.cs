using ChinookDatabase.DataModel;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.InvoiceLines;

public static class InvoiceLineCatalogCommonHelper
{
    public static async Task EnsureRelationsExistAsync(
        ChinookContext dbContext,
        int invoiceId,
        int trackId,
        CancellationToken cancellationToken)
    {
        var invoiceExists = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(entity => entity.InvoiceId == invoiceId, cancellationToken);

        if (!invoiceExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Invoice not found."));
        }

        var trackExists = await dbContext.Tracks
            .AsNoTracking()
            .AnyAsync(entity => entity.TrackId == trackId, cancellationToken);

        if (!trackExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Track not found."));
        }
    }

    public static InvoiceLineItem ToItem(InvoiceLine entity)
    {
        var customerName = entity.Invoice?.Customer is null
            ? string.Empty
            : $"{entity.Invoice.Customer.FirstName} {entity.Invoice.Customer.LastName}".Trim();

        return new InvoiceLineItem
        {
            InvoiceLineId = entity.InvoiceLineId,
            InvoiceId = entity.InvoiceId,
            CustomerId = entity.Invoice?.CustomerId ?? 0,
            CustomerName = customerName,
            TrackId = entity.TrackId,
            TrackName = entity.Track?.Name ?? string.Empty,
            UnitPrice = (double)entity.UnitPrice,
            Quantity = entity.Quantity
        };
    }

    public static bool Matches(InvoiceLineItem left, InvoiceLineItem right)
    {
        return left.InvoiceLineId == right.InvoiceLineId &&
               left.InvoiceId == right.InvoiceId &&
               left.TrackId == right.TrackId &&
               left.UnitPrice == right.UnitPrice &&
               left.Quantity == right.Quantity;
    }
}
