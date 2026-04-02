using ChinookDatabase.DataModel;
using FluentValidation;
using System.Globalization;

namespace Server.Features.Invoices;

public static class CreateInvoiceHelper
{
    public static async Task<InvoiceItem> HandleAsync(
        ChinookContext dbContext,
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await InvoiceCatalogCommonHelper.EnsureCustomerExistsAsync(dbContext, request.CustomerId, cancellationToken);

        var entity = new Invoice
        {
            CustomerId = request.CustomerId,
            InvoiceDate = InvoiceCatalogCommonHelper.ParseDate(request.InvoiceDate, nameof(request.InvoiceDate)),
            BillingAddress = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingAddress),
            BillingCity = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingCity),
            BillingState = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingState),
            BillingCountry = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingCountry),
            BillingPostalCode = InvoiceCatalogCommonHelper.NormalizeOptional(request.BillingPostalCode),
            Total = (decimal)request.Total
        };

        dbContext.Invoices.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetInvoiceByIdHelper.HandleAsync(dbContext, new GetInvoiceByIdRequest { InvoiceId = entity.InvoiceId }, cancellationToken);
    }
}

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
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
