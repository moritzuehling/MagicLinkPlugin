using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    public static class Helper
    {
        public const int MAX_DOWNLOAD = 10 * 1024 * 1024;

        // Images with more pixels than this aren't decoded: a small file can unpack to gigabytes.
        const long MAX_PIXELS = 40_000_000;

        // One client for every request: 10 seconds each, at most 10 MiB per response.
        static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10),
            MaxResponseContentBufferSize = MAX_DOWNLOAD,
        };

        public static async Task<HttpResponseMessage> Send(HttpMethod method, Uri uri, string userAgent = null, string authorization = null)
        {
            using var request = new HttpRequestMessage(method, uri);
            if (userAgent != null)
                request.Headers.UserAgent.ParseAdd(userAgent);
            if (authorization != null)
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            request.Headers.Accept.ParseAdd("*/*");
            return await Http.SendAsync(request);
        }

        public static async Task<string> GetString(string url, string userAgent = null, string authorization = null)
        {
            using var res = await Send(HttpMethod.Get, new Uri(url), userAgent, authorization);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }

        public static async Task<byte[]> GetBytes(string url)
        {
            using var res = await Send(HttpMethod.Get, new Uri(url));
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsByteArrayAsync();
        }

        // Decodes the first frame, refusing images too large to decode safely.
        public static Image LoadImage(byte[] data)
        {
            var info = Image.Identify(data);
            if ((long)info.Width * info.Height > MAX_PIXELS)
                throw new InvalidDataException($"image too large to decode: {info.Width}x{info.Height}");
            return Image.Load(new DecoderOptions { MaxFrames = 1 }, data);
        }

        // A round avatar, transparent outside the circle so it suits Mumble's light and dark themes.
        public static async Task<string> GetRoundImageUrl(string url, int size)
        {
            using var image = LoadImage(await GetBytes(url)).CloneAs<Rgba32>();
            image.Mutate(x => x.Resize(size, size));

            var radius = size / 2f;
            image.ProcessPixelRows(rows =>
            {
                for (var y = 0; y < rows.Height; y++)
                {
                    var row = rows.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        var dx = x + 0.5f - radius;
                        var dy = y + 0.5f - radius;
                        if (dx * dx + dy * dy > radius * radius)
                            row[x] = new Rgba32(0, 0, 0, 0);
                    }
                }
            });

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        }

        // Adapted from
        // https://dotnetthoughts.net/time-ago-function-for-c/
        public static string TimeAgo(this DateTime dateTime)
        {
            string result = string.Empty;
            var timeSpan = DateTime.UtcNow.Subtract(dateTime.ToUniversalTime());

            if (timeSpan <= TimeSpan.FromSeconds(60))
            {
                result = string.Format("{0} seconds ago", timeSpan.Seconds);
            }
            else if (timeSpan <= TimeSpan.FromMinutes(60))
            {
                result = timeSpan.Minutes > 1 ?
                    String.Format("about {0} minutes ago", timeSpan.Minutes) :
                    "about a minute ago";
            }
            else if (timeSpan <= TimeSpan.FromHours(24))
            {
                result = timeSpan.Hours > 1 ?
                    String.Format("about {0} hours ago", timeSpan.Hours) :
                    "about an hour ago";
            }
            else if (timeSpan <= TimeSpan.FromDays(30))
            {
                result = timeSpan.Days > 1 ?
                    String.Format("about {0} days ago", timeSpan.Days) :
                    "yesterday";
            }
            else if (timeSpan <= TimeSpan.FromDays(365))
            {
                result = timeSpan.Days > 30 ?
                    String.Format("about {0} months ago", timeSpan.Days / 30) :
                    "about a month ago";
            }
            else
            {
                result = timeSpan.Days > 365 ?
                    String.Format("about {0} years ago", timeSpan.Days / 365) :
                    "about a year ago";
            }

            return result;
        }

        public static string EncodeAsString(this Image img, int quality = 90)
        {
            using var ms = new MemoryStream();
            img.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
            return $"<img src=\"data:image/jpeg;base64,{ Convert.ToBase64String(ms.ToArray()) }\" alt=\"\">";
        }
    }
}
