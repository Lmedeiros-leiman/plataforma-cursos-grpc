using ChinookDatabase.DataModel;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.PlaylistTracks;

public static class PlaylistTrackCatalogCommonHelper
{
    public static async Task EnsureRelationsExistAsync(
        ChinookContext dbContext,
        int playlistId,
        int trackId,
        CancellationToken cancellationToken)
    {
        var playlistExists = await dbContext.Playlists
            .AsNoTracking()
            .AnyAsync(entity => entity.PlaylistId == playlistId, cancellationToken);

        if (!playlistExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Playlist not found."));
        }

        var trackExists = await dbContext.Tracks
            .AsNoTracking()
            .AnyAsync(entity => entity.TrackId == trackId, cancellationToken);

        if (!trackExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Track not found."));
        }
    }

    public static async Task EnsurePairDoesNotExistAsync(
        ChinookContext dbContext,
        int playlistId,
        int trackId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.PlaylistTracks
            .AsNoTracking()
            .AnyAsync(entity => entity.PlaylistId == playlistId && entity.TrackId == trackId, cancellationToken);

        if (exists)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Playlist track already exists."));
        }
    }

    public static PlaylistTrackItem ToItem(PlaylistTrack entity)
    {
        return new PlaylistTrackItem
        {
            PlaylistId = entity.PlaylistId,
            PlaylistName = entity.Playlist?.Name ?? string.Empty,
            TrackId = entity.TrackId,
            TrackName = entity.Track?.Name ?? string.Empty
        };
    }

    public static bool Matches(PlaylistTrackItem left, PlaylistTrackItem right)
    {
        return left.PlaylistId == right.PlaylistId &&
               left.TrackId == right.TrackId &&
               left.PlaylistName == right.PlaylistName &&
               left.TrackName == right.TrackName;
    }
}
