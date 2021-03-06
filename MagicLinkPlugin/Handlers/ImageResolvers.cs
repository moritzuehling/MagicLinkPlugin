﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                    if (newUri != null && newUri.HasValue)
                    {
                        uri = newUri.Value.Uri;
                        if (newUri.Value.StopResolving)
                            return newUri.Value;
                    }
                }
                catch
                {
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
                const string endpoint = "https://api.imgur.com/3/album/";

                var albumId = uri.AbsolutePath.Split('/')[2];

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
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

        public static Task<ResolveResult?> ResolveFacebookFix(Uri uri)
        {
            if (uri.Host != "fbcdn.net" && !uri.Host.EndsWith(".fbcdn.net"))
                return null;

            if (!uri.AbsolutePath.EndsWith(".png") && !uri.AbsolutePath.EndsWith(".jpg") && !uri.AbsolutePath.EndsWith(".jpeg") && !uri.AbsolutePath.EndsWith(".gif"))
                return null;

            return Task.FromResult<ResolveResult?>(new ResolveResult
            {
                Uri = uri,
                StopResolving = true,
            });
        }
    }
}
