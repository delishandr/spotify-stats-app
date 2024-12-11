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

        private int LoadStreamFiles()
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

            return totalData;
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

        public IActionResult TopTracks(DateTime? startPeriod = null, DateTime? endPeriod = null)
        {
            DateTime start = startPeriod == null ? DateTime.Parse("2024-01-01") : startPeriod.Value;
            DateTime end = endPeriod == null ? DateTime.Now : endPeriod.Value;

            List<StreamData> streams = GetAllData(start, end);

            List<TopTrack> topTracks = (
                from s in streams
                group s.ms_played by new { s.master_metadata_track_name, s.master_metadata_album_artist_name }
                    into t
                select new TopTrack()
                {
                    trackName = t.Key.master_metadata_track_name,
                    artistName = t.Key.master_metadata_album_artist_name,
                    duration = t.Sum()
                }).OrderByDescending(t => t.duration).ToList();

            return View();
        }

        public IActionResult Index()
        {
            int totalData = LoadStreamFiles();

            ViewBag.TotalData = totalData;
            ViewBag.Title = "Success!";

            return View();
        }
    }
}
