using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Tracks;

public static class ListTracksHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<ListTracksResponse> HandleAsync(
        ChinookContext dbContext,
        ListTracksRequest request,
        CancellationToken cancellationToken)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.Tracks
            .AsNoTracking()
            .Include(entity => entity.Album)
            .Include(entity => entity.MediaType)
            .Include(entity => entity.Genre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(entity => EF.Functions.Like(entity.Name, $"%{name}%"));
        }

        if (request.AlbumId > 0)
        {
            query = query.Where(entity => entity.AlbumId == request.AlbumId);
        }

        if (request.MediaTypeId > 0)
        {
            query = query.Where(entity => entity.MediaTypeId == request.MediaTypeId);
        }

        if (request.GenreId > 0)
        {
            query = query.Where(entity => entity.GenreId == request.GenreId);
        }

        var page = await query
            .Where(entity => entity.TrackId > cursor)
            .OrderBy(entity => entity.TrackId)
            .Select(entity => TrackCatalogCommonHelper.ToItem(entity))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListTracksResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].TrackId : 0
        };
        reply.Tracks.AddRange(items);
        return reply;
    }
}

public class ListTracksRequestValidator : AbstractValidator<ListTracksRequest>
{
    public ListTracksRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
        RuleFor(request => request.AlbumId).GreaterThanOrEqualTo(0);
        RuleFor(request => request.MediaTypeId).GreaterThanOrEqualTo(0);
        RuleFor(request => request.GenreId).GreaterThanOrEqualTo(0);
    }
}
