using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    public class ImageHandler : ILinkHandler
    {
        const int MAX_DIRECT_SEND = 1 * 1024 * 1024;
        const int MAX_WIDTH = 400;
        const int MAX_HEIGHT = 800;
        // Some hosts, such as imgur, answer curl with the image itself and browsers with a page around it.
        const string USER_AGENT = "curl/7.54.1";

        private const string ResizeImg = @"({2})<br><a href=""{0}""><img src=""{1}"" alt=""image"" /></a>";

        public async Task<string[]> TryExtractContent(string url)
        {
            var img = await GetImage(url);

            if (img == null)
                return null;

            return new string[] { String.Format(ResizeImg, url, img, GetFileSizeString(img.Length)) };
        }

        public async Task<string> GetImage(string url)
        {
            var resultingUri = await ImageResolvers.Resolve(new Uri(url));
            var isImage = resultingUri.StopResolving || await IsImage(resultingUri.Uri);
            if (!isImage)
                return null;

            var (data, type) = await DownloadAndDownsizeImage(resultingUri.Uri);
            return "data:" + type + ";base64," + Convert.ToBase64String(data);
        }

        static string GetFileSizeString(int size)
        {
            if (size < 1024)
                return size + " Bytes";

            if (size < 1024 * 1024)
                return (size / 1024.0).ToString("0.00") + " KiB";

            return (size / (1024.0 * 1024.0)).ToString("0.00") + " MiB";
        }

        static async Task<bool> IsImage(Uri url)
        {
            using var res = await Helper.Send(HttpMethod.Head, url, USER_AGENT);
            return res.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true;
        }

        // The image as it is if it's small enough, else scaled down to fit and sent as JPEG.
        public static async Task<(byte[], string)> DownloadAndDownsizeImage(Uri uri, int maxWidth = MAX_WIDTH, int maxHeight = MAX_HEIGHT)
        {
            using var res = await Helper.Send(HttpMethod.Get, uri, USER_AGENT);
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsByteArrayAsync();

            using var img = Helper.LoadImage(content);
            var type = res.Content.Headers.ContentType?.MediaType;
            if (img.Width <= maxWidth && img.Height <= maxHeight && content.Length <= MAX_DIRECT_SEND && type?.StartsWith("image/") == true)
                return (content, type);

            double wFactor = Math.Max((double)img.Width / maxWidth, 1.0);
            double hFactor = Math.Max((double)img.Height / maxHeight, 1.0);
            double factor = Math.Max(wFactor, hFactor);

            img.Mutate(x => x.Resize(Math.Max((int)(img.Width / factor), 1), Math.Max((int)(img.Height / factor), 1)));

            using var ms = new MemoryStream();
            img.SaveAsJpeg(ms);
            return (ms.ToArray(), "image/jpeg");
        }
    }
}
