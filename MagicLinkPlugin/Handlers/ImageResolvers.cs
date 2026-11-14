using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Web;

namespace MagicLinkPlugin
{
    public static class ImageResolvers
    {
        public struct ResolveResult
        {
            public Uri Uri { get; set; }
            public bool StopResolving { get; set; }
        }

        static readonly Task<ResolveResult?> NoResult = Task.FromResult<ResolveResult?>(null);

        static readonly Func<Uri, Task<ResolveResult?>>[] ResolveFunctions = new Func<Uri, Task<ResolveResult?>>[]
        {
            Resolve9Gag,
            ResolveYouTube,
            ResolveImgur,
            ResolveFacebookFix,
        };

        public async static Task<ResolveResult> Resolve(Uri uri)
        {
            foreach (var resolver in ResolveFunctions)
            {
                try
                {
                    var newUri = await resolver(uri);
                    if (newUri.HasValue)
                    {
                        uri = newUri.Value.Uri;
                        if (newUri.Value.StopResolving)
                            return newUri.Value;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MagicLink] resolving {uri} failed: {ex.Message}");
                }
            }

            return new ResolveResult
            {
                Uri = uri,
                StopResolving = false,
            };
        }

        public async static Task<ResolveResult?> ResolveImgur(Uri uri)
        {
            if (uri.Host != "imgur.com" && !uri.Host.EndsWith(".imgur.com"))
                return null;

            if (uri.AbsolutePath.StartsWith("/a/") || uri.AbsolutePath.StartsWith("/gallery/"))
            {
                // Albums need imgur's API, and with it a client ID.
                var clientId = Environment.GetEnvironmentVariable("MAGICLINK_IMGUR_CLIENT_ID");
                if (string.IsNullOrEmpty(clientId))
                    return null;

                var albumId = uri.AbsolutePath.Split('/')[2];
                var res = await Helper.GetString("https://api.imgur.com/3/album/" + albumId, authorization: "Client-ID " + clientId);
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

            var arr = uri.AbsolutePath.Split('/');
            if (arr.Length == 2 && !arr[1].Contains("."))
            {
                return new ResolveResult
                {
                    Uri = new Uri("https://i.imgur.com/" + arr[1] + ".jpg"),
                    StopResolving = false
                };
            }

            return null;
        }

        public static Task<ResolveResult?> Resolve9Gag(Uri uri)
        {
            if (uri.Host != "9gag.com" && !uri.Host.EndsWith(".9gag.com"))
                return NoResult;

            var query = uri.AbsolutePath.Split('/');
            if (query.Length < 3)
                return NoResult;

            if (query[1] != "gag")
                return NoResult;

            return Task.FromResult<ResolveResult?>(new ResolveResult
            {
                Uri = new Uri($"https://img-9gag-fun.9cache.com/photo/{ query[2] }_700b.jpg"),
                StopResolving = true,
            });
        }

        public static Task<ResolveResult?> ResolveYouTube(Uri uri)
        {
            if (uri.Host != "youtube.com" && !uri.Host.EndsWith(".youtube.com"))
                return NoResult;

            if (uri.AbsolutePath != "/watch")
                return NoResult;

            var query = HttpUtility.ParseQueryString(uri.Query);

            if (string.IsNullOrEmpty(query["v"]))
                return NoResult;

            return Task.FromResult<ResolveResult?>(new ResolveResult
            {
                Uri = new Uri($"https://img.youtube.com/vi/{ query["v"] }/0.jpg"),
                StopResolving = true,
            });
        }

        public static Task<ResolveResult?> ResolveFacebookFix(Uri uri)
        {
            if (uri.Host != "fbcdn.net" && !uri.Host.EndsWith(".fbcdn.net"))
                return NoResult;

            if (!uri.AbsolutePath.EndsWith(".png") && !uri.AbsolutePath.EndsWith(".jpg") && !uri.AbsolutePath.EndsWith(".jpeg") && !uri.AbsolutePath.EndsWith(".gif"))
                return NoResult;

            return Task.FromResult<ResolveResult?>(new ResolveResult
            {
                Uri = uri,
                StopResolving = true,
            });
        }
    }
}
