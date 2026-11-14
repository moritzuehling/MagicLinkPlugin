using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MagicLinkPlugin.Handlers
{
    // Reddit's API refuses clients without an approved app, but its oEmbed endpoint, meant for embedding posts in other sites,
    // answers without one. It gives the title and author; score, comments, age and the post's text aren't part of it.
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

            var subreddit = path[2];
            var postUrl = "https://www.reddit.com/r/" + Uri.EscapeDataString(subreddit) + "/comments/" + Uri.EscapeDataString(path[4]) + "/";

            var response = await Helper.GetString("https://www.reddit.com/oembed?url=" + Uri.EscapeDataString(postUrl), USER_AGENT);
            var data = JObject.Parse(response);
            var title = data["title"]?.ToObject<string>();
            var author = data["author_name"]?.ToObject<string>();
            if (string.IsNullOrEmpty(title))
                return null;

            string result = Template
                .Replace("@Title", WebUtility.HtmlEncode(title))
                .Replace("@Author", WebUtility.HtmlEncode(author ?? "[deleted]"))
                .Replace("@Subreddit", WebUtility.HtmlEncode(subreddit))
                .Replace("@CommentsUrl", url) // from Mumble's HTML, so already escaped
                ;

            return new string[] { result };
        }
    }
}
