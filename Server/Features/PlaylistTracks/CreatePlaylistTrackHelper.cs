using ChinookDatabase.DataModel;
using FluentValidation;

namespace Server.Features.PlaylistTracks;

public static class CreatePlaylistTrackHelper
{
    public static async Task<PlaylistTrackItem> HandleAsync(
        ChinookContext dbContext,
        CreatePlaylistTrackRequest request,
        CancellationToken cancellationToken)
    {
        await PlaylistTrackCatalogCommonHelper.EnsureRelationsExistAsync(dbContext, request.PlaylistId, request.TrackId, cancellationToken);
        await PlaylistTrackCatalogCommonHelper.EnsurePairDoesNotExistAsync(dbContext, request.PlaylistId, request.TrackId, cancellationToken);

        var entity = new PlaylistTrack
        {
            PlaylistId = request.PlaylistId,
            TrackId = request.TrackId
        };

        dbContext.PlaylistTracks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetPlaylistTrackByIdHelper.HandleAsync(
            dbContext,
            new GetPlaylistTrackByIdRequest { PlaylistId = entity.PlaylistId, TrackId = entity.TrackId },
            cancellationToken);
    }
}

public class CreatePlaylistTrackRequestValidator : AbstractValidator<CreatePlaylistTrackRequest>
{
    public CreatePlaylistTrackRequestValidator()
    {
        RuleFor(request => request.PlaylistId).GreaterThan(0);
        RuleFor(request => request.TrackId).GreaterThan(0);
    }
}
