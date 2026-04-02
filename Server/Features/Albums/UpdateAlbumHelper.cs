using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Albums;

public static class UpdateAlbumHelper
{
    public static async Task<AlbumItem> HandleAsync(
        ChinookContext dbContext,
        UpdateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        var album = await dbContext.Albums
            .FirstOrDefaultAsync(entity => entity.AlbumId == request.AlbumId, cancellationToken);

        if (album is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Album not found."));
        }

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
                entity.AlbumId != request.AlbumId &&
                entity.ArtistId == request.ArtistId &&
                entity.Title == trimmedTitle,
                cancellationToken);

        if (duplicatedAlbumExists)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Another album with the same title and artist already exists."));
        }

        album.Title = trimmedTitle;
        album.ArtistId = request.ArtistId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAlbumByIdHelper.HandleAsync(
            dbContext,
            new GetAlbumByIdRequest { AlbumId = album.AlbumId },
            cancellationToken);
    }
}

public class UpdateAlbumRequestValidator : AbstractValidator<UpdateAlbumRequest>
{
    public UpdateAlbumRequestValidator()
    {
        RuleFor(request => request.AlbumId)
            .GreaterThan(0)
            .WithMessage("O album_id deve ser maior que zero.");

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
