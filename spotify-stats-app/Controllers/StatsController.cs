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
        private int pageSize;

        public StatsController(IWebHostEnvironment _hostingEnvironment, IConfiguration _config)
        {
            hostingEnvironment = _hostingEnvironment;
            strFullPath = Path.Combine(hostingEnvironment.ContentRootPath, "wwwroot/jsondata");
            fullPath = new DirectoryInfo(strFullPath);

            pageSize = int.Parse(_config["PageSize"]);
        }

        public IActionResult LoadStreamFiles()
        {
            // TODO: load files from the input to jsondata dir

            return RedirectToAction("Index");
        }

        private List<StreamData> GetAllData(DateTime start, DateTime end)
        {

            List<StreamData> allData = new List<StreamData>();
            IEnumerable<FileInfo> Files = fullPath.GetFiles("*.json");

            foreach (FileInfo file in Files)
            {
                var jsonData = System.IO.File.ReadAllText(file.FullName);
                string data = JsonConvert.SerializeObject(new { streamData = jsonData });
                var streams = JsonConvert.DeserializeObject<List<StreamData>>(jsonData);

                allData.AddRange(streams);
            }
            allData = allData.Where(d => d.ts >= start && d.ts <= end && d.master_metadata_track_name != null).ToList();

            return allData;
        }

        private DateTime[] GetStartEnd(DateTime? startPeriod, DateTime? endPeriod, string period)
        {
            DateTime start, end = DateTime.Now;

            switch (period)
            {
                case "monthly":
                    start = DateTime.Now.AddMonths(-1);
                    break;
                case "yearly":
                    start = DateTime.Now.AddYears(-1);
                    break;
                case "thisyear":
                    start = DateTime.Parse("2024-01-01");
                    break;
                case "lifetime":
                    start = DateTime.Parse("1970-01-01");
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

            if (period == "lifetime")
            {
                datePeriod[0] = streams[0].ts;
            }

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

            topTracks = topTracks.GetRange(0, Math.Min(20, topTracks.Count));

            ViewBag.Title = "Your Top Tracks";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];
            ViewBag.Period = period;

            return View(topTracks);
        }

		public IActionResult TopTracksAll(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", int page = 1)
		{
			DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
			List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

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

			// pagination
			int startIdx = (page - 1) * pageSize;

			ViewBag.TotalItems = topTracks.Count;
			ViewBag.Page = page;
			ViewBag.StartItem = startIdx + 1;
			ViewBag.PageSize = Math.Min(pageSize, topTracks.Count - (page - 1) * pageSize);
			ViewBag.TotalPages = Math.Ceiling(topTracks.Count / (double)pageSize);

			topTracks = topTracks.GetRange(startIdx, ViewBag.PageSize);

			int first = 1, end = (int)ViewBag.TotalPages;

			if (ViewBag.TotalPages > 15)
			{
				first = Math.Max(1, page - 7);
				end = Math.Min(end, page + 7);
			}

			ViewBag.FirstPage = first;
			ViewBag.LastPage = end;

			ViewBag.Title = "Your Top Tracks";
			ViewBag.StartPeriod = datePeriod[0];
			ViewBag.EndPeriod = datePeriod[1];
			ViewBag.Period = period;

			return View(topTracks);
		}

		public IActionResult TopArtists(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

            if (period == "lifetime")
            {
                datePeriod[0] = streams[0].ts;
            }

            List<TopStream> topArtists = (
                from s in streams
                group s.ms_played by new { s.master_metadata_album_artist_name }
                    into t
                select new TopStream()
                {
                    artistName = t.Key.master_metadata_album_artist_name,
                    duration = t.Sum()
                }).OrderByDescending(t => t.duration).ToList();

            topArtists = topArtists.GetRange(0, Math.Min(20, topArtists.Count));

            ViewBag.Title = "Your Top Artists";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];
            ViewBag.Period = period;

            return View(topArtists);
        }

        public IActionResult TopArtistsAll(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", int page = 1)
        {
			DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
			List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

			List<TopStream> topArtists = (
				from s in streams
				group s.ms_played by new { s.master_metadata_album_artist_name }
					into t
				select new TopStream()
				{
					artistName = t.Key.master_metadata_album_artist_name,
					duration = t.Sum()
				}).OrderByDescending(t => t.duration).ToList();

            // pagination
			int startIdx = (page - 1) * pageSize;

            ViewBag.TotalItems = topArtists.Count;
            ViewBag.Page = page;
            ViewBag.StartItem = startIdx + 1;
			ViewBag.PageSize = Math.Min(pageSize, topArtists.Count - (page - 1) * pageSize);
			ViewBag.TotalPages = Math.Ceiling(topArtists.Count / (double)pageSize);

            topArtists = topArtists.GetRange(startIdx, ViewBag.PageSize);

            int first = 1, end = (int)ViewBag.TotalPages;


			if (ViewBag.TotalPages > 15)
            {
                first = Math.Max(1, page - 7);
                end = Math.Min(end, page + 7);
            }

            ViewBag.FirstPage = first;
            ViewBag.LastPage = end;

			ViewBag.Title = "Your Top Artists";
			ViewBag.StartPeriod = datePeriod[0];
			ViewBag.EndPeriod = datePeriod[1];
            ViewBag.Period = period;

			return View(topArtists);
		}

        public IActionResult TopAlbums(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

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

            topAlbums = topAlbums.GetRange(0, Math.Min(20, topAlbums.Count));

            ViewBag.Title = "Your Top Albums";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];
			ViewBag.Period = period;

			return View(topAlbums);
        }

		public IActionResult TopAlbumsAll(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", int page = 1)
		{
			DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
			List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

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

			// pagination
			int startIdx = (page - 1) * pageSize;

			ViewBag.TotalItems = topAlbums.Count;
			ViewBag.Page = page;
			ViewBag.StartItem = startIdx + 1;
			ViewBag.PageSize = Math.Min(pageSize, topAlbums.Count - (page - 1) * pageSize);
			ViewBag.TotalPages = Math.Ceiling(topAlbums.Count / (double)pageSize);

			topAlbums = topAlbums.GetRange(startIdx, ViewBag.PageSize);

			int first = 1, end = (int)ViewBag.TotalPages;


			if (ViewBag.TotalPages > 15)
			{
				first = Math.Max(1, page - 7);
				end = Math.Min(end, page + 7);
			}

			ViewBag.FirstPage = first;
			ViewBag.LastPage = end;

			ViewBag.Title = "Your Top Albums";
			ViewBag.StartPeriod = datePeriod[0];
			ViewBag.EndPeriod = datePeriod[1];
			ViewBag.Period = period;

			return View(topAlbums);
		}

		public IActionResult Index()
        {
            ViewBag.Title = "Success!";

            return View();
        }
    }
}
