namespace spotify_stats_app.Models
{
    public class TopStream
    {
        public string? trackName { get; set; }
        public string? artistName { get; set; }
        public string? albumName { get; set; }
        public int duration { get; set; }
    }
}
