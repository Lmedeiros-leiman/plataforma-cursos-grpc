using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Tracks;

public static class DeleteTrackHelper
{
    public static async Task<DeleteTrackResponse> HandleAsync(
        ChinookContext dbContext,
        TrackItem request,
        CancellationToken cancellationToken)
    {
        var current = await GetTrackByIdHelper.HandleAsync(dbContext, new GetTrackByIdRequest { TrackId = request.TrackId }, cancellationToken);
        if (!TrackCatalogCommonHelper.Matches(current, request))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Track data does not match the current database state."));
        }

        var isReferencedByPlaylist = await dbContext.PlaylistTracks
            .AsNoTracking()
            .AnyAsync(entity => entity.TrackId == request.TrackId, cancellationToken);

        if (isReferencedByPlaylist)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Track belongs to playlists and cannot be deleted."));
        }

        var isReferencedByInvoiceLines = await dbContext.InvoiceLines
            .AsNoTracking()
            .AnyAsync(entity => entity.TrackId == request.TrackId, cancellationToken);

        if (isReferencedByInvoiceLines)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Track belongs to invoice lines and cannot be deleted."));
        }

        var entity = await dbContext.Tracks
            .FirstOrDefaultAsync(item => item.TrackId == request.TrackId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Track not found."));

        dbContext.Tracks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteTrackResponse
        {
            TrackId = request.TrackId,
            Message = "Track deleted successfully."
        };
    }
}

public class DeleteTrackRequestValidator : AbstractValidator<TrackItem>
{
    public DeleteTrackRequestValidator()
    {
        RuleFor(request => request.TrackId).GreaterThan(0);
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.AlbumId).GreaterThan(0);
        RuleFor(request => request.MediaTypeId).GreaterThan(0);
        RuleFor(request => request.GenreId).GreaterThan(0);
        RuleFor(request => request.Composer).MaximumLength(220);
        RuleFor(request => request.Miliseconds).GreaterThan(0);
        RuleFor(request => request.Bytes).GreaterThanOrEqualTo(0);
        RuleFor(request => request.UnitPrice).GreaterThanOrEqualTo(0);
    }
}
