using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.PlaylistTracks;

public static class UpdatePlaylistTrackHelper
{
    public static async Task<PlaylistTrackItem> HandleAsync(
        ChinookContext dbContext,
        UpdatePlaylistTrackRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.PlaylistTracks
            .FirstOrDefaultAsync(item => item.PlaylistId == request.PlaylistId && item.TrackId == request.TrackId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Playlist track not found."));

        await PlaylistTrackCatalogCommonHelper.EnsureRelationsExistAsync(dbContext, request.NewPlaylistId, request.NewTrackId, cancellationToken);

        var duplicated = await dbContext.PlaylistTracks
            .AsNoTracking()
            .AnyAsync(item =>
                item.PlaylistId == request.NewPlaylistId &&
                item.TrackId == request.NewTrackId &&
                (item.PlaylistId != request.PlaylistId || item.TrackId != request.TrackId),
                cancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Playlist track already exists."));
        }

        dbContext.PlaylistTracks.Remove(entity);
        dbContext.PlaylistTracks.Add(new PlaylistTrack
        {
            PlaylistId = request.NewPlaylistId,
            TrackId = request.NewTrackId
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetPlaylistTrackByIdHelper.HandleAsync(
            dbContext,
            new GetPlaylistTrackByIdRequest { PlaylistId = request.NewPlaylistId, TrackId = request.NewTrackId },
            cancellationToken);
    }
}

public class UpdatePlaylistTrackRequestValidator : AbstractValidator<UpdatePlaylistTrackRequest>
{
    public UpdatePlaylistTrackRequestValidator()
    {
        RuleFor(request => request.PlaylistId).GreaterThan(0);
        RuleFor(request => request.TrackId).GreaterThan(0);
        RuleFor(request => request.NewPlaylistId).GreaterThan(0);
        RuleFor(request => request.NewTrackId).GreaterThan(0);
    }
}
