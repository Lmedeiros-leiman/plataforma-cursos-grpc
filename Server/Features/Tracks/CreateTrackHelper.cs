using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Tracks;

public static class CreateTrackHelper
{
    public static async Task<TrackItem> HandleAsync(
        ChinookContext dbContext,
        CreateTrackRequest request,
        CancellationToken cancellationToken)
    {
        await TrackCatalogCommonHelper.EnsureRelationsExistAsync(dbContext, request.AlbumId, request.MediaTypeId, request.GenreId, cancellationToken);

        var name = request.Name.Trim();
        var composer = TrackCatalogCommonHelper.NormalizeOptional(request.Composer);
        var duplicated = await dbContext.Tracks
            .AsNoTracking()
            .AnyAsync(entity => entity.AlbumId == request.AlbumId && entity.Name == name, cancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Another track with the same album and name already exists."));
        }

        var entity = new Track
        {
            Name = name,
            AlbumId = request.AlbumId,
            MediaTypeId = request.MediaTypeId,
            GenreId = request.GenreId,
            Composer = composer,
            Miliseconds = request.Miliseconds,
            Bytes = request.Bytes,
            UnitPrice = (decimal)request.UnitPrice
        };

        dbContext.Tracks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetTrackByIdHelper.HandleAsync(dbContext, new GetTrackByIdRequest { TrackId = entity.TrackId }, cancellationToken);
    }
}

public class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    public CreateTrackRequestValidator()
    {
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
