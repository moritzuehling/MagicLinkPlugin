using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Compat.Web;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MagicLinkPlugin.Handlers
{
    class RedditHandler : ILinkHandler
    {

        readonly string Template;
        public RedditHandler()
        {
            Assembly assembly = this.GetType().Assembly;
            using (var stream = assembly.GetManifestResourceStream("MagicLinkPlugin.Handlers.RedditMessage.html"))
            using (var sr = new StreamReader(stream))
            {
                Template = sr.ReadToEnd();
            }
        }

        public async Task<string[]> TryExtractContent(string url, HttpClientHandler handler)
        {
            Uri uri = new Uri(url);

            if (uri.Host != "reddit.com" && !uri.Host.EndsWith(".reddit.com"))
                return null;

                        var path = uri.AbsolutePath.Split('/');

            if (path.Length < 5)
                return null;

            if (path[1] != "r")
                return null;

            if (path[3] != "comments")
                return null;

            var sub = path[2];
            var thing = path[4];

            var redditEndpoint = "https://www.reddit.com/r/" + sub + "/comments/" + thing + "/.json";

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetStringAsync(redditEndpoint);
                var res = JArray.Parse(response);
                var thingUrl = HttpUtility.HtmlDecode(res[0]?["data"]?["children"]?[0]?["data"]?["url"]?.ToObject<string>());
                var domain = res[0]?["data"]?["children"]?[0]?["data"]?["domain"]?.ToObject<string>();
                var selftextHtml = res[0]?["data"]?["children"]?[0]?["data"]?["selftext_html"]?.ToObject<string>();
                var title = res[0]?["data"]?["children"]?[0]?["data"]?["title"]?.ToObject<string>();
                var author = res[0]?["data"]?["children"]?[0]?["data"]?["author"]?.ToObject<string>();
                var subreddit = res[0]?["data"]?["children"]?[0]?["data"]?["subreddit"]?.ToObject<string>();
                var score = res[0]?["data"]?["children"]?[0]?["data"]?["ups"]?.ToObject<int>();
                var createdUtc = res[0]?["data"]?["children"]?[0]?["data"]?["created_utc"]?.ToObject <float>();
                var numComments = res[0]?["data"]?["children"]?[0]?["data"]?["num_comments"]?.ToObject<int>();
                var isSelf = res[0]?["data"]?["children"]?[0]?["data"]?["is_self"]?.ToObject<bool>();
                
                var scoreString = score.ToString();
                var scoreAlignment = "".PadLeft(Math.Max(3 - (scoreString.Length) / 2, 0), '.');
                var scoreAlignment2 = scoreString.Length % 2 == 0 ? "." : "";
                var time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)createdUtc);
                var timeAgo = time.TimeAgo();

                string result = Template
                    .Replace("@ScoreAlign2", scoreAlignment2)
                    .Replace("@ScoreAlign", scoreAlignment)
                    .Replace("@Score", scoreString)
                    .Replace("@NumComments", numComments.ToString())
                    .Replace("@Domain", domain)
                    .Replace("@Title", title)
                    .Replace("@Author", author)
                    .Replace("@Subreddit", subreddit)
                    .Replace("@TimeAgo", timeAgo)
                    .Replace("@CommentsUrl", url)
                    ;

                string content = "";

                if (!string.IsNullOrEmpty(selftextHtml))
                {
                    content = HttpUtility.HtmlDecode(selftextHtml);
                }
                else if (isSelf == false)
                {
                    var results = await LinkHandler.GetContent(thingUrl);
                    if (results != null)
                        content = string.Join("<br><hr>" + Environment.NewLine, results);
                }

                result = result.Replace("@Content", content);

                return new string[] { result };
            }

            return null;
        }

    }
}
