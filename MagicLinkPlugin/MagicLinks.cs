using Fancyauth.API;
using Fancyauth.APIUtil;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    class MagicLinks : PluginBase
    {
        private static readonly Regex LinkPattern = new Regex(@"^\s*<a href=""([^""]*)"">([^<]*)<\/a>\s*$", RegexOptions.Compiled);

        public override async Task OnChatMessage(IUser sender, IEnumerable<IChannelShim> channels, string message)
        {
            var linkMatch = LinkPattern.Match(message);
            if (!linkMatch.Success || linkMatch.Groups[1].Value != linkMatch.Groups[2].Value)
                return;

            var res = await LinkHandler.GetContent(linkMatch.Groups[1].Value);
            if (res == null)
                return;

            foreach (var chan in channels)
                foreach (var msg in res)
                    await chan.SendMessage(msg);
        }
    }
}
