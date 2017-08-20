using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    interface ILinkHandler
    {
        /// <summary>
        /// Tries to extract content from a link, returns null if it fails.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<string> TryExtractContent(string url, HttpClientHandler handler);
    }
}
