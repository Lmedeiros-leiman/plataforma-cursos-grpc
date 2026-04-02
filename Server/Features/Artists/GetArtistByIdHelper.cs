using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Artists;

public static class GetArtistByIdHelper
{
    public static async Task<ArtistItem> HandleAsync(
        ChinookContext dbContext,
        GetArtistByIdRequest request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Artists
            .AsNoTracking()
            .Where(entity => entity.ArtistId == request.ArtistId)
            .Select(entity => new ArtistItem
            {
                ArtistId = entity.ArtistId,
                Name = entity.Name ?? string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Artist not found."));
    }
}

public class GetArtistByIdRequestValidator : AbstractValidator<GetArtistByIdRequest>
{
    public GetArtistByIdRequestValidator()
    {
        RuleFor(request => request.ArtistId).GreaterThan(0);
    }
}
