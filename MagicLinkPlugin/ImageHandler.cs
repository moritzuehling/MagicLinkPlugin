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
using static MagicLinkPlugin.Resolvers;

namespace MagicLinkPlugin
{
    public static class ImageHander
    {
        const int MAX_IMAGE_SIZE = 10 * 1024 * 1024;
        const int MAX_DIRECT_SEND = 1 * 1024 * 1024;
        const int MAX_WIDTH = 400;
        const int MAX_HEIGHT = 800;



        public async static Task<string> GetImage(string url, HttpClientHandler handler = null)
        {
            var uri = new Uri(url);
            var resultingUri = await Resolvers.Resolve(uri);

            using (var client = handler != null ? new HttpClient(handler) : new HttpClient())
            {
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

        async static Task<bool> IsImage(Uri url, HttpClient client)
        {
            var res = await client.SendAsync(new HttpRequestMessage
            {
                RequestUri = url,
                Method = HttpMethod.Head,
            });

            var type = res.Content.Headers.ContentType.MediaType;

            return type.StartsWith("image/");
        }

        async static Task<Tuple<byte[], string>> DownloadAndDownsizeImage(Uri uri, HttpClient client)
        {
            using (var res = await client.GetAsync(uri))
            {
                if (res.Content.Headers.ContentLength > MAX_IMAGE_SIZE)
                    throw new Exception("Image too big!");

                byte[] content = await res.Content.ReadAsByteArrayAsync();

                using (var ms = new MemoryStream(content))
                using (var img = Image.FromStream(ms))
                {
                    if (img.Width <= MAX_WIDTH && img.Height <= MAX_HEIGHT && content.Length <= MAX_DIRECT_SEND)
                        return new Tuple<byte[], string>(content, res.Content.Headers.ContentType.MediaType);

                    double wFactor = Math.Max((double)img.Width / MAX_WIDTH, 1.0);
                    double hFactor = Math.Max((double)img.Height / MAX_HEIGHT, 1.0);

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
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
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
