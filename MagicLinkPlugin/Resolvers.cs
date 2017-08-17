using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MagicLinkPlugin
{
    static class Resolvers
    {
        public struct ResolveResult
        {
            public Uri Uri { get; set; }
            public bool StopResolving { get; set; }
        }

        public async static Task<ResolveResult?> ResolveReddit(Uri uri)
        {
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
                var response = await client.GetStringAsync(redditEndpoint);
                var res = JArray.Parse(response);
                var thingUrl = res[0]?["data"]?["children"]?[0]?["data"]?["url"]?.ToObject<string>();
                if (thingUrl == null)
                    return null;

                var thingUri = new Uri(thingUrl);

                if (thingUri.Host == "v.redd.it")
                {
                    var previewUrl = res[0]?["data"]?["children"]?[0]?["data"]?["preview"]?["images"]?[0]["source"]["url"].ToObject<string>();
                    return new ResolveResult
                    {
                        Uri = new Uri(previewUrl),
                        StopResolving = true
                    };
                }

                return new ResolveResult
                {
                    Uri = thingUri,
                    StopResolving = false
                };
            }
        }

        public async static Task<ResolveResult?> ResolveImgur(Uri uri)
        {
            if (uri.Host != "imgur.com" && !uri.Host.EndsWith(".imgur.com"))
                return null;

            if (uri.AbsolutePath.StartsWith("/a/") || uri.AbsolutePath.StartsWith("/gallery/"))
            {
                const string endpoint = "https://api.imgur.com/3/album/";

                var albumId = uri.AbsolutePath.Split('/')[2];

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Client-ID 493d66cf232857f");
                    var res = await client.GetStringAsync(endpoint + albumId);
                    var response = JObject.Parse(res);
                    if (!response["success"].ToObject<bool>())
                        return null;

                    var images = (JArray)response["data"]["images"];
                    if (images.Count == 0)
                        return null;

                    return new ResolveResult
                    {
                        Uri = new Uri(images[0]["link"].ToObject<string>()),
                        StopResolving = true
                    };
                }
            }

            var arr = uri.AbsolutePath.Split('/');
            if (arr.Length == 2 && !arr[1].Contains("."))
            {
                return new ResolveResult
                {
                    Uri = new Uri("http://i.imgur.com/" + arr[1] + ".jpg"),
                    StopResolving = false
                };
            }

            return null;
        }

        public static Task<ResolveResult?> Resolve9Gag(Uri uri)
        {
            if (uri.Host != "9gag.com" && !uri.Host.EndsWith(".9gag.com"))
                return null;

            var query = uri.AbsolutePath.Split('/');
            if (query.Length < 3)
                return null;

            if (query[1] != "gag")
                return null;

            return Task.FromResult<ResolveResult?>(new ResolveResult
            {
                Uri = new Uri($"https://img-9gag-fun.9cache.com/photo/{ query[2] }_700b.jpg"),
                StopResolving = true,
            });
        }

        public static Task<ResolveResult?> ResolveYouTube(Uri uri)
        {
            if (uri.Host != "youtube.com" && !uri.Host.EndsWith(".youtube.com"))
                return null;

            if (uri.AbsolutePath != "/watch")
                return null;

            var query = HttpUtility.ParseQueryString(uri.Query);

            if (string.IsNullOrEmpty(query["v"]))
                return null;

            return Task.FromResult<ResolveResult?>(new ResolveResult
            {
                Uri = new Uri($"https://img.youtube.com/vi/{ query["v"] }/0.jpg"),
                StopResolving = true,
            });
        }
    }
}
