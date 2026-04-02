using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Invoices;

public class InvoiceCatalogService(ChinookContext dbContext) : InvoiceCatalog.InvoiceCatalogBase
{
    public override Task<ListInvoicesResponse> ListInvoices(ListInvoicesRequest request, ServerCallContext context)
    {
        return ListInvoicesHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetInvoiceByIdResponse> GetInvoiceById(GetInvoiceByIdRequest request, ServerCallContext context)
    {
        var invoice = await GetInvoiceByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetInvoiceByIdResponse { Invoice = invoice };
    }

    public override async Task<CreateInvoiceResponse> CreateInvoice(CreateInvoiceRequest request, ServerCallContext context)
    {
        var invoice = await CreateInvoiceHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreateInvoiceResponse { Invoice = invoice };
    }

    public override async Task<UpdateInvoiceResponse> UpdateInvoice(UpdateInvoiceRequest request, ServerCallContext context)
    {
        var invoice = await UpdateInvoiceHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdateInvoiceResponse { Invoice = invoice };
    }

    public override Task<DeleteInvoiceResponse> DeleteInvoice(DeleteInvoiceRequest request, ServerCallContext context)
    {
        return DeleteInvoiceHelper.HandleAsync(dbContext, request.Invoice, context.CancellationToken);
    }
}
