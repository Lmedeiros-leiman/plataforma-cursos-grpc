using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.PlaylistTracks;

public static class DeletePlaylistTrackHelper
{
    public static async Task<DeletePlaylistTrackResponse> HandleAsync(
        ChinookContext dbContext,
        PlaylistTrackItem request,
        CancellationToken cancellationToken)
    {
        var current = await GetPlaylistTrackByIdHelper.HandleAsync(
            dbContext,
            new GetPlaylistTrackByIdRequest { PlaylistId = request.PlaylistId, TrackId = request.TrackId },
            cancellationToken);

        if (!PlaylistTrackCatalogCommonHelper.Matches(current, request))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Playlist track data does not match the current database state."));
        }

        var entity = await dbContext.PlaylistTracks
            .FirstOrDefaultAsync(item => item.PlaylistId == request.PlaylistId && item.TrackId == request.TrackId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Playlist track not found."));

        dbContext.PlaylistTracks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeletePlaylistTrackResponse
        {
            PlaylistId = request.PlaylistId,
            TrackId = request.TrackId,
            Message = "Playlist track deleted successfully."
        };
    }
}

public class DeletePlaylistTrackRequestValidator : AbstractValidator<PlaylistTrackItem>
{
    public DeletePlaylistTrackRequestValidator()
    {
        RuleFor(request => request.PlaylistId).GreaterThan(0);
        RuleFor(request => request.PlaylistName).NotEmpty().MaximumLength(120);
        RuleFor(request => request.TrackId).GreaterThan(0);
        RuleFor(request => request.TrackName).NotEmpty().MaximumLength(200);
    }
}
