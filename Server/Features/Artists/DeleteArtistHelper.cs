using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Artists;

public static class DeleteArtistHelper
{
    public static async Task<DeleteArtistResponse> HandleAsync(
        ChinookContext dbContext,
        ArtistItem request,
        CancellationToken cancellationToken)
    {
        var current = await GetArtistByIdHelper.HandleAsync(
            dbContext,
            new GetArtistByIdRequest { ArtistId = request.ArtistId },
            cancellationToken);

        if (current.Name != request.Name)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Artist data does not match the current database state."));
        }

        var hasAlbums = await dbContext.Albums
            .AsNoTracking()
            .AnyAsync(entity => entity.ArtistId == request.ArtistId, cancellationToken);

        if (hasAlbums)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Artist has albums and cannot be deleted."));
        }

        var entity = await dbContext.Artists
            .FirstOrDefaultAsync(item => item.ArtistId == request.ArtistId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Artist not found."));

        dbContext.Artists.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteArtistResponse
        {
            ArtistId = request.ArtistId,
            Message = "Artist deleted successfully."
        };
    }
}

public class DeleteArtistRequestValidator : AbstractValidator<ArtistItem>
{
    public DeleteArtistRequestValidator(ChinookContext dbContext)
    {
        RuleFor(request => request.ArtistId)
            .GreaterThan(0)
            .WithMessage("O artist_id deve ser maior que zero.");

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(request => request)
            .MustAsync(async (request, cancellationToken) =>
            {
                var current = await dbContext.Artists
                    .AsNoTracking()
                    .Where(entity => entity.ArtistId == request.ArtistId)
                    .Select(entity => new ArtistItem
                    {
                        ArtistId = entity.ArtistId,
                        Name = entity.Name ?? string.Empty
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return current is not null &&
                    current.ArtistId == request.ArtistId &&
                    current.Name == request.Name;
            })
            .WithMessage("O artista informado precisa existir e ser identico ao salvo no banco.");
    }
}
