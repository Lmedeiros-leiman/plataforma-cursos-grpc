using ChinookDatabase.DataModel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Server;
using Server.Infrastructure.Validation;
using Server.Interceptors;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ChinookContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Chinook") ?? "Data Source=Chinook_Sqlite.sqlite"));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton<GrpcValidationPolicyRegistry>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAndLan", policy =>
    {
        policy
            .SetIsOriginAllowed(static origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                if (uri.IsLoopback)
                {
                    return true;
                }

                if (!IPAddress.TryParse(uri.Host, out var address))
                {
                    return false;
                }

                return IsPrivateAddress(address);
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ValidationInterceptor>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("LocalAndLan");

app.MapGrpcEndpoints();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
// To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909

app.Run();

static bool IsPrivateAddress(IPAddress address)
{
    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
    {
        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
    }

    var bytes = address.GetAddressBytes();
    return bytes[0] switch
    {
        10 => true,
        172 when bytes[1] is >= 16 and <= 31 => true,
        192 when bytes[1] == 168 => true,
        _ => false
    };
}
