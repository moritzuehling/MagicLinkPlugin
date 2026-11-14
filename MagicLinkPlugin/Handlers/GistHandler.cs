using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MagicLinkPlugin.Handlers
{
    // Plain code: the highlighter used before needs System.Drawing, which .NET only has on Windows.
    class GistHandler : ILinkHandler
    {
        const int MAX_FILES = 3;
        const int MAX_LINES = 25;
        const int MAX_COLUMN = 200;
        const string USER_AGENT = "MumbleGistPreview";

        public async Task<string[]> TryExtractContent(string url)
        {
            var uri = new System.Uri(url);
            if (uri.Host != "gist.github.com")
                return null;

            if (uri.Query != "")
                return null;

            var parts = uri.AbsolutePath.Split('/');
            if (parts.Length != 3)
                return null;

            return await GetFiles(parts[2]);
        }

        public async Task<string[]> GetFiles(string gist)
        {
            var result = await Helper.GetString("https://api.github.com/gists/" + System.Uri.EscapeDataString(gist), USER_AGENT);
            var obj = JObject.Parse(result);

            return obj["files"]
                .Take(MAX_FILES)
                .Select(a => FormatFile(
                    ((JProperty)a).Value["filename"].ToObject<string>(),
                    ((JProperty)a).Value["content"].ToObject<string>()))
                .ToArray();
        }

        public string FormatFile(string fileName, string content)
        {
            var length = content.Split('\n').Length;

            var cleanCodeArr =
                content
                .Replace("\r\n", "\n")
                .Split('\n')
                .Take(MAX_LINES)
                .Select(a => a.Length < MAX_COLUMN ? a : a.Substring(0, MAX_COLUMN))
                .ToArray();

            var nonEmpty = cleanCodeArr.Where(a => a.Length > 0).ToArray();
            var minTabCount = nonEmpty.Select(a => CountAtBeginning(a, '\t')).DefaultIfEmpty(0).Min();
            var minSpaceCount = nonEmpty.Select(a => CountAtBeginning(a.Substring(minTabCount), ' ')).DefaultIfEmpty(0).Min();

            cleanCodeArr = cleanCodeArr
                .Select(a => a.Length == 0 ? a : a.Substring(minTabCount).Substring(minSpaceCount))
                .Select(a => a.Replace("\t", "    "))
                .ToArray();

            StringBuilder res = new StringBuilder();
            res.AppendFormat("<b>{0}</b>:", WebUtility.HtmlEncode(fileName));
            res.Append("<pre>");
            res.Append(WebUtility.HtmlEncode(string.Join("\n", cleanCodeArr)));
            res.Append("</pre>");

            if (length > MAX_LINES)
                res.Append("(truncated)");

            return res.ToString();
        }

        public int CountAtBeginning(string line, char chr)
        {
            int i = 0;
            while (line.Length != i && line[i] == chr)
                i++;

            return i;
        }
    }
}
