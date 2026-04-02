using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Tracks;

public static class GetTrackByIdHelper
{
    public static async Task<TrackItem> HandleAsync(
        ChinookContext dbContext,
        GetTrackByIdRequest request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Tracks
            .AsNoTracking()
            .Include(entity => entity.Album)
            .Include(entity => entity.MediaType)
            .Include(entity => entity.Genre)
            .Where(entity => entity.TrackId == request.TrackId)
            .Select(entity => TrackCatalogCommonHelper.ToItem(entity))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Track not found."));
    }
}

public class GetTrackByIdRequestValidator : AbstractValidator<GetTrackByIdRequest>
{
    public GetTrackByIdRequestValidator()
    {
        RuleFor(request => request.TrackId).GreaterThan(0);
    }
}
