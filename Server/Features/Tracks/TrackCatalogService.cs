using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Tracks;

public class TrackCatalogService(ChinookContext dbContext) : TrackCatalog.TrackCatalogBase
{
    public override Task<ListTracksResponse> ListTracks(ListTracksRequest request, ServerCallContext context)
    {
        return ListTracksHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetTrackByIdResponse> GetTrackById(GetTrackByIdRequest request, ServerCallContext context)
    {
        var track = await GetTrackByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetTrackByIdResponse { Track = track };
    }

    public override async Task<CreateTrackResponse> CreateTrack(CreateTrackRequest request, ServerCallContext context)
    {
        var track = await CreateTrackHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreateTrackResponse { Track = track };
    }

    public override async Task<UpdateTrackResponse> UpdateTrack(UpdateTrackRequest request, ServerCallContext context)
    {
        var track = await UpdateTrackHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdateTrackResponse { Track = track };
    }

    public override Task<DeleteTrackResponse> DeleteTrack(DeleteTrackRequest request, ServerCallContext context)
    {
        return DeleteTrackHelper.HandleAsync(dbContext, request.Track, context.CancellationToken);
    }
}
