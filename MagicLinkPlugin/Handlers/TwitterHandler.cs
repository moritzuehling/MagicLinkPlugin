using MagicLinkPlugin.Handlers;
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
        const string CONSUMER_KEY = "[TWITTER_CONSUMER_KEY]";
        const string CONSUMER_SECRET = "[TWITTER_CONSUMER_SECRET]";

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

        public async Task<string[]> TryExtractContent(string url, HttpClientHandler handler)
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

            return new string[] { await Extract(tweetId) };
        }

        public async Task<string> Extract(long tweetId)
        {

            // In v1.1, all API calls require authentication
            var service = new TwitterService(CONSUMER_KEY, CONSUMER_SECRET);

            var tweet = service.GetTweet(new GetTweetOptions
            {
                Id = tweetId,
                IncludeEntities = true,
                TweetMode = TweetMode.Extended
            });

            var authorTask = GetAuthorImage(tweet);

            var imageContent = GetImageContent(tweet);

            string time;

            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            time = tweet.CreatedDate.ToShortTimeString() + " - " + tweet.CreatedDate.ToLongDateString();
            Thread.CurrentThread.CurrentCulture = oldCulture;

            var resultingHtml = Template
                .Replace("@AuthorName", tweet.User.Name)
                .Replace("@AuthorHandle", tweet.User.ScreenName)
                .Replace("@time", time)
                .Replace("@TweetContent", GetHtmlContent(tweet))
                .Replace("@AuthorImage", await authorTask)
                .Replace("@Images", await imageContent);

            return resultingHtml;
        }

        async Task<string> GetImageContent(TwitterStatus tweet)
        {
            var bitmaps = await DownloadBitmaps(tweet);
            if (bitmaps == null)
                return "";

            if (bitmaps.Count == 1)
            {
                using (var result = ImageLayouter.Layout1(bitmaps[0]))
                    return result.EncodeAsString();
            }
            else if (bitmaps.Count == 2)
            {
                using (var result = ImageLayouter.Layout2(bitmaps.ToArray()))
                    return result.EncodeAsString();
            }
            else if (bitmaps.Count == 3)
            {
                using (var result = ImageLayouter.Layout3(bitmaps.ToArray()))
                    return result.EncodeAsString();
            }
            else if (bitmaps.Count == 4)
            {
                using (var result = ImageLayouter.Layout4(bitmaps.ToArray()))
                    return result.EncodeAsString();
            }

            return "";
        }

        async Task<List<Image>> DownloadBitmaps(TwitterStatus tweet)
        {
            if (tweet.ExtendedEntities == null)
                return null;

            var entities = tweet.ExtendedEntities.Where(a => a.ExtendedEntityType == TwitterMediaType.Photo).ToArray();

            if (entities.Length == 0)
                return null;

            var bitmapTasks = entities.Select(a => Helper.GetBitmap(a.MediaUrl)).ToArray();

            List<Image> res = new List<Image>();
            foreach (var task in bitmapTasks)
                res.Add(await task);

            return res;
        }

        async Task<string> GetAuthorImage(TwitterStatus tweet)
        {
            return await Helper.GetRoundImageUrl(tweet.User.ProfileImageUrl, 48);
        }

        string GetHtmlContent(TwitterStatus status)
        {
            if (status.Entities.Count() == 0)
                return status.FullText.Replace("\n", "<br>");

            StringBuilder res = new StringBuilder();
            var entities = status.Entities.OrderBy(a => a.StartIndex).ToArray();

            res.Append(status.FullText.Substring(0, entities[0].StartIndex).Replace("\n", "<br>"));

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var entityText = status.FullText.Substring(entity.StartIndex, entity.EndIndex - entity.StartIndex);
                var nextEnd = i + 1 < entities.Length ? entities[i + 1].StartIndex : status.FullText.Length;
                var bridgeText = status.FullText.Substring(entity.EndIndex, nextEnd - entity.EndIndex);


                switch (entity.EntityType)
                {
                    case TwitterEntityType.HashTag:
                    case TwitterEntityType.Mention:
                        res.AppendFormat("<span style=\"color: #{0}\">{1}</span>", status.User.ProfileLinkColor, entityText);
                        break;
                    case TwitterEntityType.Url:
                        res.AppendFormat("<a href=\"{2}\" style=\"color:#{0}\">{1}</a>", status.User.ProfileLinkColor, ((TwitterUrl)entity).DisplayUrl, ((TwitterUrl)entity).ExpandedValue);
                        break;
                    case TwitterEntityType.Media:
                        var extendedEntity = (TwitterMedia)entity;
                        if (extendedEntity.MediaType != TwitterMediaType.Photo)
                        {
                            res.AppendFormat("<a href=\"{2}\" style=\"color:#{0}\">{1}</a>", status.User.ProfileLinkColor, extendedEntity.DisplayUrl, extendedEntity.ExpandedUrl);
                        }
                        break;
                    default:
                        // We should probably format it normally *shrug*
                        res.AppendFormat("<span style=\"color:#{0}\">{1}</span>", status.User.ProfileLinkColor, entityText);
                        break;
                }

                res.Append(bridgeText.Replace("\n", "<br>"));
            }

            return res.ToString();
        }
    }
}
