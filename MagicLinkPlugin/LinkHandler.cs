using MagicLinkPlugin.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    public static class LinkHandler
    {

        static ILinkHandler[] Handlers = new ILinkHandler[]
        {
            new RedditHandler(),
            new TwitterHandler(),
            new GistHandler(),
            new ImageHander(),
        };

        public async static Task<string[]> GetContent(string url, HttpClientHandler handler = null)
        {
            foreach (var linkHandler in Handlers)
            {
                var res = await linkHandler.TryExtractContent(url, handler);

                if (res != null)
                    return res;
            }

            return null;
        }

    }
}
