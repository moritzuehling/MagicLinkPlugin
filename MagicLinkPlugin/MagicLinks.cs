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
        private const string ResizeImg = @"<a href=""{0}""><img src=""{1}"" alt=""image"" /></a>";

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

                try
                {
                    var img = await ImageHander.GetImage(link, clientHandler);

                    if (img != null)
                        foreach (var chan in channels)
                            await chan.SendMessage(String.Format(ResizeImg, link, img));
                }
                catch
                {
                }
            }
        }

    }
}
