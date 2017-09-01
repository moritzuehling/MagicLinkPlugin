using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static MagicLinkPlugin.ImageResolvers;

namespace MagicLinkPlugin
{
    public class ImageHander : ILinkHandler
    {
        const int MAX_IMAGE_SIZE = 10 * 1024 * 1024;
        const int MAX_DIRECT_SEND = 1 * 1024 * 1024;
        const int MAX_WIDTH = 400;
        const int MAX_HEIGHT = 800;

        private const string ResizeImg = @"({2})<br><a href=""{0}""><img src=""{1}"" alt=""image"" /></a>";

        public async Task<string[]> TryExtractContent(string url, HttpClientHandler handler)
        {
            var img = await GetImage(url, handler);

            if (img == null)
                return null;

            return new string[] { String.Format(ResizeImg, url, img, GetFileSizeString(img.Length)) };
        }

        public async Task<string> GetImage(string url, HttpClientHandler handler)
        {
            var uri = new Uri(url);
            var resultingUri = await ImageResolvers.Resolve(uri);

            using (var client = handler != null ? new HttpClient(handler) : new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "curl/7.54.1");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                var isImage = resultingUri.StopResolving || await IsImage(resultingUri.Uri, client);
                if (!isImage)
                    return null;

                var data = await DownloadAndDownsizeImage(resultingUri.Uri, client);

                if (data == null)
                    return null;

                return "data:" + data.Item2 + ";base64," + Convert.ToBase64String(data.Item1);
            }
        }

        static string GetFileSizeString(int size)
        {
            if (size < 1024)
                return size + " Bytes";

            if (size < 1024 * 1024)
                return (size / 1024.0).ToString("0.00") + " KiB";

            return (size / (1024.0 * 1024.0)).ToString("0.00") + " MiB";
        }

        async Task<bool> IsImage(Uri url, HttpClient client)
        {
            var res = await client.SendAsync(new HttpRequestMessage
            {
                RequestUri = url,
                Method = HttpMethod.Head,
            });

            var type = res.Content.Headers.ContentType.MediaType;

            return type.StartsWith("image/");
        }

        public static async Task<Tuple<byte[], string>> DownloadAndDownsizeImage(Uri uri, HttpClient client, int maxWidth = MAX_WIDTH, int maxHeight = MAX_HEIGHT)
        {
            using (var res = await client.GetAsync(uri))
            {
                if (res.Content.Headers.ContentLength > MAX_IMAGE_SIZE)
                    throw new Exception("Image too big!");

                byte[] content = await res.Content.ReadAsByteArrayAsync();

                using (var ms = new MemoryStream(content))
                using (var img = Image.FromStream(ms))
                {
                    if (img.Width <= maxWidth && img.Height <= maxHeight && content.Length <= MAX_DIRECT_SEND)
                        return new Tuple<byte[], string>(content, res.Content.Headers.ContentType.MediaType);

                    double wFactor = Math.Max((double)img.Width / maxWidth, 1.0);
                    double hFactor = Math.Max((double)img.Height / maxHeight, 1.0);

                    double factor = wFactor > hFactor ? wFactor : hFactor;

                    int newWidth = (int)(img.Width / factor);
                    int newHeight = (int)(img.Height / factor);

                    using (var b = new Bitmap(newWidth, newHeight))
                    using (var g = Graphics.FromImage(b))
                    {
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.DrawImage(img, new Rectangle(0, 0, newWidth, newHeight));

                        using (var resMs = new MemoryStream())
                        {
                            b.Save(resMs, ImageFormat.Jpeg);
                            resMs.Flush();
                            return new Tuple<byte[], string>(resMs.ToArray(), "image/jpeg");
                        }
                    }

                }
            }
        }
        private ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}
