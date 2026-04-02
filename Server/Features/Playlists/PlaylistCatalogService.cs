using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Playlists;

public class PlaylistCatalogService(ChinookContext dbContext) : PlaylistCatalog.PlaylistCatalogBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public override async Task<ListPlaylistsResponse> ListPlaylists(ListPlaylistsRequest request, ServerCallContext context)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.Playlists.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(entity => EF.Functions.Like(entity.Name, $"%{name}%"));
        }

        var page = await query
            .Where(entity => entity.PlaylistId > cursor)
            .OrderBy(entity => entity.PlaylistId)
            .Select(entity => new PlaylistItem
            {
                PlaylistId = entity.PlaylistId,
                Name = entity.Name ?? string.Empty
            })
            .Take(pageSize + 1)
            .ToListAsync(context.CancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListPlaylistsResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].PlaylistId : 0
        };
        reply.Playlists.AddRange(items);
        return reply;
    }

    public override async Task<GetPlaylistByIdResponse> GetPlaylistById(GetPlaylistByIdRequest request, ServerCallContext context)
    {
        var playlist = await dbContext.Playlists
            .AsNoTracking()
            .Where(entity => entity.PlaylistId == request.PlaylistId)
            .Select(entity => new PlaylistItem
            {
                PlaylistId = entity.PlaylistId,
                Name = entity.Name ?? string.Empty
            })
            .FirstOrDefaultAsync(context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Playlist not found."));

        return new GetPlaylistByIdResponse { Playlist = playlist };
    }

    public override async Task<CreatePlaylistResponse> CreatePlaylist(CreatePlaylistRequest request, ServerCallContext context)
    {
        var name = request.Name.Trim();
        var duplicated = await dbContext.Playlists
            .AsNoTracking()
            .AnyAsync(entity => entity.Name == name, context.CancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Playlist already exists."));
        }

        var entity = new Playlist { Name = name };
        dbContext.Playlists.Add(entity);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new CreatePlaylistResponse
        {
            Playlist = new PlaylistItem { PlaylistId = entity.PlaylistId, Name = entity.Name }
        };
    }

    public override async Task<UpdatePlaylistResponse> UpdatePlaylist(UpdatePlaylistRequest request, ServerCallContext context)
    {
        var entity = await dbContext.Playlists
            .FirstOrDefaultAsync(item => item.PlaylistId == request.PlaylistId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Playlist not found."));

        var name = request.Name.Trim();
        var duplicated = await dbContext.Playlists
            .AsNoTracking()
            .AnyAsync(item => item.PlaylistId != request.PlaylistId && item.Name == name, context.CancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Playlist already exists."));
        }

        entity.Name = name;
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new UpdatePlaylistResponse
        {
            Playlist = new PlaylistItem { PlaylistId = entity.PlaylistId, Name = entity.Name }
        };
    }

    public override async Task<DeletePlaylistResponse> DeletePlaylist(DeletePlaylistRequest request, ServerCallContext context)
    {
        var playlist = request.Playlist;
        var current = await GetPlaylistById(new GetPlaylistByIdRequest { PlaylistId = playlist.PlaylistId }, context);
        if (current.Playlist.Name != playlist.Name)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Playlist data does not match the current database state."));
        }

        var hasTracks = await dbContext.PlaylistTracks
            .AsNoTracking()
            .AnyAsync(entity => entity.PlaylistId == playlist.PlaylistId, context.CancellationToken);

        if (hasTracks)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Playlist has tracks and cannot be deleted."));
        }

        var entity = await dbContext.Playlists
            .FirstOrDefaultAsync(item => item.PlaylistId == playlist.PlaylistId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Playlist not found."));

        dbContext.Playlists.Remove(entity);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new DeletePlaylistResponse
        {
            PlaylistId = playlist.PlaylistId,
            Message = "Playlist deleted successfully."
        };
    }
}

public class ListPlaylistsRequestValidator : AbstractValidator<ListPlaylistsRequest>
{
    public ListPlaylistsRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
    }
}

public class GetPlaylistByIdRequestValidator : AbstractValidator<GetPlaylistByIdRequest>
{
    public GetPlaylistByIdRequestValidator()
    {
        RuleFor(request => request.PlaylistId).GreaterThan(0);
    }
}

public class CreatePlaylistRequestValidator : AbstractValidator<CreatePlaylistRequest>
{
    public CreatePlaylistRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}

public class UpdatePlaylistRequestValidator : AbstractValidator<UpdatePlaylistRequest>
{
    public UpdatePlaylistRequestValidator()
    {
        RuleFor(request => request.PlaylistId).GreaterThan(0);
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}

public class DeletePlaylistRequestValidator : AbstractValidator<DeletePlaylistRequest>
{
    public DeletePlaylistRequestValidator()
    {
        RuleFor(request => request.Playlist.PlaylistId).GreaterThan(0);
        RuleFor(request => request.Playlist.Name).NotEmpty().MaximumLength(120);
    }
}
