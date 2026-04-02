using Microsoft.EntityFrameworkCore;

namespace ChinookDatabase.DataModel;

public class ChinookContext(DbContextOptions<ChinookContext> options) : DbContext(options)
{
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<MediaType> MediaTypes => Set<MediaType>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
    public DbSet<Track> Tracks => Set<Track>();
}
