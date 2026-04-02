using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.PlaylistTracks;

public class PlaylistTrackCatalogService(ChinookContext dbContext) : PlaylistTrackCatalog.PlaylistTrackCatalogBase
{
    public override Task<ListPlaylistTracksResponse> ListPlaylistTracks(ListPlaylistTracksRequest request, ServerCallContext context)
    {
        return ListPlaylistTracksHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetPlaylistTrackByIdResponse> GetPlaylistTrackById(GetPlaylistTrackByIdRequest request, ServerCallContext context)
    {
        var playlistTrack = await GetPlaylistTrackByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetPlaylistTrackByIdResponse { PlaylistTrack = playlistTrack };
    }

    public override async Task<CreatePlaylistTrackResponse> CreatePlaylistTrack(CreatePlaylistTrackRequest request, ServerCallContext context)
    {
        var playlistTrack = await CreatePlaylistTrackHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreatePlaylistTrackResponse { PlaylistTrack = playlistTrack };
    }

    public override async Task<UpdatePlaylistTrackResponse> UpdatePlaylistTrack(UpdatePlaylistTrackRequest request, ServerCallContext context)
    {
        var playlistTrack = await UpdatePlaylistTrackHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdatePlaylistTrackResponse { PlaylistTrack = playlistTrack };
    }

    public override Task<DeletePlaylistTrackResponse> DeletePlaylistTrack(DeletePlaylistTrackRequest request, ServerCallContext context)
    {
        return DeletePlaylistTrackHelper.HandleAsync(dbContext, request.PlaylistTrack, context.CancellationToken);
    }
}
