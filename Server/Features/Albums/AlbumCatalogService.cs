using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Albums;

public class AlbumCatalogService(ChinookContext dbContext) : AlbumCatalog.AlbumCatalogBase
{
    public override Task<SearchAlbumsResponse> SearchAlbums(SearchAlbumsRequest request, ServerCallContext context)
    {
        return SearchAlbumsHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetAlbumByIdResponse> GetAlbumById(GetAlbumByIdRequest request, ServerCallContext context)
    {
        var album = await GetAlbumByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetAlbumByIdResponse { Album = album };
    }

    public override async Task<CreateAlbumResponse> CreateAlbum(CreateAlbumRequest request, ServerCallContext context)
    {
        var album = await CreateAlbumHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreateAlbumResponse { Album = album };
    }

    public override async Task<UpdateAlbumResponse> UpdateAlbum(UpdateAlbumRequest request, ServerCallContext context)
    {
        var album = await UpdateAlbumHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdateAlbumResponse { Album = album };
    }

    public override Task<DeleteAlbumResponse> DeleteAlbum(DeleteAlbumRequest request, ServerCallContext context)
    {
        return DeleteAlbumHelper.HandleAsync(dbContext, request.Album, context.CancellationToken);
    }
}
