using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.InvoiceLines;

public class InvoiceLineCatalogService(ChinookContext dbContext) : InvoiceLineCatalog.InvoiceLineCatalogBase
{
    public override Task<ListInvoiceLinesResponse> ListInvoiceLines(ListInvoiceLinesRequest request, ServerCallContext context)
    {
        return ListInvoiceLinesHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetInvoiceLineByIdResponse> GetInvoiceLineById(GetInvoiceLineByIdRequest request, ServerCallContext context)
    {
        var invoiceLine = await GetInvoiceLineByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetInvoiceLineByIdResponse { InvoiceLine = invoiceLine };
    }

    public override async Task<CreateInvoiceLineResponse> CreateInvoiceLine(CreateInvoiceLineRequest request, ServerCallContext context)
    {
        var invoiceLine = await CreateInvoiceLineHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreateInvoiceLineResponse { InvoiceLine = invoiceLine };
    }

    public override async Task<UpdateInvoiceLineResponse> UpdateInvoiceLine(UpdateInvoiceLineRequest request, ServerCallContext context)
    {
        var invoiceLine = await UpdateInvoiceLineHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdateInvoiceLineResponse { InvoiceLine = invoiceLine };
    }

    public override Task<DeleteInvoiceLineResponse> DeleteInvoiceLine(DeleteInvoiceLineRequest request, ServerCallContext context)
    {
        return DeleteInvoiceLineHelper.HandleAsync(dbContext, request.InvoiceLine, context.CancellationToken);
    }
}
