using ChinookDatabase.DataModel;
using FluentValidation;

namespace Server.Features.InvoiceLines;

public static class CreateInvoiceLineHelper
{
    public static async Task<InvoiceLineItem> HandleAsync(
        ChinookContext dbContext,
        CreateInvoiceLineRequest request,
        CancellationToken cancellationToken)
    {
        await InvoiceLineCatalogCommonHelper.EnsureRelationsExistAsync(dbContext, request.InvoiceId, request.TrackId, cancellationToken);

        var entity = new InvoiceLine
        {
            InvoiceId = request.InvoiceId,
            TrackId = request.TrackId,
            UnitPrice = (decimal)request.UnitPrice,
            Quantity = request.Quantity
        };

        dbContext.InvoiceLines.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetInvoiceLineByIdHelper.HandleAsync(dbContext, new GetInvoiceLineByIdRequest { InvoiceLineId = entity.InvoiceLineId }, cancellationToken);
    }
}

public class CreateInvoiceLineRequestValidator : AbstractValidator<CreateInvoiceLineRequest>
{
    public CreateInvoiceLineRequestValidator()
    {
        RuleFor(request => request.InvoiceId).GreaterThan(0);
        RuleFor(request => request.TrackId).GreaterThan(0);
        RuleFor(request => request.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Quantity).GreaterThan(0);
    }
}
