using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Albums;

public static class DeleteAlbumHelper
{
    public static async Task<DeleteAlbumResponse> HandleAsync(
        ChinookContext dbContext,
        AlbumItem request,
        CancellationToken cancellationToken)
    {
        var currentAlbum = await GetAlbumByIdHelper.HandleAsync(
            dbContext,
            new GetAlbumByIdRequest { AlbumId = request.AlbumId },
            cancellationToken);

        if (!Matches(currentAlbum, request))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Album data does not match the current database state."));
        }

        var album = await dbContext.Albums
            .FirstOrDefaultAsync(entity => entity.AlbumId == request.AlbumId, cancellationToken);

        if (album is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Album not found."));
        }

        var hasTracks = await dbContext.Tracks
            .AsNoTracking()
            .AnyAsync(entity => entity.AlbumId == request.AlbumId, cancellationToken);

        if (hasTracks)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Album has tracks and cannot be deleted."));
        }

        dbContext.Albums.Remove(album);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteAlbumResponse
        {
            AlbumId = request.AlbumId,
            Message = "Album deleted successfully."
        };
    }

    private static bool Matches(AlbumItem currentAlbum, AlbumItem incomingAlbum)
    {
        if (currentAlbum.AlbumId != incomingAlbum.AlbumId ||
            currentAlbum.Title != incomingAlbum.Title ||
            currentAlbum.ArtistId != incomingAlbum.ArtistId ||
            currentAlbum.ArtistName != incomingAlbum.ArtistName ||
            currentAlbum.Tracks.Count != incomingAlbum.Tracks.Count)
        {
            return false;
        }

        for (var index = 0; index < currentAlbum.Tracks.Count; index++)
        {
            var currentTrack = currentAlbum.Tracks[index];
            var incomingTrack = incomingAlbum.Tracks[index];

            if (currentTrack.TrackId != incomingTrack.TrackId ||
                currentTrack.Name != incomingTrack.Name ||
                currentTrack.GenreId != incomingTrack.GenreId ||
                currentTrack.GenreName != incomingTrack.GenreName ||
                currentTrack.Composer != incomingTrack.Composer ||
                currentTrack.Milliseconds != incomingTrack.Milliseconds ||
                currentTrack.UnitPrice != incomingTrack.UnitPrice)
            {
                return false;
            }
        }

        return true;
    }
}

public class DeleteAlbumRequestValidator : AbstractValidator<AlbumItem>
{
    public DeleteAlbumRequestValidator()
    {
        RuleFor(request => request.AlbumId)
            .GreaterThan(0)
            .WithMessage("O album_id deve ser maior que zero.");

        RuleFor(request => request.Title)
            .NotEmpty()
            .WithMessage("O titulo e obrigatorio.");

        RuleFor(request => request.ArtistId)
            .GreaterThan(0)
            .WithMessage("O artist_id deve ser maior que zero.");
    }
}
