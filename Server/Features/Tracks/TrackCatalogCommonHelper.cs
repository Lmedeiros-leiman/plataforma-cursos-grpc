using ChinookDatabase.DataModel;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Tracks;

public static class TrackCatalogCommonHelper
{
    public static async Task EnsureRelationsExistAsync(
        ChinookContext dbContext,
        int albumId,
        int mediaTypeId,
        int genreId,
        CancellationToken cancellationToken)
    {
        var albumExists = await dbContext.Albums.AsNoTracking().AnyAsync(entity => entity.AlbumId == albumId, cancellationToken);
        if (!albumExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Album not found."));
        }

        var mediaTypeExists = await dbContext.MediaTypes.AsNoTracking().AnyAsync(entity => entity.MediaTypeId == mediaTypeId, cancellationToken);
        if (!mediaTypeExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Media type not found."));
        }

        var genreExists = await dbContext.Genres.AsNoTracking().AnyAsync(entity => entity.GenreId == genreId, cancellationToken);
        if (!genreExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Genre not found."));
        }
    }

    public static TrackItem ToItem(Track entity)
    {
        return new TrackItem
        {
            TrackId = entity.TrackId,
            Name = entity.Name ?? string.Empty,
            AlbumId = entity.AlbumId,
            AlbumTitle = entity.Album?.Title ?? string.Empty,
            MediaTypeId = entity.MediaTypeId,
            MediaTypeName = entity.MediaType?.Name ?? string.Empty,
            GenreId = entity.GenreId,
            GenreName = entity.Genre?.Name ?? string.Empty,
            Composer = entity.Composer ?? string.Empty,
            Miliseconds = entity.Miliseconds,
            Bytes = entity.Bytes,
            UnitPrice = (double)entity.UnitPrice
        };
    }

    public static bool Matches(TrackItem left, TrackItem right)
    {
        return left.TrackId == right.TrackId &&
               left.Name == right.Name &&
               left.AlbumId == right.AlbumId &&
               left.MediaTypeId == right.MediaTypeId &&
               left.GenreId == right.GenreId &&
               left.Composer == right.Composer &&
               left.Miliseconds == right.Miliseconds &&
               left.Bytes == right.Bytes &&
               left.UnitPrice == right.UnitPrice;
    }

    public static string NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
