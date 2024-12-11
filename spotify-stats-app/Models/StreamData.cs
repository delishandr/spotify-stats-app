namespace spotify_stats_app.Models
{
    public class StreamData
    {
        public DateTime ts { get; set; }
        public int ms_played { get; set; }
        public string? master_metadata_track_name { get; set; }
        public string? master_metadata_album_artist_name { get; set; }
        public string? master_metadata_album_album_name { get; set; }
        public string? spotify_track_uri { get; set; }
        public string? reason_start { get; set; }
        public string? reason_end { get; set; }
        public bool shuffle { get; set; }
        public bool skipped { get; set; }
        public bool offline { get; set; }
        public long? offline_timestamp { get; set; }
        public bool incognito_mode { get; set; }
    }
}
