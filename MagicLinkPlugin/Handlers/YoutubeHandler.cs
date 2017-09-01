using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Compat.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MagicLinkPlugin.Handlers
{
    class YoutubeHandler : ILinkHandler
    {
        const string APIKEY = "[YOUTUBE_API_KEY]";

        struct VideoDetails
        {
            public string Title { get; set; }
            public string Channel { get; set; }
            public int ViewCount { get; set; }
            public TimeSpan Duration { get; set; }
            public DateTime Published { get; set; }
            public string ThumbnailUrl { get; set; }
            public string ChannelThumbnailUrl { get; set; }
            public string ChannelId { get; set; }
        }

        readonly string Template;
        public YoutubeHandler()
        {
            Assembly assembly = this.GetType().Assembly;
            using (var stream = assembly.GetManifestResourceStream("MagicLinkPlugin.Handlers.YoutubeMessage.html"))
            using (var sr = new StreamReader(stream))
            {
                Template = sr.ReadToEnd();
            }
        }

        public async Task<string[]> TryExtractContent(string url, HttpClientHandler handler)
        {
            Uri uri = new Uri(url);
            var id = GetId(uri);

            if (id == null)
                return null;

            var data = await GetVideoDetails(id);

            var channelThumbnail = await Helper.GetRoundImageUrl(data.ChannelThumbnailUrl, 48);
            var image = await Helper.GetBitmap(new Uri(data.ThumbnailUrl));

            var tsimg = GetTimestampImage(image, data.Duration);

            var res = Template
                .Replace("@ChannelId", data.ChannelId)
                .Replace("@VideoTitle", data.Title)
                .Replace("@ChannelName", data.Channel)
                .Replace("@NumViews", data.ViewCount.ToString())
                .Replace("@TimeAgo", data.Published.TimeAgo())
                .Replace("@OriginalUrl", url)
                .Replace("@ChannelImage", channelThumbnail)
                .Replace("@FullImage", tsimg.EncodeAsString())
                ;

            return new string[] { res };
        }

        Image GetTimestampImage(Image original, TimeSpan duration)
        {
            var width = 400;
            double factor = original.Width / 400.0;
            var height = (int)(original.Height / factor);

            

            var res = new Bitmap(width, height);
            using (var g = Graphics.FromImage(res))
            using (var f = new Font(FontFamily.GenericSansSerif, 12))
            using (var bgBrush = new SolidBrush(Color.FromArgb(180, Color.Black)))
            using (var fgBrush = new SolidBrush(Color.White))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.DrawImage(original, new Rectangle(0, 0, width, height));


                var durationText = "";
                if ((int)duration.TotalHours > 0)
                    durationText += (int)duration.TotalHours + ":";

                durationText += duration.Minutes.ToString().PadLeft(2, '0') + ":";
                durationText += duration.Seconds.ToString().PadLeft(2, '0');

                var size = g.MeasureString(durationText, f);

                var right = width - 5;
                var bottom = height - 5;
                var left = right - size.Width - 3;
                var top = bottom - size.Height - 3;

                var rect = Helper.MakeRoundedRect(new RectangleF(left, top, right - left, bottom - top), 5, 5);
                g.Clip = new Region(rect);
                g.FillRectangle(bgBrush, new Rectangle(0, 0, width, height));

                g.DrawString(durationText, f, fgBrush, new Point((int)left + 2, (int)top + 2));

            }

            return res;
        }

        async Task<VideoDetails> GetVideoDetails(string id)
        {
            string url = "https://www.googleapis.com/youtube/v3/videos/?key=" + APIKEY + "&part=snippet%2CcontentDetails%2Cstatistics&id=" + id;

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "curl/7.54.1");
                var res = await client.GetStringAsync(url);

                var data = JObject.Parse(res);

                return new VideoDetails
                {
                    Title = data["items"][0]["snippet"]["title"].ToObject<string>(),
                    Channel = data["items"][0]["snippet"]["channelTitle"].ToObject<string>(),
                    Published = data["items"][0]["snippet"]["publishedAt"].ToObject<DateTime>(),
                    ViewCount = int.Parse(data["items"][0]["statistics"]["viewCount"].ToObject<string>()),
                    Duration = XmlConvert.ToTimeSpan(data["items"][0]["contentDetails"]["duration"].ToObject<string>()),
                    ChannelId = data["items"][0]["snippet"]["channelId"].ToObject<string>(),
                    ChannelThumbnailUrl = await GetChannelThumbnail(data["items"][0]["snippet"]["channelId"].ToObject<string>()),
                    ThumbnailUrl = data["items"][0]["snippet"]["thumbnails"]["maxres"]["url"].ToObject<string>(),
                };
            }
        }

        async Task<string> GetChannelThumbnail(string channelId)
        {
            string url = "https://www.googleapis.com/youtube/v3/channels/?key=" + APIKEY + "&part=snippet&id=" + channelId;
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var res = await client.GetStringAsync(url);
                var data = JObject.Parse(res);

                return data["items"][0]["snippet"]["thumbnails"]["default"]["url"].ToObject<string>();
            }

        }
        string GetId(Uri uri)
        {
            if ((uri.Host == "youtube.com" || uri.Host.EndsWith(".youtube.com")) && uri.AbsolutePath == "/watch")
                return HttpUtility.ParseQueryString(uri.Query)["v"];

            if (uri.Host == "youtu.be")
                return uri.AbsolutePath.Substring(1);

            return null;
        }

    }
}
