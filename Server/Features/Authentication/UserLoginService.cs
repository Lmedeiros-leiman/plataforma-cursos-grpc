using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Authentication.User;

public class UserLoginService(ChinookContext dbContext, ILogger<UserLoginService> logger) : UserLogin.UserLoginBase
{
    public override Task<UserSignInReply> SignIn(UserSignInRequest request, ServerCallContext context)
    {
        return SignInUserHelper.HandleAsync(dbContext, logger, request, context.CancellationToken);
    }
}
