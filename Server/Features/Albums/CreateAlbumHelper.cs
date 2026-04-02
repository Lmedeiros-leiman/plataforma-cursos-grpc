using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Albums;

public static class CreateAlbumHelper
{
    public static async Task<AlbumItem> HandleAsync(
        ChinookContext dbContext,
        CreateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        var artistExists = await dbContext.Artists
            .AsNoTracking()
            .AnyAsync(entity => entity.ArtistId == request.ArtistId, cancellationToken);

        if (!artistExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Artist not found."));
        }

        var trimmedTitle = request.Title.Trim();
        var duplicatedAlbumExists = await dbContext.Albums
            .AsNoTracking()
            .AnyAsync(entity =>
                entity.ArtistId == request.ArtistId &&
                entity.Title == trimmedTitle,
                cancellationToken);

        if (duplicatedAlbumExists)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Another album with the same title and artist already exists."));
        }

        var album = new Album
        {
            Title = trimmedTitle,
            ArtistId = request.ArtistId
        };

        dbContext.Albums.Add(album);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAlbumByIdHelper.HandleAsync(
            dbContext,
            new GetAlbumByIdRequest { AlbumId = album.AlbumId },
            cancellationToken);
    }
}

public class CreateAlbumRequestValidator : AbstractValidator<CreateAlbumRequest>
{
    public CreateAlbumRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .WithMessage("O titulo e obrigatorio.")
            .MaximumLength(160)
            .WithMessage("O titulo deve ter no maximo 160 caracteres.");

        RuleFor(request => request.ArtistId)
            .GreaterThan(0)
            .WithMessage("O artist_id deve ser maior que zero.");
    }
}
