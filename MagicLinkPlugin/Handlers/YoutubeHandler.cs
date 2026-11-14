using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace MagicLinkPlugin.Handlers
{
    // Needs MAGICLINK_YOUTUBE_API_KEY. Without it, or if the API fails, the image handler shows the video's thumbnail.
    class YoutubeHandler : ILinkHandler
    {
        const int WIDTH = 400;
        const string USER_AGENT = "curl/7.54.1";

        struct VideoDetails
        {
            public string Title { get; set; }
            public string Channel { get; set; }
            public long ViewCount { get; set; }
            public TimeSpan Duration { get; set; }
            public DateTime Published { get; set; }
            public string ThumbnailUrl { get; set; }
            public string ChannelThumbnailUrl { get; set; }
            public string ChannelId { get; set; }
        }

        readonly string Template;
        public YoutubeHandler()
        {
            var assembly = GetType().Assembly;
            using (var stream = assembly.GetManifestResourceStream("MagicLinkPlugin.Handlers.YoutubeMessage.html"))
            using (var sr = new StreamReader(stream))
            {
                Template = sr.ReadToEnd();
            }
        }

        public async Task<string[]> TryExtractContent(string url)
        {
            var apiKey = Environment.GetEnvironmentVariable("MAGICLINK_YOUTUBE_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                return null;

            var id = GetId(new Uri(url));
            if (id == null)
                return null;

            var data = await GetVideoDetails(apiKey, id);

            var channelThumbnail = await Helper.GetRoundImageUrl(data.ChannelThumbnailUrl, 48);
            using var image = Helper.LoadImage(await Helper.GetBytes(data.ThumbnailUrl));
            image.Mutate(x => x.Resize(WIDTH, 0)); // 0 keeps the aspect ratio

            var res = Template
                .Replace("@ChannelId", WebUtility.HtmlEncode(data.ChannelId))
                .Replace("@VideoTitle", WebUtility.HtmlEncode(data.Title))
                .Replace("@ChannelName", WebUtility.HtmlEncode(data.Channel))
                .Replace("@Duration", FormatDuration(data.Duration))
                .Replace("@NumViews", data.ViewCount.ToString("N0"))
                .Replace("@TimeAgo", data.Published.TimeAgo())
                .Replace("@OriginalUrl", url)
                .Replace("@ChannelImage", channelThumbnail)
                .Replace("@FullImage", image.EncodeAsString())
                ;

            return new string[] { res };
        }

        static string FormatDuration(TimeSpan duration)
        {
            var text = "";
            if ((int)duration.TotalHours > 0)
                text += (int)duration.TotalHours + ":";
            return text + duration.Minutes.ToString().PadLeft(2, '0') + ":" + duration.Seconds.ToString().PadLeft(2, '0');
        }

        static async Task<VideoDetails> GetVideoDetails(string apiKey, string id)
        {
            var url = "https://www.googleapis.com/youtube/v3/videos/?key=" + Uri.EscapeDataString(apiKey)
                + "&part=snippet%2CcontentDetails%2Cstatistics&id=" + Uri.EscapeDataString(id);
            var data = JObject.Parse(await Helper.GetString(url, USER_AGENT));
            var item = data["items"][0];
            var thumbnails = item["snippet"]["thumbnails"];
            var channelId = item["snippet"]["channelId"].ToObject<string>();

            return new VideoDetails
            {
                Title = item["snippet"]["title"].ToObject<string>(),
                Channel = item["snippet"]["channelTitle"].ToObject<string>(),
                Published = item["snippet"]["publishedAt"].ToObject<DateTime>(),
                ViewCount = long.Parse(item["statistics"]["viewCount"].ToObject<string>()),
                Duration = XmlConvert.ToTimeSpan(item["contentDetails"]["duration"].ToObject<string>()),
                ChannelId = channelId,
                ChannelThumbnailUrl = await GetChannelThumbnail(apiKey, channelId),
                // Not every video has every size.
                ThumbnailUrl = (thumbnails["maxres"] ?? thumbnails["standard"] ?? thumbnails["high"] ?? thumbnails["default"])["url"].ToObject<string>(),
            };
        }

        static async Task<string> GetChannelThumbnail(string apiKey, string channelId)
        {
            var url = "https://www.googleapis.com/youtube/v3/channels/?key=" + Uri.EscapeDataString(apiKey)
                + "&part=snippet&id=" + Uri.EscapeDataString(channelId);
            var data = JObject.Parse(await Helper.GetString(url, USER_AGENT));
            return data["items"][0]["snippet"]["thumbnails"]["default"]["url"].ToObject<string>();
        }

        static string GetId(Uri uri)
        {
            if ((uri.Host == "youtube.com" || uri.Host.EndsWith(".youtube.com")) && uri.AbsolutePath == "/watch")
                return HttpUtility.ParseQueryString(uri.Query)["v"];

            if (uri.Host == "youtu.be" && uri.AbsolutePath.Length > 1)
                return uri.AbsolutePath.Substring(1);

            return null;
        }
    }
}
