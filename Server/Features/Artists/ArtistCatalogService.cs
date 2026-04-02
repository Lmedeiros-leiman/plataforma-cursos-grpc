using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Artists;

public class ArtistCatalogService(ChinookContext dbContext) : ArtistCatalog.ArtistCatalogBase
{
    public override Task<ListArtistsResponse> ListArtists(ListArtistsRequest request, ServerCallContext context)
    {
        return ListArtistsHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetArtistByIdResponse> GetArtistById(GetArtistByIdRequest request, ServerCallContext context)
    {
        var artist = await GetArtistByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetArtistByIdResponse { Artist = artist };
    }

    public override async Task<CreateArtistResponse> CreateArtist(CreateArtistRequest request, ServerCallContext context)
    {
        var artist = await CreateArtistHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreateArtistResponse { Artist = artist };
    }

    public override async Task<UpdateArtistResponse> UpdateArtist(UpdateArtistRequest request, ServerCallContext context)
    {
        var artist = await UpdateArtistHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdateArtistResponse { Artist = artist };
    }

    public override Task<DeleteArtistResponse> DeleteArtist(DeleteArtistRequest request, ServerCallContext context)
    {
        return DeleteArtistHelper.HandleAsync(dbContext, request.Artist, context.CancellationToken);
    }
}
