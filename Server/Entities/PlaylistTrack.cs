using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ChinookDatabase.DataModel
{
    [PrimaryKey(nameof(PlaylistId), nameof(TrackId))]
    [DebuggerDisplay("PlaylistId = {PlaylistId}, TrackId = {TrackId}")]
    public class PlaylistTrack
    {
        public int PlaylistId { get; set; }

        public int TrackId { get; set; }

        [ForeignKey("PlaylistId")]
        public Playlist Playlist { get; set; }

        [ForeignKey("TrackId")]
        public Track Track { get; set; }
    }
}
