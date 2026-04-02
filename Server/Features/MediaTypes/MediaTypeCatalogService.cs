using ChinookDatabase.DataModel;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Server.Features.MediaTypes;

public class MediaTypeCatalogService(ChinookContext dbContext) : MediaTypeCatalog.MediaTypeCatalogBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 1000;

    public override async Task<ListMediaTypesResponse> ListMediaTypes(ListMediaTypesRequest request, ServerCallContext context)
    {
        var cursor = request.Cursor < 0 ? 0 : request.Cursor;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var query = dbContext.MediaTypes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(entity => EF.Functions.Like(entity.Name, $"%{name}%"));
        }

        var page = await query
            .Where(entity => entity.MediaTypeId > cursor)
            .OrderBy(entity => entity.MediaTypeId)
            .Select(entity => new MediaTypeItem
            {
                MediaTypeId = entity.MediaTypeId,
                Name = entity.Name ?? string.Empty
            })
            .Take(pageSize + 1)
            .ToListAsync(context.CancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();

        var reply = new ListMediaTypesResponse
        {
            NextCursor = hasMore && items.Count > 0 ? items[^1].MediaTypeId : 0
        };
        reply.MediaTypes.AddRange(items);
        return reply;
    }

    public override async Task<GetMediaTypeByIdResponse> GetMediaTypeById(GetMediaTypeByIdRequest request, ServerCallContext context)
    {
        var mediaType = await dbContext.MediaTypes
            .AsNoTracking()
            .Where(entity => entity.MediaTypeId == request.MediaTypeId)
            .Select(entity => new MediaTypeItem
            {
                MediaTypeId = entity.MediaTypeId,
                Name = entity.Name ?? string.Empty
            })
            .FirstOrDefaultAsync(context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Media type not found."));

        return new GetMediaTypeByIdResponse { MediaType = mediaType };
    }

    public override async Task<CreateMediaTypeResponse> CreateMediaType(CreateMediaTypeRequest request, ServerCallContext context)
    {
        var name = request.Name.Trim();
        var duplicated = await dbContext.MediaTypes
            .AsNoTracking()
            .AnyAsync(entity => entity.Name == name, context.CancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Media type already exists."));
        }

        var entity = new MediaType { Name = name };
        dbContext.MediaTypes.Add(entity);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new CreateMediaTypeResponse
        {
            MediaType = new MediaTypeItem { MediaTypeId = entity.MediaTypeId, Name = entity.Name ?? string.Empty }
        };
    }

    public override async Task<UpdateMediaTypeResponse> UpdateMediaType(UpdateMediaTypeRequest request, ServerCallContext context)
    {
        var entity = await dbContext.MediaTypes
            .FirstOrDefaultAsync(item => item.MediaTypeId == request.MediaTypeId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Media type not found."));

        var name = request.Name.Trim();
        var duplicated = await dbContext.MediaTypes
            .AsNoTracking()
            .AnyAsync(item => item.MediaTypeId != request.MediaTypeId && item.Name == name, context.CancellationToken);

        if (duplicated)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Media type already exists."));
        }

        entity.Name = name;
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new UpdateMediaTypeResponse
        {
            MediaType = new MediaTypeItem { MediaTypeId = entity.MediaTypeId, Name = entity.Name ?? string.Empty }
        };
    }

    public override async Task<DeleteMediaTypeResponse> DeleteMediaType(DeleteMediaTypeRequest request, ServerCallContext context)
    {
        var mediaType = request.MediaType;
        var current = await GetMediaTypeById(new GetMediaTypeByIdRequest { MediaTypeId = mediaType.MediaTypeId }, context);
        if (current.MediaType.Name != mediaType.Name)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Media type data does not match the current database state."));
        }

        var hasTracks = await dbContext.Tracks
            .AsNoTracking()
            .AnyAsync(entity => entity.MediaTypeId == mediaType.MediaTypeId, context.CancellationToken);

        if (hasTracks)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Media type has tracks and cannot be deleted."));
        }

        var entity = await dbContext.MediaTypes
            .FirstOrDefaultAsync(item => item.MediaTypeId == mediaType.MediaTypeId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Media type not found."));

        dbContext.MediaTypes.Remove(entity);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new DeleteMediaTypeResponse
        {
            MediaTypeId = mediaType.MediaTypeId,
            Message = "Media type deleted successfully."
        };
    }
}

public class ListMediaTypesRequestValidator : AbstractValidator<ListMediaTypesRequest>
{
    public ListMediaTypesRequestValidator()
    {
        RuleFor(request => request.Cursor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PageSize).GreaterThanOrEqualTo(0);
    }
}

public class GetMediaTypeByIdRequestValidator : AbstractValidator<GetMediaTypeByIdRequest>
{
    public GetMediaTypeByIdRequestValidator()
    {
        RuleFor(request => request.MediaTypeId).GreaterThan(0);
    }
}

public class CreateMediaTypeRequestValidator : AbstractValidator<CreateMediaTypeRequest>
{
    public CreateMediaTypeRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}

public class UpdateMediaTypeRequestValidator : AbstractValidator<UpdateMediaTypeRequest>
{
    public UpdateMediaTypeRequestValidator()
    {
        RuleFor(request => request.MediaTypeId).GreaterThan(0);
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
    }
}

public class DeleteMediaTypeRequestValidator : AbstractValidator<DeleteMediaTypeRequest>
{
    public DeleteMediaTypeRequestValidator()
    {
        RuleFor(request => request.MediaType.MediaTypeId).GreaterThan(0);
        RuleFor(request => request.MediaType.Name).NotEmpty().MaximumLength(120);
    }
}
