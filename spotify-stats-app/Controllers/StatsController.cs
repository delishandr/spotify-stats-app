using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using spotify_stats_app.Models;
using System.Reflection;

namespace spotify_stats_app.Controllers
{
    public class StatsController : Controller
    {
        private readonly IWebHostEnvironment webHostEnv;
        private readonly string jsonFolder;
        private int pageSize;

        public StatsController(IWebHostEnvironment _webHostEnv, IConfiguration _config)
        {
            jsonFolder = _config["JsonFolder"];
            webHostEnv = _webHostEnv;
            pageSize = int.Parse(_config["PageSize"]);
        }

        private string UploadFile(IFormFile? file)
        {
            string fileName = string.Empty;

            if (file != null)
            {
                fileName = file.FileName;
                using (FileStream fileStream = new FileStream(
                    $"{webHostEnv.WebRootPath}\\{jsonFolder}\\{fileName}",
                    FileMode.CreateNew
                ))
                    file.CopyTo(fileStream);
            }

            return fileName;
        }

        private bool DeleteAllExtFiles()
        {
            DirectoryInfo di = new DirectoryInfo($"{webHostEnv.WebRootPath}\\{jsonFolder}");
            FileInfo[] files = di.GetFiles("*.json")
                .Where(p => p.Extension == ".json").ToArray();

            foreach (FileInfo file in files)
            {
                System.IO.File.Delete(file.FullName);
            }

            return true;
        }

        [HttpPost]
        [RequestSizeLimit(2147483647)]       //unit is bytes => 2GB
        [RequestFormLimits(MultipartBodyLengthLimit = 2147483647)]
        public Response<JsonFile> LoadStreamFiles(JsonFile data)
        {
            Response<JsonFile> response = new Response<JsonFile>();

            if (data.Files != null && data.Files.Count > 0)
            {
                if (DeleteAllExtFiles())
                {
                    foreach (IFormFile file in data.Files)
                    {
                        string fName = UploadFile(file);
                    }
                    response.statusCode = System.Net.HttpStatusCode.OK;
                }
            }
            else
            {
                response.statusCode = System.Net.HttpStatusCode.BadRequest;
                response.message = "Data cannot be empty!";
            }

            return response;
        }

        private List<StreamData> GetAllData(DateTime start, DateTime end)
        {

            List<StreamData> allData = new List<StreamData>();
            DirectoryInfo fullPath = new DirectoryInfo($"{webHostEnv.WebRootPath}\\{jsonFolder}");
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

        public IActionResult TopTracks(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", string type = "duration")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

            if (period == "lifetime")
            {
                datePeriod[0] = streams[0].ts;
            }

            List<TopStream> topTracks = new List<TopStream>();

            var listTracks = (
                from s in streams
                group s.ms_played by new { s.master_metadata_track_name, s.master_metadata_album_artist_name }
                            into t
                select new TopStream()
                {
                    trackName = t.Key.master_metadata_track_name,
                    artistName = t.Key.master_metadata_album_artist_name,
                    duration = t.Sum(),
                    count = t.Count()
                });

            switch (type)
            {
                case "count":
					topTracks = listTracks.OrderByDescending(t => t.count).ToList();
					break;
                default:
					topTracks = listTracks.OrderByDescending(t => t.duration).ToList();
					break;
            }
            topTracks = topTracks.GetRange(0, Math.Min(20, topTracks.Count));

            ViewBag.Title = "Your Top Tracks";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];
            ViewBag.Period = period;
            ViewBag.Type = type;

            return View(topTracks);
        }

		public IActionResult TopTracksAll(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", string type = "duration", int page = 1)
		{
			DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
			List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

			List<TopStream> topTracks = new List<TopStream>();

			var listTracks = (
				from s in streams
				group s.ms_played by new { s.master_metadata_track_name, s.master_metadata_album_artist_name }
							into t
				select new TopStream()
				{
					trackName = t.Key.master_metadata_track_name,
					artistName = t.Key.master_metadata_album_artist_name,
					duration = t.Sum(),
					count = t.Count()
				});

			switch (type)
			{
				case "count":
					topTracks = listTracks.OrderByDescending(t => t.count).ToList();
					break;
				default:
					topTracks = listTracks.OrderByDescending(t => t.duration).ToList();
					break;
			}

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

		public IActionResult TopArtists(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", string type = "duration")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

            if (period == "lifetime")
            {
                datePeriod[0] = streams[0].ts;
            }

			List<TopStream> topArtists = new List<TopStream>();

			var listArtists = (
				from s in streams
				group s.ms_played by new { s.master_metadata_album_artist_name }
					into t
				select new TopStream()
				{
					artistName = t.Key.master_metadata_album_artist_name,
					duration = t.Sum(),
					count = t.Count()
				});

			switch (type)
			{
				case "count":
					topArtists = listArtists.OrderByDescending(t => t.count).ToList();
					break;
				default:
					topArtists = listArtists.OrderByDescending(t => t.duration).ToList();
					break;
			}

			topArtists = topArtists.GetRange(0, Math.Min(20, topArtists.Count));

            ViewBag.Title = "Your Top Artists";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];
            ViewBag.Period = period;
			ViewBag.Type = type;

			return View(topArtists);
        }

        public IActionResult TopArtistsAll(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", string type = "duration", int page = 1)
        {
			DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
			List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

			List<TopStream> topArtists = new List<TopStream>();

			var listArtists = (
				from s in streams
				group s.ms_played by new { s.master_metadata_album_artist_name }
					into t
				select new TopStream()
				{
					artistName = t.Key.master_metadata_album_artist_name,
					duration = t.Sum(),
                    count = t.Count()
				});

			switch (type)
			{
				case "count":
					topArtists = listArtists.OrderByDescending(t => t.count).ToList();
					break;
				default:
					topArtists = listArtists.OrderByDescending(t => t.duration).ToList();
					break;
			}

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
			ViewBag.Type = type;

			return View(topArtists);
		}

        public IActionResult TopAlbums(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", string type = "duration")
        {
            DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
            List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

			List<TopStream> topAlbums = new List<TopStream>();

			var listAlbums = (
				from s in streams
				group s.ms_played by new { s.master_metadata_album_album_name, s.master_metadata_album_artist_name }
					into t
				select new TopStream()
				{
					albumName = t.Key.master_metadata_album_album_name,
					artistName = t.Key.master_metadata_album_artist_name,
					duration = t.Sum(),
					count = t.Count()
				});

			switch (type)
			{
				case "count":
					topAlbums = listAlbums.OrderByDescending(t => t.count).ToList();
					break;
				default:
					topAlbums = listAlbums.OrderByDescending(t => t.duration).ToList();
					break;
			}

            topAlbums = topAlbums.GetRange(0, Math.Min(20, topAlbums.Count));

            ViewBag.Title = "Your Top Albums";
            ViewBag.StartPeriod = datePeriod[0];
            ViewBag.EndPeriod = datePeriod[1];
			ViewBag.Period = period;
			ViewBag.Type = type;

			return View(topAlbums);
        }

		public IActionResult TopAlbumsAll(DateTime? startPeriod = null, DateTime? endPeriod = null, string period = "thisyear", string type = "duration", int page = 1)
		{
			DateTime[] datePeriod = GetStartEnd(startPeriod, endPeriod, period);
			List<StreamData> streams = GetAllData(datePeriod[0], datePeriod[1]);

			if (period == "lifetime")
			{
				datePeriod[0] = streams[0].ts;
			}

			List<TopStream> topAlbums = new List<TopStream>();

			var listAlbums = (
				from s in streams
				group s.ms_played by new { s.master_metadata_album_album_name, s.master_metadata_album_artist_name }
					into t
				select new TopStream()
				{
					albumName = t.Key.master_metadata_album_album_name,
					artistName = t.Key.master_metadata_album_artist_name,
					duration = t.Sum(),
					count = t.Count()
				});

			switch (type)
			{
				case "count":
					topAlbums = listAlbums.OrderByDescending(t => t.count).ToList();
					break;
				default:
					topAlbums = listAlbums.OrderByDescending(t => t.duration).ToList();
					break;
			}

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
			ViewBag.Type = type;

			return View(topAlbums);
		}

		public IActionResult Index()
        {
            ViewBag.Title = "Success!";

            return View();
        }
    }
}
