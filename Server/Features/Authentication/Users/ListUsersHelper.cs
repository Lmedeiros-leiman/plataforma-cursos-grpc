using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Authentication.Users;

public static class ListUsersHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<ListUsersResponse> HandleAsync(
        ChinookContext dbContext,
        ListUsersRequest request,
        CancellationToken cancellationToken)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var value = request.Query.Trim();
            query = query.Where(entity =>
                EF.Functions.Like(entity.FirstName, $"%{value}%") ||
                EF.Functions.Like(entity.LastName, $"%{value}%") ||
                EF.Functions.Like(entity.Email, $"%{value}%") ||
                EF.Functions.Like(entity.Company, $"%{value}%"));
        }

        var page = await query
            .Where(entity => entity.CustomerId > cursor)
            .OrderBy(entity => entity.CustomerId)
            .Select(entity => UserCatalogCommonHelper.ToItem(entity))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();
        var reply = new ListUsersResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].CustomerId : 0
        };
        reply.Users.AddRange(items);
        return reply;
    }
}

public class ListUsersRequestValidator : AbstractValidator<ListUsersRequest>
{
    public ListUsersRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
    }
}
