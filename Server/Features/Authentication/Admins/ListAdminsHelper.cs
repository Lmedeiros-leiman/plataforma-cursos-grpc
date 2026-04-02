using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Admins;

public static class ListAdminsHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<ListAdminsResponse> HandleAsync(
        ChinookContext dbContext,
        ListAdminsRequest request,
        CancellationToken cancellationToken)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.Employees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var value = request.Query.Trim();
            query = query.Where(entity =>
                EF.Functions.Like(entity.FirstName, $"%{value}%") ||
                EF.Functions.Like(entity.LastName, $"%{value}%") ||
                EF.Functions.Like(entity.Email, $"%{value}%") ||
                EF.Functions.Like(entity.Title, $"%{value}%"));
        }

        var page = await query
            .Where(entity => entity.EmployeeId > cursor)
            .OrderBy(entity => entity.EmployeeId)
            .Select(entity => AdminCatalogCommonHelper.ToItem(entity))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();
        var reply = new ListAdminsResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].EmployeeId : 0
        };
        reply.Admins.AddRange(items);
        return reply;
    }
}

public class ListAdminsRequestValidator : AbstractValidator<ListAdminsRequest>
{
    public ListAdminsRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
    }
}
