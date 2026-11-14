using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MagicLinkPlugin.Handlers
{
    class RedditHandler : ILinkHandler
    {
        // Reddit refuses requests without a descriptive User-Agent.
        const string USER_AGENT = "MagicLinkPlugin/2.0 (link previews for a Mumble server)";

        readonly string Template;
        public RedditHandler()
        {
            var assembly = GetType().Assembly;
            using (var stream = assembly.GetManifestResourceStream("MagicLinkPlugin.Handlers.RedditMessage.html"))
            using (var sr = new StreamReader(stream))
            {
                Template = sr.ReadToEnd();
            }
        }

        public async Task<string[]> TryExtractContent(string url)
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

            var redditEndpoint = "https://www.reddit.com/r/" + Uri.EscapeDataString(sub) + "/comments/" + Uri.EscapeDataString(thing) + "/.json";

            var response = await Helper.GetString(redditEndpoint, USER_AGENT);
            var res = JArray.Parse(response);
            var data = res[0]?["data"]?["children"]?[0]?["data"];
            var thingUrl = WebUtility.HtmlDecode(data?["url"]?.ToObject<string>());
            var domain = data?["domain"]?.ToObject<string>();
            var selftextHtml = data?["selftext_html"]?.ToObject<string>();
            var title = data?["title"]?.ToObject<string>();
            var author = data?["author"]?.ToObject<string>();
            var subreddit = data?["subreddit"]?.ToObject<string>();
            var score = data?["ups"]?.ToObject<int>();
            var createdUtc = data?["created_utc"]?.ToObject<double>() ?? 0;
            var numComments = data?["num_comments"]?.ToObject<int>();
            var isSelf = data?["is_self"]?.ToObject<bool>();

            var scoreString = score.ToString();
            var scoreAlignment = "".PadLeft(Math.Max(3 - (scoreString.Length) / 2, 0), '.');
            var scoreAlignment2 = scoreString.Length % 2 == 0 ? "." : "";
            var time = DateTime.UnixEpoch.AddSeconds(createdUtc);
            var timeAgo = time.TimeAgo();

            string result = Template
                .Replace("@ScoreAlign2", scoreAlignment2)
                .Replace("@ScoreAlign", scoreAlignment)
                .Replace("@Score", scoreString)
                .Replace("@NumComments", numComments.ToString())
                .Replace("@Domain", WebUtility.HtmlEncode(domain))
                .Replace("@Title", WebUtility.HtmlEncode(title))
                .Replace("@Author", WebUtility.HtmlEncode(author))
                .Replace("@Subreddit", WebUtility.HtmlEncode(subreddit))
                .Replace("@TimeAgo", timeAgo)
                .Replace("@CommentsUrl", url)
                .Replace("@TargetUrl", thingUrl ?? url)
                ;

            string content = "";

            if (!string.IsNullOrEmpty(selftextHtml))
            {
                content = WebUtility.HtmlDecode(selftextHtml);
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
    }
}
