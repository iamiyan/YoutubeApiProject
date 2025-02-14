using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using YouTubeApiProject.Models;

namespace YouTubeApiProject.Services
{
    public class YouTubeApiService
    {
        private readonly string _apiKey;

        public YouTubeApiService(IConfiguration configuration)
        {
            _apiKey = configuration["YouTubeApiKey"]; // Fetch API key from appsettings.json
        }

        public async Task<(List<YouTubeVideoModel> Videos, string NextPageToken, string PrevPageToken)> SearchVideosWithPaginationAsync(string query, string duration, string uploadDate, string pageToken = null)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = "YoutubeProject"
            });

            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.Q = query;
            searchRequest.MaxResults = 12;
            searchRequest.Type = "video"; // Ensure fetching only videos
            searchRequest.PageToken = pageToken; // Include pagination token if provided

            // Apply Video Duration Filter
            if (!string.IsNullOrEmpty(duration))
            {
                searchRequest.VideoDuration = duration switch
                {
                    "short" => SearchResource.ListRequest.VideoDurationEnum.Short__,
                    "medium" => SearchResource.ListRequest.VideoDurationEnum.Medium,
                    "long" => SearchResource.ListRequest.VideoDurationEnum.Long__,
                    _ => null
                };
            }

            // Apply Upload Date Filter
            if (!string.IsNullOrEmpty(uploadDate))
            {
                DateTime publishedAfter = uploadDate switch
                {
                    "today" => DateTime.UtcNow.AddDays(-1),
                    "this_week" => DateTime.UtcNow.AddDays(-7),
                    "this_month" => DateTime.UtcNow.AddMonths(-1),
                    "this_year" => DateTime.UtcNow.AddYears(-1),
                    _ => DateTime.MinValue
                };

                if (publishedAfter != DateTime.MinValue)
                {
                    searchRequest.PublishedAfter = publishedAfter;
                }
            }

            // Execute search request
            var searchResponse = await searchRequest.ExecuteAsync();

            // Extract video IDs for duration & publish date lookup
            var videoIds = searchResponse.Items.Select(item => item.Id.VideoId).ToList();

            // Fetch video details (including duration & publish date)
            var videoRequest = youtubeService.Videos.List("contentDetails,snippet");
            videoRequest.Id = string.Join(",", videoIds);
            var videoResponse = await videoRequest.ExecuteAsync();

            // Match video details with search results
            var videos = searchResponse.Items.Select(item =>
            {
                var videoDetail = videoResponse.Items.FirstOrDefault(v => v.Id == item.Id.VideoId);
                return new YouTubeVideoModel
                {
                    Title = item.Snippet.Title,
                    Description = item.Snippet.Description,
                    ThumbnailUrl = item.Snippet.Thumbnails.Medium.Url,
                    VideoUrl = "https://www.youtube.com/watch?v=" + item.Id.VideoId,
                    RawDuration = videoDetail?.ContentDetails.Duration, // ISO 8601 duration
                    RawDateUpload = videoDetail?.Snippet.PublishedAt
                };
            }).ToList();

            // Return videos along with pagination tokens
            return (videos, searchResponse.NextPageToken, searchResponse.PrevPageToken);
        }


        //Method to fetch trending videos
        public async Task<(List<YouTubeVideoModel> Videos, string NextPageToken, string PrevPageToken)> GetTrendingVideosWithPaginationAsync(string pageToken = null)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = "YoutubeProject"
            });

            try
            {
                var searchRequest = youtubeService.Search.List("snippet");
                searchRequest.MaxResults = 12;
                searchRequest.RegionCode = "MY"; // Change this if needed
                searchRequest.Order = SearchResource.ListRequest.OrderEnum.ViewCount;
                searchRequest.Type = "video";
                searchRequest.PageToken = pageToken; // Include page token for pagination

                var searchResponse = await searchRequest.ExecuteAsync();

                if (searchResponse.Items == null || !searchResponse.Items.Any())
                {
                    Console.WriteLine("Warning: No trending videos found.");
                    return (new List<YouTubeVideoModel>(), null, null); // Return empty results with no tokens
                }

                // Extract Video IDs
                var videoIds = searchResponse.Items.Select(item => item.Id.VideoId).ToList();

                // Fetch video details
                var videoRequest = youtubeService.Videos.List("contentDetails");
                videoRequest.Id = string.Join(",", videoIds);

                var videoResponse = await videoRequest.ExecuteAsync();

                // Map search results to video details
                var videos = searchResponse.Items.Select(item =>
                {
                    var videoDetail = videoResponse.Items.FirstOrDefault(v => v.Id == item.Id.VideoId);

                    return new YouTubeVideoModel
                    {
                        Title = item.Snippet.Title ?? "No Title",
                        Description = item.Snippet.Description ?? "No Description",
                        ThumbnailUrl = item.Snippet.Thumbnails?.Medium?.Url ?? "/images/no-thumbnail.jpg",
                        VideoUrl = "https://www.youtube.com/watch?v=" + item.Id.VideoId,
                        RawDuration = videoDetail?.ContentDetails.Duration,
                        RawDateUpload = item.Snippet.PublishedAt
                    };
                }).ToList();

                // Return the videos along with pagination tokens
                return (videos, searchResponse.NextPageToken, searchResponse.PrevPageToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching trending videos: {ex.Message}");
                // Return empty results with no tokens in case of an exception
                return (new List<YouTubeVideoModel>(), null, null);
            }
        }

        public async Task<(List<YouTubeVideoModel> Videos, string NextPageToken, string PrevPageToken)> GetVideosByCategoryWithPaginationAsync(string category, string pageToken = null)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = "YoutubeProject"
            });

            try
            {
                // Fetch the category ID (map category name to ID dynamically if needed)
                string categoryId = GetCategoryIdByName(category); // Add a method or map for category IDs.

                var searchRequest = youtubeService.Search.List("snippet");
                searchRequest.MaxResults = 12;
                searchRequest.RegionCode = "MY"; // Change region code as needed
                searchRequest.Type = "video";
                searchRequest.VideoCategoryId = categoryId; // Filter by category ID
                searchRequest.PageToken = pageToken; // Include page token for pagination

                var searchResponse = await searchRequest.ExecuteAsync();

                if (searchResponse.Items == null || !searchResponse.Items.Any())
                {
                    Console.WriteLine("Warning: No videos found for the specified category.");
                    return (new List<YouTubeVideoModel>(), null, null); // Return empty results with no tokens
                }

                // Extract Video IDs
                var videoIds = searchResponse.Items.Select(item => item.Id.VideoId).ToList();

                // Fetch video details
                var videoRequest = youtubeService.Videos.List("contentDetails");
                videoRequest.Id = string.Join(",", videoIds);

                var videoResponse = await videoRequest.ExecuteAsync();

                // Map search results to video details
                var videos = searchResponse.Items.Select(item =>
                {
                    var videoDetail = videoResponse.Items.FirstOrDefault(v => v.Id == item.Id.VideoId);

                    return new YouTubeVideoModel
                    {
                        Title = item.Snippet.Title ?? "No Title",
                        Description = item.Snippet.Description ?? "No Description",
                        ThumbnailUrl = item.Snippet.Thumbnails?.Medium?.Url ?? "/images/no-thumbnail.jpg",
                        VideoUrl = "https://www.youtube.com/watch?v=" + item.Id.VideoId,
                        RawDuration = videoDetail?.ContentDetails.Duration,
                        RawDateUpload = item.Snippet.PublishedAt
                    };
                }).ToList();

                // Return the videos along with pagination tokens
                return (videos, searchResponse.NextPageToken, searchResponse.PrevPageToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching videos by category: {ex.Message}");
                // Return empty results with no tokens in case of an exception
                return (new List<YouTubeVideoModel>(), null, null);
            }
        }

        // Helper Method to Get Category ID by Name
        private string GetCategoryIdByName(string categoryName)
        {
            // Example of hardcoded category mapping (you can replace this with a database or API call if needed)
            var categoryMap = new Dictionary<string, string>
            {
                { "Music", "10" },
                { "Gaming", "20" },
                { "Education", "27" },
                { "Sports", "17" },
                { "News", "25" }
            };

            return categoryMap.ContainsKey(categoryName) ? categoryMap[categoryName] : null;
        }

    }
}
