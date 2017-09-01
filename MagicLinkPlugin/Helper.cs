using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MagicLinkPlugin
{
    public static class Helper
    {
        public static async Task<string> GetRoundImageUrl(string url, int size)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                using (var stream = await client.GetStreamAsync(url))
                using (var source = Image.FromStream(stream))
                using (var target = new Bitmap(size, size))
                using (var graphics = Graphics.FromImage(target))
                using (var textureBrush = new TextureBrush(source))
                using (var ms = new MemoryStream())
                {
                    textureBrush.ScaleTransform(size / (float)source.Width, size / (float)source.Width);
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.Clear(Color.White);
                    graphics.FillEllipse(textureBrush, new Rectangle(0, 0, 48, 48));

                    target.Save(ms, ImageFormat.Png);

                    return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                  }

            }
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

        public static async Task<Image> GetBitmap(Uri uri)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var data = await client.GetByteArrayAsync(uri);
                using (var ms = new MemoryStream(data))
                {
                    return Image.FromStream(ms);
                }
            }
        }

        public static string EncodeAsString(this Image img, long quality = 90)
        {
            var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var encParams = new EncoderParameters() { Param = new[] { new EncoderParameter(Encoder.Quality, quality) } };

            using (var ms = new MemoryStream())
            {
                img.Save(ms, encoder, encParams);
                return $"<img src=\"data:image/jpeg;base64,{ Convert.ToBase64String(ms.ToArray()) }\" alt=\"\">";
            }
        }
        

        public static  GraphicsPath MakeRoundedRect(RectangleF rect, float xradius, float yradius, bool round_ul = true, bool round_ur = true, bool round_lr = true, bool round_ll = true)
        {
            // Make a GraphicsPath to draw the rectangle.
            PointF point1, point2;
            GraphicsPath path = new GraphicsPath();

            // Upper left corner.
            if (round_ul)
            {
                RectangleF corner = new RectangleF(
                    rect.X, rect.Y,
                    2 * xradius, 2 * yradius);
                path.AddArc(corner, 180, 90);
                point1 = new PointF(rect.X + xradius, rect.Y);
            }
            else point1 = new PointF(rect.X, rect.Y);

            // Top side.
            if (round_ur)
                point2 = new PointF(rect.Right - xradius, rect.Y);
            else
                point2 = new PointF(rect.Right, rect.Y);
            path.AddLine(point1, point2);

            // Upper right corner.
            if (round_ur)
            {
                RectangleF corner = new RectangleF(
                    rect.Right - 2 * xradius, rect.Y,
                    2 * xradius, 2 * yradius);
                path.AddArc(corner, 270, 90);
                point1 = new PointF(rect.Right, rect.Y + yradius);
            }
            else point1 = new PointF(rect.Right, rect.Y);

            // Right side.
            if (round_lr)
                point2 = new PointF(rect.Right, rect.Bottom - yradius);
            else
                point2 = new PointF(rect.Right, rect.Bottom);
            path.AddLine(point1, point2);

            // Lower right corner.
            if (round_lr)
            {
                RectangleF corner = new RectangleF(
                    rect.Right - 2 * xradius,
                    rect.Bottom - 2 * yradius,
                    2 * xradius, 2 * yradius);
                path.AddArc(corner, 0, 90);
                point1 = new PointF(rect.Right - xradius, rect.Bottom);
            }
            else point1 = new PointF(rect.Right, rect.Bottom);

            // Bottom side.
            if (round_ll)
                point2 = new PointF(rect.X + xradius, rect.Bottom);
            else
                point2 = new PointF(rect.X, rect.Bottom);
            path.AddLine(point1, point2);

            // Lower left corner.
            if (round_ll)
            {
                RectangleF corner = new RectangleF(
                    rect.X, rect.Bottom - 2 * yradius,
                    2 * xradius, 2 * yradius);
                path.AddArc(corner, 90, 90);
                point1 = new PointF(rect.X, rect.Bottom - yradius);
            }
            else point1 = new PointF(rect.X, rect.Bottom);

            // Left side.
            if (round_ul)
                point2 = new PointF(rect.X, rect.Y + yradius);
            else
                point2 = new PointF(rect.X, rect.Y);
            path.AddLine(point1, point2);

            // Join with the start point.
            path.CloseFigure();

            return path;
        }

    }
}
