using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Invoices;

public static class GetInvoiceByIdHelper
{
    public static async Task<InvoiceItem> HandleAsync(
        ChinookContext dbContext,
        GetInvoiceByIdRequest request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Invoices
            .AsNoTracking()
            .Include(entity => entity.Customer)
            .Where(entity => entity.InvoiceId == request.InvoiceId)
            .Select(entity => InvoiceCatalogCommonHelper.ToItem(entity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Invoice not found."));
    }
}

public class GetInvoiceByIdRequestValidator : AbstractValidator<GetInvoiceByIdRequest>
{
    public GetInvoiceByIdRequestValidator()
    {
        RuleFor(request => request.InvoiceId).GreaterThan(0);
    }
}
