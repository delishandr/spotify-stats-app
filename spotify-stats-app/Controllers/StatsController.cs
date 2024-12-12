using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using spotify_stats_app.Models;
using System.Reflection;

namespace spotify_stats_app.Controllers
{
    public class StatsController : Controller
    {
        private readonly IWebHostEnvironment hostingEnvironment;
        private string strFullPath;
        DirectoryInfo fullPath;
        DirectoryInfo savedPath;

        public StatsController(IWebHostEnvironment _hostingEnvironment)
        {
            hostingEnvironment = _hostingEnvironment;
            strFullPath = Path.Combine(hostingEnvironment.ContentRootPath, "wwwroot/jsondata");
            fullPath = new DirectoryInfo(strFullPath);
            string saveDir = Path.Combine(strFullPath, "saved");
            savedPath = new DirectoryInfo(saveDir);
        }

        public IActionResult LoadStreamFiles()
        {
            // delete "saved" folder and create new empty folder
            foreach (DirectoryInfo di in fullPath.GetDirectories())
            {
                di.Delete(true);
            }
            string saveDir = Path.Combine(strFullPath, "saved");
            Directory.CreateDirectory(saveDir);
            
            // save all data to list
            List<StreamData> allData = new List<StreamData>();
            IEnumerable<FileInfo> Files = fullPath.GetFiles("*.json");
            {
                foreach (FileInfo file in Files)
                {
                    var jsonData = System.IO.File.ReadAllText(file.FullName);
                    string data = JsonConvert.SerializeObject(new { streamData = jsonData });
                    var streams = JsonConvert.DeserializeObject<List<StreamData>>(jsonData);

                    allData.AddRange(streams);
                }
            }

            int totalData = allData.Count();

            // split each data to yearly, remove streams with null trackname values (podcast streams)
            int earliest = allData[0].ts.Year;
            int curYear = DateTime.Now.Year;

            for (int i = earliest; i <= curYear; i++)
            {
                List<StreamData> yearlyData = allData
                    .Where(d => d.ts.Year == i && d.master_metadata_track_name != null).ToList();
                string dataJson = JsonConvert.SerializeObject(yearlyData);
                System.IO.File.WriteAllText(Path.Combine(saveDir, $"Yearly_Data_{i}.json"), dataJson);
            }

            return RedirectToAction("Index");
        }

        private List<StreamData> GetAllData(DateTime start, DateTime end)
        {

            List<StreamData> allData = new List<StreamData>();
            IEnumerable<FileInfo> Files = savedPath.GetFiles("*.json");

            foreach (FileInfo file in Files)
            {
                var jsonData = System.IO.File.ReadAllText(file.FullName);
                string data = JsonConvert.SerializeObject(new { streamData = jsonData });
                var streams = JsonConvert.DeserializeObject<List<StreamData>>(jsonData);

                allData.AddRange(streams);
            }
            allData = allData.Where(d => d.ts >= start && d.ts <= end).ToList();

            return allData;
        }

        private DateTime[] GetStartEnd(DateTime? startPeriod, DateTime? endPeriod, string period)
        {
            DateTime start, end = DateTime.Now;

            switch (period)
            {
                case "weekly":
                    start = DateTime.Now.AddDays(-7);
                    break;
                case "monthly":
                    start = DateTime.Now.AddMonths(-1);
                    break;
                case "yearly":
                    start = DateTime.Now.AddYears(-1);
                    break;
                case "thisyear":
                    start = DateTime.Parse("2024-01-01");
                    break;
                case "custom":
                    start = startPeriod.Value;
                    end = endPeriod.Value;
                    break;
                default:
                    start = DateTime.Parse("1970-01-01");
                    break;

            }

            return new DateTime[] { start, end };
        }

        public IActionResult TopTracks(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

            List<TopStream> topTracks = (
                from s in streams
                group s.ms_played by new { s.master_metadata_track_name, s.master_metadata_album_artist_name }
                    into t
                select new TopStream()
                {
                    trackName = t.Key.master_metadata_track_name,
                    artistName = t.Key.master_metadata_album_artist_name,
                    duration = t.Sum()
                }).OrderByDescending(t => t.duration).ToList();

            topTracks = topTracks.GetRange(0, 20);

            ViewBag.Title = "Your Top Tracks";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];

            return View(topTracks);
        }

        public IActionResult TopArtists(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

            List<TopStream> topArtists = (
                from s in streams
                group s.ms_played by new { s.master_metadata_album_artist_name }
                    into t
                select new TopStream()
                {
                    artistName = t.Key.master_metadata_album_artist_name,
                    duration = t.Sum()
                }).OrderByDescending(t => t.duration).ToList();

            topArtists = topArtists.GetRange(0, 20);

            ViewBag.Title = "Your Top Artists";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];

            return View(topArtists);
        }

        public IActionResult TopAlbums(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

            List<TopStream> topAlbums = (
                from s in streams
                group s.ms_played by new { s.master_metadata_album_album_name, s.master_metadata_album_artist_name }
                    into t
                select new TopStream()
                {
                    albumName = t.Key.master_metadata_album_album_name,
                    artistName = t.Key.master_metadata_album_artist_name,
                    duration = t.Sum()
                }).OrderByDescending(t => t.duration).ToList();

            topAlbums = topAlbums.GetRange(0, 20);

            ViewBag.Title = "Your Top Albums";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];

            return View(topAlbums);
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Success!";

            return View();
        }
    }
}
