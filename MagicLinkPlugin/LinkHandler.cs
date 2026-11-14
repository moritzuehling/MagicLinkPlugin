using MagicLinkPlugin.Handlers;
using System;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    public static class LinkHandler
    {
        static readonly ILinkHandler[] Handlers = new ILinkHandler[]
        {
            new RedditHandler(),
            new GistHandler(),
            new YoutubeHandler(),
            new ImageHandler(),
        };

        // The first handler that recognises the link and succeeds, or null. A handler that fails is logged and the next one
        // tried, so a YouTube link still gets its thumbnail when the API is down.
        public async static Task<string[]> GetContent(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;

            foreach (var linkHandler in Handlers)
            {
                try
                {
                    var res = await linkHandler.TryExtractContent(url);
                    if (res != null)
                        return res;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MagicLink] {linkHandler.GetType().Name} failed for {url}: {ex.Message}");
                }
            }

            return null;
        }
    }
}
