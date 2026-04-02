using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Artists;

public static class UpdateArtistHelper
{
    public static async Task<ArtistItem> HandleAsync(
        ChinookContext dbContext,
        UpdateArtistRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Artists
            .FirstOrDefaultAsync(item => item.ArtistId == request.ArtistId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Artist not found."));

        var name = request.Name.Trim();
        var duplicated = await dbContext.Artists
            .AsNoTracking()
            .AnyAsync(item => item.ArtistId != request.ArtistId && item.Name == name, cancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Artist already exists."));
        }

        entity.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArtistItem { ArtistId = entity.ArtistId, Name = entity.Name };
    }
}

public class UpdateArtistRequestValidator : AbstractValidator<UpdateArtistRequest>
{
    public UpdateArtistRequestValidator(ChinookContext dbContext)
    {
        RuleFor(request => request.ArtistId)
            .GreaterThan(0)
            .MustAsync(async (artistId, cancellationToken) =>
                await dbContext.Artists.AsNoTracking().AnyAsync(entity => entity.ArtistId == artistId, cancellationToken))
            .WithMessage("O artist_id informado nao existe.");

        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}
