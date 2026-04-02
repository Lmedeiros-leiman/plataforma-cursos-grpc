using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.InvoiceLines;

public static class UpdateInvoiceLineHelper
{
    public static async Task<InvoiceLineItem> HandleAsync(
        ChinookContext dbContext,
        UpdateInvoiceLineRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.InvoiceLines
            .FirstOrDefaultAsync(item => item.InvoiceLineId == request.InvoiceLineId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Invoice line not found."));

        await InvoiceLineCatalogCommonHelper.EnsureRelationsExistAsync(dbContext, request.InvoiceId, request.TrackId, cancellationToken);

        entity.InvoiceId = request.InvoiceId;
        entity.TrackId = request.TrackId;
        entity.UnitPrice = (decimal)request.UnitPrice;
        entity.Quantity = request.Quantity;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetInvoiceLineByIdHelper.HandleAsync(dbContext, new GetInvoiceLineByIdRequest { InvoiceLineId = entity.InvoiceLineId }, cancellationToken);
    }
}

public class UpdateInvoiceLineRequestValidator : AbstractValidator<UpdateInvoiceLineRequest>
{
    public UpdateInvoiceLineRequestValidator()
    {
        RuleFor(request => request.InvoiceLineId).GreaterThan(0);
        RuleFor(request => request.InvoiceId).GreaterThan(0);
        RuleFor(request => request.TrackId).GreaterThan(0);
        RuleFor(request => request.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Quantity).GreaterThan(0);
    }
}
