using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Tracks;

public static class UpdateTrackHelper
{
    public static async Task<TrackItem> HandleAsync(
        ChinookContext dbContext,
        UpdateTrackRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Tracks
            .FirstOrDefaultAsync(item => item.TrackId == request.TrackId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Track not found."));

        await TrackCatalogCommonHelper.EnsureRelationsExistAsync(dbContext, request.AlbumId, request.MediaTypeId, request.GenreId, cancellationToken);

        var name = request.Name.Trim();
        var composer = TrackCatalogCommonHelper.NormalizeOptional(request.Composer);
        var duplicated = await dbContext.Tracks
            .AsNoTracking()
            .AnyAsync(item =>
                item.TrackId != request.TrackId &&
                item.AlbumId == request.AlbumId &&
                item.Name == name,
                cancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Another track with the same album and name already exists."));
        }

        entity.Name = name;
        entity.AlbumId = request.AlbumId;
        entity.MediaTypeId = request.MediaTypeId;
        entity.GenreId = request.GenreId;
        entity.Composer = composer;
        entity.Miliseconds = request.Miliseconds;
        entity.Bytes = request.Bytes;
        entity.UnitPrice = (decimal)request.UnitPrice;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetTrackByIdHelper.HandleAsync(dbContext, new GetTrackByIdRequest { TrackId = entity.TrackId }, cancellationToken);
    }
}

public class UpdateTrackRequestValidator : AbstractValidator<UpdateTrackRequest>
{
    public UpdateTrackRequestValidator()
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
