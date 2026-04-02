using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.InvoiceLines;

public static class GetInvoiceLineByIdHelper
{
    public static async Task<InvoiceLineItem> HandleAsync(
        ChinookContext dbContext,
        GetInvoiceLineByIdRequest request,
        CancellationToken cancellationToken)
    {
        return await dbContext.InvoiceLines
            .AsNoTracking()
            .Include(entity => entity.Invoice)
            .ThenInclude(entity => entity.Customer)
            .Include(entity => entity.Track)
            .Where(entity => entity.InvoiceLineId == request.InvoiceLineId)
            .Select(entity => InvoiceLineCatalogCommonHelper.ToItem(entity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Invoice line not found."));
    }
}

public class GetInvoiceLineByIdRequestValidator : AbstractValidator<GetInvoiceLineByIdRequest>
{
    public GetInvoiceLineByIdRequestValidator()
    {
        RuleFor(request => request.InvoiceLineId).GreaterThan(0);
    }
}
