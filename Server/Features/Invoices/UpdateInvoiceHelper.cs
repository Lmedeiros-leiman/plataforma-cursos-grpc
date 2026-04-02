using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Server.Features.Invoices;

public static class UpdateInvoiceHelper
{
    public static async Task<InvoiceItem> HandleAsync(
        ChinookContext dbContext,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Invoices
            .FirstOrDefaultAsync(item => item.InvoiceId == request.InvoiceId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Invoice not found."));

        await InvoiceCatalogCommonHelper.EnsureCustomerExistsAsync(dbContext, request.CustomerId, cancellationToken);

        entity.CustomerId = request.CustomerId;
        entity.InvoiceDate = InvoiceCatalogCommonHelper.ParseDate(request.InvoiceDate, nameof(request.InvoiceDate));
        entity.BillingAddress = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingAddress);
        entity.BillingCity = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingCity);
        entity.BillingState = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingState);
        entity.BillingCountry = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingCountry);
        entity.BillingPostalCode = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingPostalCode);
        entity.Total = (decimal)request.Total;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetInvoiceByIdHelper.HandleAsync(dbContext, new GetInvoiceByIdRequest { InvoiceId = entity.InvoiceId }, cancellationToken);
    }
}

public class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
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
