using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Artists;

public static class ListArtistsHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<ListArtistsResponse> HandleAsync(
        ChinookContext dbContext,
        ListArtistsRequest request,
        CancellationToken cancellationToken)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.Artists.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(entity => EF.Functions.Like(entity.Name, $"%{name}%"));
        }

        var page = await query
            .Where(entity => entity.ArtistId > cursor)
            .OrderBy(entity => entity.ArtistId)
            .Select(entity => new ArtistItem
            {
                ArtistId = entity.ArtistId,
                Name = entity.Name ?? string.Empty
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListArtistsResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].ArtistId : 0
        };
        reply.Artists.AddRange(items);
        return reply;
    }
}

public class ListArtistsRequestValidator : AbstractValidator<ListArtistsRequest>
{
    public ListArtistsRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
    }
}
