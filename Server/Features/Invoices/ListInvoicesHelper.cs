using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Invoices;

public static class ListInvoicesHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<ListInvoicesResponse> HandleAsync(
        ChinookContext dbContext,
        ListInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.Invoices
            .AsNoTracking()
            .Include(entity => entity.Customer)
            .AsQueryable();

        if (request.CustomerId > 0)
        {
            query = query.Where(entity => entity.CustomerId == request.CustomerId);
        }

        var page = await query
            .Where(entity => entity.InvoiceId > cursor)
            .OrderBy(entity => entity.InvoiceId)
            .Select(entity => InvoiceCatalogCommonHelper.ToItem(entity))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListInvoicesResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].InvoiceId : 0
        };
        reply.Invoices.AddRange(items);
        return reply;
    }
}

public class ListInvoicesRequestValidator : AbstractValidator<ListInvoicesRequest>
{
    public ListInvoicesRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
        RuleFor(request => request.CustomerId).GreaterThanOrEqualTo(0);
    }
}
