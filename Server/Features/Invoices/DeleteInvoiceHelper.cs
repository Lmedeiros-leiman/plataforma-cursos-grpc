using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Server.Features.Invoices;

public static class DeleteInvoiceHelper
{
    public static async Task<DeleteInvoiceResponse> HandleAsync(
        ChinookContext dbContext,
        InvoiceItem request,
        CancellationToken cancellationToken)
    {
        var current = await GetInvoiceByIdHelper.HandleAsync(dbContext, new GetInvoiceByIdRequest { InvoiceId = request.InvoiceId }, cancellationToken);
        if (!InvoiceCatalogCommonHelper.Matches(current, request))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invoice data does not match the current database state."));
        }

        var entity = await dbContext.Invoices
            .FirstOrDefaultAsync(item => item.InvoiceId == request.InvoiceId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Invoice not found."));

        var hasInvoiceLines = await dbContext.InvoiceLines
            .AsNoTracking()
            .AnyAsync(item => item.InvoiceId == request.InvoiceId, cancellationToken);

        if (hasInvoiceLines)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invoice has invoice lines and cannot be deleted."));
        }

        dbContext.Invoices.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteInvoiceResponse
        {
            InvoiceId = request.InvoiceId,
            Message = "Invoice deleted successfully."
        };
    }
}

public class DeleteInvoiceRequestValidator : AbstractValidator<InvoiceItem>
{
    public DeleteInvoiceRequestValidator()
    {
        RuleFor(request => request.InvoiceId).GreaterThan(0);
        RuleFor(request => request.CustomerId).GreaterThan(0);
        RuleFor(request => request.InvoiceDate)
            .NotEmpty()
            .Must(value => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            .WithMessage("The invoice_date must be a valid date.");
        RuleFor(request => request.BillingAddress).MaximumLength(70);
        RuleFor(request => request.BillingCity).MaximumLength(40);
        RuleFor(request => request.BillingState).MaximumLength(40);
        RuleFor(request => request.BillingCountry).MaximumLength(40);
        RuleFor(request => request.BillingPostalCode).MaximumLength(10);
        RuleFor(request => request.Total).GreaterThanOrEqualTo(0);
    }
}
