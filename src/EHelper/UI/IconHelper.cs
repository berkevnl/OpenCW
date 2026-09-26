using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace EHelper.UI
{
    public static class IconHelper
    {
        public static Icon CreateAppIcon()
        {
            const int size = 64;
            using var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Clear transparent
            g.Clear(Color.Transparent);

            // Dark rounded shield background
            using var bgBrush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(size, size),
                Color.FromArgb(255, 15, 23, 42),   // Slate 900
                Color.FromArgb(255, 30, 41, 59)    // Slate 800
            );

            using var path = new GraphicsPath();
            int r = 14;
            path.AddArc(2, 2, r * 2, r * 2, 180, 90);
            path.AddArc(size - 2 - r * 2, 2, r * 2, r * 2, 270, 90);
            path.AddArc(size - 2 - r * 2, size - 2 - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(2, size - 2 - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();

            g.FillPath(bgBrush, path);

            // Vibrant Cyan-Blue glowing border
            using var borderPen = new Pen(Color.FromArgb(255, 14, 165, 233), 2.5f); // Sky 500
            g.DrawPath(borderPen, path);

            // Stylized 'E' monogram
            using var textBrush = new LinearGradientBrush(
                new Point(10, 10),
                new Point(50, 50),
                Color.FromArgb(255, 56, 189, 248),  // Sky 400
                Color.FromArgb(255, 99, 102, 241)   // Indigo 500
            );

            using var font = new Font("Segoe UI", 30, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString("E", font, textBrush, new RectangleF(0, -1, size, size), format);

            return Icon.FromHandle(bmp.GetHicon());
        }
    }
}
