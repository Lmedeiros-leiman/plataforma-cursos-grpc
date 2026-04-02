using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Albums;

public static class GetAlbumByIdHelper
{
    public static async Task<AlbumItem> HandleAsync(
        ChinookContext dbContext,
        GetAlbumByIdRequest request,
        CancellationToken cancellationToken)
    {
        var album = await dbContext.Albums
            .AsNoTracking()
            .Include(entity => entity.Artist)
            .Where(entity => entity.AlbumId == request.AlbumId)
            .Select(entity => new AlbumProjection
            {
                AlbumId = entity.AlbumId,
                Title = entity.Title,
                ArtistId = entity.ArtistId,
                ArtistName = entity.Artist.Name
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Album not found."));

        var tracksByAlbumId = await LoadTracksByAlbumIdAsync(dbContext, [album.AlbumId], cancellationToken);
        return ToAlbumItem(album, tracksByAlbumId);
    }

    private static async Task<Dictionary<int, List<TrackItem>>> LoadTracksByAlbumIdAsync(
        ChinookContext dbContext,
        IReadOnlyCollection<int> albumIds,
        CancellationToken cancellationToken)
    {
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

public class GetAlbumByIdRequestValidator : AbstractValidator<GetAlbumByIdRequest>
{
    public GetAlbumByIdRequestValidator()
    {
        RuleFor(request => request.AlbumId)
            .GreaterThan(0)
            .WithMessage("O album_id deve ser maior que zero.");
    }
}
