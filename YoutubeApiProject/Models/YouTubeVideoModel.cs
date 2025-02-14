using System;
using System.Text.RegularExpressions;

namespace YouTubeApiProject.Models
{
    public class YouTubeVideoModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string VideoUrl { get; set; }
        public string RawDuration { get; set; } // Store raw ISO 8601 duration
        public DateTime? RawDateUpload { get; set; } // Store raw upload date

        // Format YouTube's ISO 8601 duration (e.g., PT23M43S -> 23:43)
        public string FormattedDuration => FormatYouTubeDuration(RawDuration);

        // Format upload date (e.g., "2024-02-07T10:00:00Z" → "Feb 7, 2024")
        public string FormattedDateUpload => RawDateUpload?.ToString("MMM d, yyyy") ?? "Unknown";

        private static string FormatYouTubeDuration(string isoDuration)
        {
            if (string.IsNullOrEmpty(isoDuration))
                return "Unknown";

            var match = Regex.Match(isoDuration, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");

            int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

            return hours > 0 ? $"{hours:D2}:{minutes:D2}:{seconds:D2}" : $"{minutes:D2}:{seconds:D2}";
        }

        public string Category { get; set; }
    }
}
