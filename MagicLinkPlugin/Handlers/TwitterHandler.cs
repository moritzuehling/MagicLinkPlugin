using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TweetSharp;

namespace MagicLinkPlugin
{
    class TwitterHandler : ILinkHandler
    {
        const string CONSUMER_KEY = "DBKIqaIXjT2bMnQ11mXeJhtJ8";
        const string CONSUMER_SECRET = "nZgj2rCFtyRaWRgzckkDzMusLS2rbWMkgESlOS85uPhHmnSblR";
        const string ACCESS_TOKEN = "2391432427-gT1By2JvNVpFWug8cLQR5Xdwc7PiKgGGuF3L1Qe";
        const string ACCESS_TOKEN_SECRET = "TDSstUlQSLhpPwlJwGKwBIQVmzApcB8632ttnXGvaaPuI";

        const int DefaultMaxWidth = 400;
        const int DefaultMaxHeight = 400;
        const int SecondaryMaxWidth = DefaultMaxWidth / 2;
        const int SecondaryMaxHeight = DefaultMaxWidth / 2;

        readonly string Template;

        public TwitterHandler()
        {
            Assembly assembly = this.GetType().Assembly;
            using (var stream = assembly.GetManifestResourceStream("MagicLinkPlugin.Handlers.TwitterMessage.html"))
            using (var sr = new StreamReader(stream))
            {
                Template = sr.ReadToEnd();
            }
        }

        public async Task<string> TryExtractContent(string url, HttpClientHandler handler)
        {
            var uri = new Uri(url);
            if (uri.Host != "twitter.com")
                return null;

            var urlSegements = uri.AbsolutePath.Split('/');

            if (urlSegements.Length < 4)
                return null;

            if (urlSegements[2] != "status")
                return null;

            if (!long.TryParse(urlSegements[3], out long tweetId))
                return null;

            return await Extract(tweetId);
        }

        public async Task<string> Extract(long tweetId)
        {

            // In v1.1, all API calls require authentication
            var service = new TwitterService(CONSUMER_KEY, CONSUMER_SECRET);
            service.AuthenticateWith(ACCESS_TOKEN, ACCESS_TOKEN_SECRET);

            var tweet = service.GetTweet(new GetTweetOptions
            {
                Id = tweetId,
                IncludeEntities = true,
            });


            string resultingAuthorImage;
            using(var client = new HttpClient())
            {
                // Download profile image
                using (var stream = await client.GetStreamAsync(tweet.User.ProfileImageUrl))
                using (var source = Image.FromStream(stream))
                using (var target = new Bitmap(48, 48))
                using (var graphics = Graphics.FromImage(target))
                using (var textureBrush = new TextureBrush(source))
                using(var ms = new MemoryStream())
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.Clear(Color.White);
                    graphics.FillEllipse(textureBrush, new Rectangle(0, 0, 48, 48));

                    target.Save(ms, ImageFormat.Png);

                    resultingAuthorImage = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                }
            }

            var imageContent = await GetImageContent(tweet);

            string time;

            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            time = tweet.CreatedDate.ToShortTimeString() + " - " + tweet.CreatedDate.ToLongDateString();
            Thread.CurrentThread.CurrentCulture = oldCulture;

            var resultingHtml = Template
                .Replace("@AuthorImage", resultingAuthorImage)
                .Replace("@AuthorName", tweet.User.Name)
                .Replace("@AuthorHandle", tweet.User.ScreenName)
                .Replace("@time", time)
                .Replace("@TweetContent", GetHtmlContent(tweet))
                .Replace("@Images", imageContent);

            return resultingHtml;
        }

        async Task<string> GetImageContent(TwitterStatus tweet)
        {
            if (tweet.ExtendedEntities == null)
                return "";

            StringBuilder res = new StringBuilder();
            using (var client = new HttpClient())
            {
                var entities = tweet.ExtendedEntities.Media.ToArray();
                int maxWidth = entities.Length > 2 ? SecondaryMaxWidth : DefaultMaxWidth;
                int maxHeight = entities.Length > 2 ? SecondaryMaxHeight : DefaultMaxHeight;
                int i = 0;

                foreach (var currentEntity in entities)
                {
                    var image = await ImageHander.DownloadAndDownsizeImage(currentEntity.MediaUrl, client, maxWidth, maxHeight);
                    
                    res.Append("<img src=\"");
                    res.Append("data:" + image.Item2 + ";base64," + Convert.ToBase64String(image.Item1));
                    res.Append("\" alt=\"no\">");
                    if (i % 2 == 0)
                        res.Append("<br>");
                }
            }

            return res.ToString();

        }

        string GetHtmlContent(TwitterStatus status)
        {
            StringBuilder res = new StringBuilder();
            var entities = status.Entities.OrderBy(a => a.StartIndex).ToArray();

            res.Append(status.Text.Substring(0, entities[0].StartIndex));

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var entityText = status.Text.Substring(entity.StartIndex, entity.EndIndex - entity.StartIndex);
                var nextEnd = i + 1 < entities.Length ? entities[i + 1].StartIndex : status.Text.Length;
                var bridgeText = status.Text.Substring(entity.EndIndex, nextEnd - entity.EndIndex);
                
                switch (entity.EntityType)
                {
                    case TwitterEntityType.HashTag:
                    case TwitterEntityType.Mention:
                        res.AppendFormat("<span style=\"color: #{0}\">{1}</span>", status.User.ProfileLinkColor, entityText);
                        break;
                    case TwitterEntityType.Url:
                        res.AppendFormat("<a href=\"{1}\" style=\"color:#{0}\">{1}</a>", status.User.ProfileLinkColor, entityText);
                        break;
                    case TwitterEntityType.Media:
                        var extendedMedia = status.ExtendedEntities.Media.Single(a => a.StartIndex == entity.StartIndex);
                        if (extendedMedia.ExtendedEntityType != TwitterMediaType.Photo)
                        {
                            res.AppendFormat("<a href=\"{2}\" style=\"color:#{0}\">{1}</a>", status.User.ProfileLinkColor, entityText, extendedMedia.ExpandedUrl);
                        }
                        break;
                    default:
                        // We should probably format it normally *shrug*
                        res.AppendFormat("<span style=\"color:#{0}\">{1}</span>", status.User.ProfileLinkColor, entityText);
                        break;
                }

                res.Append(bridgeText);
            }

            return res.ToString();
        }
    }
}
