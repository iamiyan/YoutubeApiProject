using Microsoft.AspNetCore.Mvc;
using YouTubeApiProject.Services;
using YouTubeApiProject.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging; // Logger

namespace YouTubeApiProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly YouTubeApiService _youtubeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(YouTubeApiService youtubeService, ILogger<HomeController> logger)
        {
            _youtubeService = youtubeService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string category = null, string pageToken = null)
        {
            var videosWithPagination = string.IsNullOrEmpty(category)
                ? await _youtubeService.GetTrendingVideosWithPaginationAsync(pageToken)
                : await _youtubeService.GetVideosByCategoryWithPaginationAsync(category, pageToken);

            ViewBag.NextPageToken = videosWithPagination.NextPageToken;
            ViewBag.PrevPageToken = videosWithPagination.PrevPageToken;
            ViewBag.SelectedCategory = category;

            // Static list of categories for the view
            ViewBag.Categories = new List<string> { "Music", "Gaming", "Education", "Sports", "News" };

            return View(videosWithPagination.Videos);
        }

    }


}
