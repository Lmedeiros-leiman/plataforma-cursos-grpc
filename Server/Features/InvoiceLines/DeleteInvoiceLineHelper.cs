using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.InvoiceLines;

public static class DeleteInvoiceLineHelper
{
    public static async Task<DeleteInvoiceLineResponse> HandleAsync(
        ChinookContext dbContext,
        InvoiceLineItem request,
        CancellationToken cancellationToken)
    {
        var current = await GetInvoiceLineByIdHelper.HandleAsync(dbContext, new GetInvoiceLineByIdRequest { InvoiceLineId = request.InvoiceLineId }, cancellationToken);
        if (!InvoiceLineCatalogCommonHelper.Matches(current, request))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invoice line data does not match the current database state."));
        }

        var entity = await dbContext.InvoiceLines
            .FirstOrDefaultAsync(item => item.InvoiceLineId == request.InvoiceLineId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Invoice line not found."));

        dbContext.InvoiceLines.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteInvoiceLineResponse
        {
            InvoiceLineId = request.InvoiceLineId,
            Message = "Invoice line deleted successfully."
        };
    }
}

public class DeleteInvoiceLineRequestValidator : AbstractValidator<InvoiceLineItem>
{
    public DeleteInvoiceLineRequestValidator()
    {
        RuleFor(request => request.InvoiceLineId).GreaterThan(0);
        RuleFor(request => request.InvoiceId).GreaterThan(0);
        RuleFor(request => request.TrackId).GreaterThan(0);
        RuleFor(request => request.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Quantity).GreaterThan(0);
    }
}
