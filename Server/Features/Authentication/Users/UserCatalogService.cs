using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Authentication.Users;

public class UserCatalogService(ChinookContext dbContext) : UserCatalog.UserCatalogBase
{
    public override Task<ListUsersResponse> ListUsers(ListUsersRequest request, ServerCallContext context)
    {
        return ListUsersHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetUserByIdResponse> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        var user = await GetUserByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetUserByIdResponse { User = user };
    }

    public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        var user = await CreateUserHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreateUserResponse { User = user };
    }

    public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var user = await UpdateUserHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdateUserResponse { User = user };
    }

    public override Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        return DeleteUserHelper.HandleAsync(dbContext, request.User, context.CancellationToken);
    }
}
