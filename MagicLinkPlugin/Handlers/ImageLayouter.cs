using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicLinkPlugin.Handlers
{
    static class ImageLayouter
    {
        const int MAX_WIDTH = 400;
        const int MAX_HEIGHT = 400;

        const int BORDER_RADIUS = 9;

        public static Bitmap GenerateEdgeImage()
        {
            Bitmap b = new Bitmap(BORDER_RADIUS * 4, BORDER_RADIUS * 4);

            using (var g = Graphics.FromImage(b))
            using (var brush = new SolidBrush(Color.Transparent))
            {
                g.InterpolationMode = InterpolationMode.High;
                g.CompositingMode = CompositingMode.SourceCopy;
                g.Clear(Color.White);
                g.FillEllipse(brush, new Rectangle(0, 0, BORDER_RADIUS * 8, BORDER_RADIUS * 8));
            }

            return b;
        }
        
        public static Bitmap Layout1(Image image1)
        {
            double xFactor = image1.Width / (double)MAX_WIDTH;
            double yFactor = image1.Height / (double)MAX_HEIGHT;

            var factor = Math.Max(1, Math.Max(xFactor, yFactor));

            var width = (int)(image1.Width / factor);
            var height = (int)(image1.Height / factor);

            Bitmap result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.High;
                g.DrawImage(image1, new Rectangle(0, 0, width, height));

                RoundCorners(g, width, height);
            }

            return result;
        }

        public static Bitmap Layout2(Image[] bitmaps)
        {
            var width = MAX_WIDTH;
            var height = MAX_WIDTH / 2;

            Bitmap result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.High;

                int widthDelim = (int)(width * .5);

                g.Clip = new Region(new Rectangle(0, 0, widthDelim - 1, height));
                var size = GetScaled(bitmaps[0], height);
                g.DrawImage(bitmaps[0], 0 - (size.Width - widthDelim) / 2, 0, size.Width, size.Height);

                g.Clip = new Region(new Rectangle(widthDelim, 0, widthDelim, height));
                size = GetScaled(bitmaps[1], height);
                g.DrawImage(bitmaps[1], widthDelim - (size.Width - widthDelim) / 2, 0, size.Width, size.Height);

                g.Clip = new Region(new Rectangle(0, 0, width, height));

                RoundCorners(g, width, height);
            }

            return result;
        }

        public static Bitmap Layout3(Image[] bitmaps)
        {
            var width = 400;
            var height = 266;

            int widthDelim = 267;
            int smallWidthDelim = width - widthDelim;
            int heightDelim = height / 2;

            Bitmap result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.High;

                g.Clip = new Region(new Rectangle(0, 0, widthDelim - 1, height));
                var size = GetScaled(bitmaps[0], height);
                g.DrawImage(bitmaps[0], 0 - (size.Width - widthDelim) / 2, 0, size.Width, size.Height);

                g.Clip = new Region(new Rectangle(widthDelim, 0, smallWidthDelim, heightDelim - 1));
                size = GetScaled(bitmaps[1], heightDelim);
                g.DrawImage(bitmaps[1], widthDelim - (size.Width - smallWidthDelim) / 2, 0, size.Width, size.Height);

                g.Clip = new Region(new Rectangle(widthDelim, heightDelim, smallWidthDelim, heightDelim));
                size = GetScaled(bitmaps[2], heightDelim);
                g.DrawImage(bitmaps[2], widthDelim - (size.Width - smallWidthDelim) / 2, heightDelim, size.Width, size.Height);

                g.Clip = new Region(new Rectangle(0, 0, width, height));

                RoundCorners(g, width, height);
            }

            return result;
        }

        public static Bitmap Layout4(Image[] bitmaps)
        {
            bitmaps = bitmaps.OrderByDescending(a => a.Width * a.Height).ToArray();
            
            var width = MAX_WIDTH;
            var height = 300;

            Bitmap result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.High;

                int widthDelim = (int)(width * .75);
                int smallWidthDelim = (int)(width * .25);
                int heightDelim = (int)(height / 3.0);

                g.Clip = new Region(new Rectangle(0, 0, widthDelim -1, height));

                var size = GetScaled(bitmaps[0], height);
                g.DrawImage(bitmaps[0], 0 - (size.Width - widthDelim) / 2, 0, size.Width, size.Height);

                g.Clip = new Region(new Rectangle(widthDelim, 0, smallWidthDelim, heightDelim -1));
                var size2 = GetScaled(bitmaps[0], heightDelim -1);
                g.DrawImage(bitmaps[1], widthDelim - (size2.Width - smallWidthDelim) / 2, 0, size2.Width, size2.Height);


                g.Clip = new Region(new Rectangle(widthDelim, heightDelim, smallWidthDelim, heightDelim - 1));
                size2 = GetScaled(bitmaps[0], heightDelim - 1);
                g.DrawImage(bitmaps[2], widthDelim - (size2.Width - smallWidthDelim) / 2, heightDelim, size2.Width, size2.Height);

                g.Clip = new Region(new Rectangle(widthDelim, heightDelim * 2, smallWidthDelim, heightDelim));
                size2 = GetScaled(bitmaps[0], heightDelim);
                g.DrawImage(bitmaps[3], widthDelim - (size2.Width - smallWidthDelim) / 2, heightDelim * 2, size2.Width, size2.Height);

                g.Clip = new Region(new Rectangle(0, 0, width, height));

                RoundCorners(g, width, height);
            }

            return result;
        }

        static void RoundCorners(Graphics g, int width, int height)
        {
            using (var corner = GenerateEdgeImage())
            {
                g.DrawImage(corner, new Rectangle(-1, -1, BORDER_RADIUS, BORDER_RADIUS));
                corner.RotateFlip(RotateFlipType.Rotate90FlipNone);
                g.DrawImage(corner, new Rectangle(width - BORDER_RADIUS, -1, BORDER_RADIUS, BORDER_RADIUS));
                corner.RotateFlip(RotateFlipType.Rotate90FlipNone);
                g.DrawImage(corner, new Rectangle(width - BORDER_RADIUS + 1, height - BORDER_RADIUS + 1, BORDER_RADIUS, BORDER_RADIUS));
                corner.RotateFlip(RotateFlipType.Rotate90FlipNone);
                g.DrawImage(corner, new Rectangle(-1, height - BORDER_RADIUS + 1, BORDER_RADIUS, BORDER_RADIUS));
            }
        }

        static Size GetScaled(Image image, int targetHeight)
        {
            return new Size((int)(image.Width * (targetHeight / (double) image.Height)), targetHeight);
        }
    }
}
