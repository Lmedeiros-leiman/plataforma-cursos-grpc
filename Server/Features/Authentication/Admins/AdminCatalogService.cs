using ChinookDatabase.DataModel;
using Grpc.Core;

namespace Server.Features.Authentication.Admins;

public class AdminCatalogService(ChinookContext dbContext) : AdminCatalog.AdminCatalogBase
{
    public override Task<ListAdminsResponse> ListAdmins(ListAdminsRequest request, ServerCallContext context)
    {
        return ListAdminsHelper.HandleAsync(dbContext, request, context.CancellationToken);
    }

    public override async Task<GetAdminByIdResponse> GetAdminById(GetAdminByIdRequest request, ServerCallContext context)
    {
        var admin = await GetAdminByIdHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new GetAdminByIdResponse { Admin = admin };
    }

    public override async Task<CreateAdminResponse> CreateAdmin(CreateAdminRequest request, ServerCallContext context)
    {
        var admin = await CreateAdminHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new CreateAdminResponse { Admin = admin };
    }

    public override async Task<UpdateAdminResponse> UpdateAdmin(UpdateAdminRequest request, ServerCallContext context)
    {
        var admin = await UpdateAdminHelper.HandleAsync(dbContext, request, context.CancellationToken);
        return new UpdateAdminResponse { Admin = admin };
    }

    public override Task<DeleteAdminResponse> DeleteAdmin(DeleteAdminRequest request, ServerCallContext context)
    {
        return DeleteAdminHelper.HandleAsync(dbContext, request.Admin, context.CancellationToken);
    }
}
