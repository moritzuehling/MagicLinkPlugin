using Highlight;
using Highlight.Engines;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MagicLinkPlugin.Handlers
{
    class GistHandler : ILinkHandler
    {
        const int MAX_LINES = 25;
        const int MAX_COLUMN = 200;

        public async Task<string[]> TryExtractContent(string url, HttpClientHandler handler)
        {
            var uri = new Uri(url);
            if (uri.Host != "gist.github.com")
                return null;

            if (uri.Query != "")
                return null;

            var parts = uri.AbsolutePath.Split('/');
            if (parts.Length != 3)
                return null;

            var gistId = parts[2];


            return await GetFiles(gistId);
        }


        public async Task<string[]> GetFiles(string gist)
        {
            using (var wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.UserAgent] = "MumbleGistPreview";
                var result = await wc.DownloadStringTaskAsync("https://api.github.com/gists/" + gist);
                var obj = JObject.Parse(result);
                var files = obj["files"];

                return files
                    .Select(a => FormatFile
                    (
                        ((JProperty)a).Value["filename"].ToObject<string>(),
                        ((JProperty)a).Value["language"].ToObject<string>(),
                        ((JProperty)a).Value["content"].ToObject<string>())
                    )
                    .ToArray();
            }
        }

        public string FormatFile(string fileName, string languageString, string content)
        {
            var length = content.Split('\n').Length;

            var cleanCodeArr =
                content
                .Replace("\r\n", "\n")
                .Split('\n')
                .Take(MAX_LINES)
                .Select(a => a.Length < MAX_COLUMN ? a : a.Substring(0, MAX_COLUMN))
                .ToArray();

            var minTabCount = cleanCodeArr.Where(a => a.Length > 0).Min(a => CountAtBeginning(a, '\t'));
            var minSpaceCount = cleanCodeArr.Where(a => a.Length > 0).Min(a => CountAtBeginning(a, ' '));

            cleanCodeArr = cleanCodeArr
                .Select(a => a.Length == 0 ? a : a.Substring(minTabCount).Substring(minSpaceCount))
                .Select(a => a.Replace("\t", "    "))
                .ToArray();

            string cleanCode = string.Join("\n", cleanCodeArr);

            StringBuilder res = new StringBuilder();
            res.AppendFormat("<b>{0}</b>:", fileName);
            res.Append("<pre>");
            var high = new Highlighter(new HtmlEngine());
            var highlightedCode = high.Highlight(languageString, cleanCode).Replace("background-color: Transparent", "");

            res.Append(highlightedCode);

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
