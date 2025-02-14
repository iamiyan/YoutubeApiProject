using Microsoft.AspNetCore.Mvc;
using YouTubeApiProject.Services;
using YouTubeApiProject.Models;


namespace YouTubeApiProject.Controllers
{
    public class YouTubeController : Controller
    {
        private readonly YouTubeApiService _youtubeService;

        public YouTubeController(YouTubeApiService youtubeService)
        {
            _youtubeService = youtubeService;
        }

        // Display Search Page
        public IActionResult Index()
        {
            return View(new List<YouTubeVideoModel>()); // Pass an empty list initially
        }

        // Handle the search query with filters and pagination
        [HttpPost]
        public async Task<IActionResult> Search(string query, string duration, string uploadDate, string pageToken = null)
        {
            // Trim query to remove unwanted spaces
            query = query?.Trim();

            // Check if query is empty to prevent invalid API requests
            if (string.IsNullOrEmpty(query))
            {
                ViewBag.Message = "Please enter a search query.";
                return View("Index", new List<YouTubeVideoModel>());
            }

            try
            {
                // Fetch search results with pagination from the YouTube API
                var (videos, nextPageToken, prevPageToken) = await _youtubeService.SearchVideosWithPaginationAsync(query, duration, uploadDate, pageToken);

                if (videos == null || videos.Count == 0)
                {
                    ViewBag.Message = "No videos found. Try a different search!";
                }

                // Pass pagination tokens and filters to the view for navigation
                ViewBag.NextPageToken = nextPageToken;
                ViewBag.PrevPageToken = prevPageToken;
                ViewBag.Query = query; // Keep the query
                ViewBag.Duration = duration; // Keep the duration filter
                ViewBag.UploadDate = uploadDate; // Keep the upload date filter

                return View("Index", videos);
            }
            catch (Exception ex)
            {
                // Log the error (for debugging)
                Console.WriteLine($"Search error: {ex.Message}");

                // Show an error message to the user
                ViewBag.Message = "An error occurred while fetching videos. Please try again later.";
                return View("Index", new List<YouTubeVideoModel>());
            }
        }
    }
}
