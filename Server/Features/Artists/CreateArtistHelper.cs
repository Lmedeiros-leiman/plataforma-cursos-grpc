using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Artists;

public static class CreateArtistHelper
{
    public static async Task<ArtistItem> HandleAsync(
        ChinookContext dbContext,
        CreateArtistRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var duplicated = await dbContext.Artists
            .AsNoTracking()
            .AnyAsync(entity => entity.Name == name, cancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Artist already exists."));
        }

        var entity = new Artist { Name = name };
        dbContext.Artists.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArtistItem { ArtistId = entity.ArtistId, Name = entity.Name };
    }
}

public class CreateArtistRequestValidator : AbstractValidator<CreateArtistRequest>
{
    public CreateArtistRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}
