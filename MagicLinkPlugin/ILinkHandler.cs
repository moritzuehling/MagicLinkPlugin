using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    interface ILinkHandler
    {
        /// <summary>
        /// Tries to extract content from a link, returns null if it fails.
        /// </summary>
        Task<string[]> TryExtractContent(string url);
    }
}
