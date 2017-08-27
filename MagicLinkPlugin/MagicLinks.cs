using Fancyauth.API;
using Fancyauth.APIUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    class MagicLinks : PluginBase
    {
        private static readonly Regex LinkPattern = new Regex(@"^<a href=""([^""]*)"">([^<]*)<\/a>$", RegexOptions.Compiled);

        public override async Task OnChatMessage(IUser sender, IEnumerable<IChannelShim> channels, string message)
        {
            var linkMatch = LinkPattern.Match(message);
            if (linkMatch.Success && (linkMatch.Groups[1].Captures[0].Value == linkMatch.Groups[2].Captures[0].Value))
            {
                var link = linkMatch.Groups[1].Captures[0].Value;

                var clientHandler = new HttpClientHandler
                {
                    Proxy = new WebProxy("http://127.0.0.1:8118")
                };

                var res = await LinkHandler.GetContent(link, clientHandler);

                if (res == null)
                    return;

                foreach (var chan in channels)
                    foreach (var msg in res)
                        await chan.SendMessage(msg);
            }
        }

        public string GetFileSizeString(int size)
        {
            if (size < 1024)
                return size + " Bytes";

            if (size < 1024 * 1024)
                return (size / 1024.0).ToString("0.00") + " KiB";

            return (size / (1024.0 * 1024.0)).ToString("0.00") + " MiB";
        }

    }
}
