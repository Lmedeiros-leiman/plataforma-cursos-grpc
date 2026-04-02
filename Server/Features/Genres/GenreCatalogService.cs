using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.Genres;

public class GenreCatalogService(ChinookContext dbContext) : GenreCatalog.GenreCatalogBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public override async Task<ListGenresResponse> ListGenres(ListGenresRequest request, ServerCallContext context)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.Genres.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(entity => EF.Functions.Like(entity.Name, $"%{name}%"));
        }

        var page = await query
            .Where(entity => entity.GenreId > cursor)
            .OrderBy(entity => entity.GenreId)
            .Select(entity => new GenreItem
            {
                GenreId = entity.GenreId,
                Name = entity.Name ?? string.Empty
            })
            .Take(pageSize + 1)
            .ToListAsync(context.CancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListGenresResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].GenreId : 0
        };
        reply.Genres.AddRange(items);
        return reply;
    }

    public override async Task<GetGenreByIdResponse> GetGenreById(GetGenreByIdRequest request, ServerCallContext context)
    {
        var genre = await dbContext.Genres
            .AsNoTracking()
            .Where(entity => entity.GenreId == request.GenreId)
            .Select(entity => new GenreItem
            {
                GenreId = entity.GenreId,
                Name = entity.Name ?? string.Empty
            })
            .FirstOrDefaultAsync(context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Genre not found."));

        return new GetGenreByIdResponse { Genre = genre };
    }

    public override async Task<CreateGenreResponse> CreateGenre(CreateGenreRequest request, ServerCallContext context)
    {
        var name = request.Name.Trim();
        var duplicated = await dbContext.Genres
            .AsNoTracking()
            .AnyAsync(entity => entity.Name == name, context.CancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Genre already exists."));
        }

        var entity = new Genre { Name = name };
        dbContext.Genres.Add(entity);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new CreateGenreResponse
        {
            Genre = new GenreItem { GenreId = entity.GenreId, Name = entity.Name ?? string.Empty }
        };
    }

    public override async Task<UpdateGenreResponse> UpdateGenre(UpdateGenreRequest request, ServerCallContext context)
    {
        var entity = await dbContext.Genres
            .FirstOrDefaultAsync(item => item.GenreId == request.GenreId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Genre not found."));

        var name = request.Name.Trim();
        var duplicated = await dbContext.Genres
            .AsNoTracking()
            .AnyAsync(item => item.GenreId != request.GenreId && item.Name == name, context.CancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Genre already exists."));
        }

        entity.Name = name;
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new UpdateGenreResponse
        {
            Genre = new GenreItem { GenreId = entity.GenreId, Name = entity.Name ?? string.Empty }
        };
    }

    public override async Task<DeleteGenreResponse> DeleteGenre(DeleteGenreRequest request, ServerCallContext context)
    {
        var genre = request.Genre;
        var current = await GetGenreById(new GetGenreByIdRequest { GenreId = genre.GenreId }, context);
        if (current.Genre.Name != genre.Name)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Genre data does not match the current database state."));
        }

        var hasTracks = await dbContext.Tracks
            .AsNoTracking()
            .AnyAsync(entity => entity.GenreId == genre.GenreId, context.CancellationToken);

        if (hasTracks)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Genre has tracks and cannot be deleted."));
        }

        var entity = await dbContext.Genres
            .FirstOrDefaultAsync(item => item.GenreId == genre.GenreId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Genre not found."));

        dbContext.Genres.Remove(entity);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new DeleteGenreResponse
        {
            GenreId = genre.GenreId,
            Message = "Genre deleted successfully."
        };
    }
}

public class ListGenresRequestValidator : AbstractValidator<ListGenresRequest>
{
    public ListGenresRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
    }
}

public class GetGenreByIdRequestValidator : AbstractValidator<GetGenreByIdRequest>
{
    public GetGenreByIdRequestValidator()
    {
        RuleFor(request => request.GenreId).GreaterThan(0);
    }
}

public class CreateGenreRequestValidator : AbstractValidator<CreateGenreRequest>
{
    public CreateGenreRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}

public class UpdateGenreRequestValidator : AbstractValidator<UpdateGenreRequest>
{
    public UpdateGenreRequestValidator()
    {
        RuleFor(request => request.GenreId).GreaterThan(0);
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}

public class DeleteGenreRequestValidator : AbstractValidator<DeleteGenreRequest>
{
    public DeleteGenreRequestValidator()
    {
        RuleFor(request => request.Genre.GenreId).GreaterThan(0);
        RuleFor(request => request.Genre.Name).NotEmpty().MaximumLength(120);
    }
}
