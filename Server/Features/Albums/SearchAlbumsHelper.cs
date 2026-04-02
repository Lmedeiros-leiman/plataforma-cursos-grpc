using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Albums;

public static class SearchAlbumsHelper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public static async Task<SearchAlbumsResponse> HandleAsync(
        ChinookContext dbContext,
        SearchAlbumsRequest request,
        CancellationToken cancellationToken)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var query = dbContext.Albums
            .AsNoTracking()
            .Include(album => album.Artist)
            .AsQueryable();

        query = ApplyAuthorFilter(dbContext, query, request.Author);
        query = ApplyBandFilter(query, request.Band);
        query = ApplyCategoryFilter(dbContext, query, request.Category);

        var albumPage = await query
            .Where(album => album.AlbumId > cursor)
            .OrderBy(album => album.AlbumId)
            .Select(album => new AlbumProjection
            {
                AlbumId = album.AlbumId,
                Title = album.Title,
                ArtistId = album.ArtistId,
                ArtistName = album.Artist.Name
            })
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = albumPage.Count > pageSize;
        var albums = albumPage.Take(pageSize).ToList();
        var tracksByAlbumId = await LoadTracksByAlbumIdAsync(dbContext, albums.Select(album => album.AlbumId).ToList(), cancellationToken);

        var response = new SearchAlbumsResponse
        {
            NextCursor = hasMore && albums.Count > 0 ? albums[^1].AlbumId : 0
        };

        response.Albums.AddRange(albums.Select(album => ToAlbumItem(album, tracksByAlbumId)));
        return response;
    }

    private static IQueryable<Album> ApplyAuthorFilter(ChinookContext dbContext, IQueryable<Album> query, FilterValue? filter)
    {
        return filter?.ValueCase switch
        {
            FilterValue.ValueOneofCase.Id => query.Where(album => album.ArtistId == filter.Id),
            FilterValue.ValueOneofCase.Text when !string.IsNullOrWhiteSpace(filter.Text) =>
                query.Where(album => EF.Functions.Like(album.Artist.Name, $"%{filter.Text.Trim()}%")),
            _ => query
        };
    }

    private static IQueryable<Album> ApplyBandFilter(IQueryable<Album> query, FilterValue? filter)
    {
        return filter?.ValueCase switch
        {
            FilterValue.ValueOneofCase.Id => query.Where(album => album.AlbumId == filter.Id),
            FilterValue.ValueOneofCase.Text when !string.IsNullOrWhiteSpace(filter.Text) =>
                query.Where(album => EF.Functions.Like(album.Title, $"%{filter.Text.Trim()}%")),
            _ => query
        };
    }

    private static IQueryable<Album> ApplyCategoryFilter(ChinookContext dbContext, IQueryable<Album> query, FilterValue? filter)
    {
        return filter?.ValueCase switch
        {
            FilterValue.ValueOneofCase.Id => query.Where(album =>
                dbContext.Tracks.Any(track => track.AlbumId == album.AlbumId && track.GenreId == filter.Id)),
            FilterValue.ValueOneofCase.Text when !string.IsNullOrWhiteSpace(filter.Text) =>
                query.Where(album => dbContext.Tracks.Any(track =>
                    track.AlbumId == album.AlbumId &&
                    EF.Functions.Like(track.Genre.Name, $"%{filter.Text.Trim()}%"))),
            _ => query
        };
    }

    private static async Task<Dictionary<int, List<TrackItem>>> LoadTracksByAlbumIdAsync(
        ChinookContext dbContext,
        IReadOnlyCollection<int> albumIds,
        CancellationToken cancellationToken)
    {
        if (albumIds.Count == 0)
        {
            return new Dictionary<int, List<TrackItem>>();
        }

        var tracks = await dbContext.Tracks
            .AsNoTracking()
            .Include(track => track.Genre)
            .Where(track => albumIds.Contains(track.AlbumId))
            .OrderBy(track => track.TrackId)
            .Select(track => new
            {
                track.AlbumId,
                Item = new TrackItem
                {
                    TrackId = track.TrackId,
                    Name = track.Name,
                    GenreId = track.GenreId,
                    GenreName = track.Genre.Name ?? string.Empty,
                    Composer = track.Composer ?? string.Empty,
                    Milliseconds = track.Miliseconds,
                    UnitPrice = Convert.ToDouble(track.UnitPrice)
                }
            })
            .ToListAsync(cancellationToken);

        return tracks
            .GroupBy(track => track.AlbumId)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Item).ToList());
    }

    private static AlbumItem ToAlbumItem(AlbumProjection album, IReadOnlyDictionary<int, List<TrackItem>> tracksByAlbumId)
    {
        var item = new AlbumItem
        {
            AlbumId = album.AlbumId,
            Title = album.Title ?? string.Empty,
            ArtistId = album.ArtistId,
            ArtistName = album.ArtistName ?? string.Empty
        };

        if (tracksByAlbumId.TryGetValue(album.AlbumId, out var tracks))
        {
            item.Tracks.AddRange(tracks);
        }

        return item;
    }

    private sealed class AlbumProjection
    {
        public int AlbumId { get; init; }
        public string Title { get; init; } = string.Empty;
        public int ArtistId { get; init; }
        public string ArtistName { get; init; } = string.Empty;
    }
}

public class SearchAlbumsRequestValidator : AbstractValidator<SearchAlbumsRequest>
{
    public SearchAlbumsRequestValidator()
    {
        RuleFor(request => request.Cursor)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O cursor deve ser maior ou igual a zero.");

        RuleFor(request => request.PageSize)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O page_size deve ser maior ou igual a zero.");
    }
}
