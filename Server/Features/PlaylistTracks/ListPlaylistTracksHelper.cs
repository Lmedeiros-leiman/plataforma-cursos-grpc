using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.PlaylistTracks;

public static class ListPlaylistTracksHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<ListPlaylistTracksResponse> HandleAsync(
        ChinookContext dbContext,
        ListPlaylistTracksRequest request,
        CancellationToken cancellationToken)
    {
        var cursorPlaylistId = request.CursorPlaylistId < 0 ? 0 : request.CursorPlaylistId;
        var cursorTrackId = request.CursorTrackId < 0 ? 0 : request.CursorTrackId;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.PlaylistTracks
            .AsNoTracking()
            .Include(entity => entity.Playlist)
            .Include(entity => entity.Track)
            .AsQueryable();

        if (request.PlaylistId > 0)
        {
            query = query.Where(entity => entity.PlaylistId == request.PlaylistId);
        }

        var page = await query
            .Where(entity =>
                entity.PlaylistId > cursorPlaylistId ||
                (entity.PlaylistId == cursorPlaylistId && entity.TrackId > cursorTrackId))
            .OrderBy(entity => entity.PlaylistId)
            .ThenBy(entity => entity.TrackId)
            .Select(entity => PlaylistTrackCatalogCommonHelper.ToItem(entity))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListPlaylistTracksResponse
        {
            NextCursorPlaylistId = hasMore && items.Count > 0 ? items[^1].PlaylistId : 0,
            NextCursorTrackId = hasMore && items.Count > 0 ? items[^1].TrackId : 0
        };
        reply.PlaylistTracks.AddRange(items);
        return reply;
    }
}

public class ListPlaylistTracksRequestValidator : AbstractValidator<ListPlaylistTracksRequest>
{
    public ListPlaylistTracksRequestValidator()
    {
        RuleFor(request => request.PlaylistId).GreaterThanOrEqualTo(0);
        RuleFor(request => request.CursorPlaylistId).GreaterThanOrEqualTo(0);
        RuleFor(request => request.CursorTrackId).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
    }
}
