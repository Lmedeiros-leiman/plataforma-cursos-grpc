using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.PlaylistTracks;

public static class GetPlaylistTrackByIdHelper
{
    public static async Task<PlaylistTrackItem> HandleAsync(
        ChinookContext dbContext,
        GetPlaylistTrackByIdRequest request,
        CancellationToken cancellationToken)
    {
        return await dbContext.PlaylistTracks
            .AsNoTracking()
            .Include(entity => entity.Playlist)
            .Include(entity => entity.Track)
            .Where(entity => entity.PlaylistId == request.PlaylistId && entity.TrackId == request.TrackId)
            .Select(entity => PlaylistTrackCatalogCommonHelper.ToItem(entity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Playlist track not found."));
    }
}

public class GetPlaylistTrackByIdRequestValidator : AbstractValidator<GetPlaylistTrackByIdRequest>
{
    public GetPlaylistTrackByIdRequestValidator()
    {
        RuleFor(request => request.PlaylistId).GreaterThan(0);
        RuleFor(request => request.TrackId).GreaterThan(0);
    }
}
