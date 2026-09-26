using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace EHelper.UI
{
    public static class IconHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon CreateAppIcon()
        {
            try
            {
                // 1. Try local Assets directory beside the executable
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string localIco = Path.Combine(baseDir, "Assets", "app.ico");
                if (File.Exists(localIco))
                {
                    return new Icon(localIco);
                }

                // 2. Try embedded resource stream
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("EHelper.Assets.app.ico");
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
            catch
            {
                // Fall back to runtime vector rendering
            }

            return GenerateFallbackIcon();
        }

        private static Icon GenerateFallbackIcon()
        {
            const int size = 64;
            using var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                // Blue circle
                using var brush = new SolidBrush(Color.FromArgb(255, 108, 180, 238));
                g.FillEllipse(brush, 2, 2, size - 4, size - 4);

                // White bold 'E'
                using var textBrush = new SolidBrush(Color.White);
                using var font = new Font("Segoe UI", 38, FontStyle.Bold, GraphicsUnit.Pixel);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString("E", font, textBrush, new RectangleF(0, -3, size, size), format);
            }

            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using var temp = Icon.FromHandle(hIcon);
                return (Icon)temp.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
    }
}
