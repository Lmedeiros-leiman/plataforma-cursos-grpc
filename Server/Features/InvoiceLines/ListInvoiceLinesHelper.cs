using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.InvoiceLines;

public static class ListInvoiceLinesHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<ListInvoiceLinesResponse> HandleAsync(
        ChinookContext dbContext,
        ListInvoiceLinesRequest request,
        CancellationToken cancellationToken)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.InvoiceLines
            .AsNoTracking()
            .Include(entity => entity.Invoice)
            .ThenInclude(entity => entity.Customer)
            .Include(entity => entity.Track)
            .AsQueryable();

        if (request.InvoiceId > 0)
        {
            query = query.Where(entity => entity.InvoiceId == request.InvoiceId);
        }

        if (request.TrackId > 0)
        {
            query = query.Where(entity => entity.TrackId == request.TrackId);
        }

        var page = await query
            .Where(entity => entity.InvoiceLineId > cursor)
            .OrderBy(entity => entity.InvoiceLineId)
            .Select(entity => InvoiceLineCatalogCommonHelper.ToItem(entity))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListInvoiceLinesResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].InvoiceLineId : 0
        };
        reply.InvoiceLines.AddRange(items);
        return reply;
    }
}

public class ListInvoiceLinesRequestValidator : AbstractValidator<ListInvoiceLinesRequest>
{
    public ListInvoiceLinesRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
        RuleFor(request => request.InvoiceId).GreaterThanOrEqualTo(0);
        RuleFor(request => request.TrackId).GreaterThanOrEqualTo(0);
    }
}
