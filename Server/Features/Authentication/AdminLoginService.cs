using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Authentication.Admin;

public class AdminLoginService(ChinookContext dbContext, ILogger<AdminLoginService> logger) : AdminLogin.AdminLoginBase
{
    public override Task<AdminSignInReply> SignIn(AdminSignInRequest request, ServerCallContext context)
    {
        return SignInAdminHelper.HandleAsync(dbContext, logger, request, context.CancellationToken);
    }
}
