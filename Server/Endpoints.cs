
using Server.Features.Authentication.Admin;
using Server.Features.Authentication.User;
using Server.Features.Authentication.Admins;
using Server.Features.Authentication.Users;
using Server.Features.Albums;
using Server.Features.Artists;
using Server.Features.Genres;
using Server.Features.InvoiceLines;
using Server.Features.Invoices;
using Server.Features.MediaTypes;
using Server.Features.Playlists;
using Server.Features.PlaylistTracks;
using Server.Features.Tracks;
using Server.Infrastructure.Validation;

namespace Server;

public static class Endpoints
{
    public static void MapGrpcEndpoints(this WebApplication app)
    {
        app.MapValidatedGrpcService<AlbumCatalogService>("/albums.AlbumCatalog/");
        app.MapValidatedGrpcService<ArtistCatalogService>("/artists.ArtistCatalog/");
        app.MapValidatedGrpcService<GenreCatalogService>("/genres.GenreCatalog/");
        app.MapValidatedGrpcService<MediaTypeCatalogService>("/media_types.MediaTypeCatalog/");
        app.MapValidatedGrpcService<PlaylistCatalogService>("/playlists.PlaylistCatalog/");
        app.MapValidatedGrpcService<TrackCatalogService>("/tracks.TrackCatalog/");
        app.MapValidatedGrpcService<InvoiceCatalogService>("/invoices.InvoiceCatalog/");
        app.MapValidatedGrpcService<InvoiceLineCatalogService>("/invoice_lines.InvoiceLineCatalog/");
        app.MapValidatedGrpcService<PlaylistTrackCatalogService>("/playlist_tracks.PlaylistTrackCatalog/");
        app.MapValidatedGrpcService<UserLoginService>("/authentication.user.UserLogin/");
        app.MapValidatedGrpcService<AdminLoginService>("/authentication.admin.AdminLogin/");
        app.MapValidatedGrpcService<UserCatalogService>("/authentication.users.UserCatalog/");
        app.MapValidatedGrpcService<AdminCatalogService>("/authentication.admins.AdminCatalog/");
    }

    public static WebApplication MapValidatedGrpcService<TService>(this WebApplication app, string methodPrefix)
        where TService : class
    {
        var validationPolicies = app.Services.GetRequiredService<GrpcValidationPolicyRegistry>();
        validationPolicies.RequireValidationForPrefix(methodPrefix);
        app.MapGrpcService<TService>();

        return app;
    }
}
