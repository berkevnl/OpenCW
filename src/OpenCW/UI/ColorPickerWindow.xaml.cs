using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Color = System.Windows.Media.Color;
using Button = System.Windows.Controls.Button;

namespace OpenCW.UI
{
    public partial class ColorPickerWindow : Window
    {
        public byte OriginalRed { get; }
        public byte OriginalGreen { get; }
        public byte OriginalBlue { get; }

        public byte SelectedRed { get; private set; }
        public byte SelectedGreen { get; private set; }
        public byte SelectedBlue { get; private set; }

        public event Action<byte, byte, byte>? ColorPreviewChanged;

        private double _hue;         // 0.0 - 360.0
        private double _saturation;  // 0.0 - 1.0
        private double _value;       // 0.0 - 1.0

        private bool _isUpdating;
        private bool _isDraggingSv;
        private bool _isDraggingHue;

        public ColorPickerWindow(byte initialR, byte initialG, byte initialB)
        {
            InitializeComponent();

            OriginalRed = initialR;
            OriginalGreen = initialG;
            OriginalBlue = initialB;

            SelectedRed = initialR;
            SelectedGreen = initialG;
            SelectedBlue = initialB;

            BrdOldColor.Background = new SolidColorBrush(Color.FromRgb(initialR, initialG, initialB));

            RgbToHsv(initialR, initialG, initialB, out _hue, out _saturation, out _value);

            Loaded += (s, e) =>
            {
                UpdateFromHsv(updateInputs: true, updateReticle: true, updateHueThumb: true);
            };
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        #region SV Canvas Interaction

        private void SvBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDraggingSv = true;
                ((UIElement)sender).CaptureMouse();
                UpdateSvFromMouse(e.GetPosition(GridSvBox));
            }
        }

        private void SvBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingSv && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSvFromMouse(e.GetPosition(GridSvBox));
            }
        }

        private void SvBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSv)
            {
                _isDraggingSv = false;
                ((UIElement)sender).ReleaseMouseCapture();
            }
        }

        private void UpdateSvFromMouse(Point pos)
        {
            double width = GridSvBox.ActualWidth;
            double height = GridSvBox.ActualHeight;
            if (width <= 0 || height <= 0) return;

            _saturation = Math.Clamp(pos.X / width, 0.0, 1.0);
            _value = Math.Clamp(1.0 - (pos.Y / height), 0.0, 1.0);

            UpdateFromHsv(updateInputs: true, updateReticle: true, updateHueThumb: false);
        }

        #endregion

        #region Hue Bar Interaction

        private void HueBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDraggingHue = true;
                ((UIElement)sender).CaptureMouse();
                UpdateHueFromMouse(e.GetPosition(GridHueBar));
            }
        }

        private void HueBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingHue && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateHueFromMouse(e.GetPosition(GridHueBar));
            }
        }

        private void HueBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingHue)
            {
                _isDraggingHue = false;
                ((UIElement)sender).ReleaseMouseCapture();
            }
        }

        private void UpdateHueFromMouse(Point pos)
        {
            double width = GridHueBar.ActualWidth;
            if (width <= 0) return;

            _hue = Math.Clamp((pos.X / width) * 360.0, 0.0, 359.9);
            UpdateFromHsv(updateInputs: true, updateReticle: false, updateHueThumb: true);
        }

        #endregion

        #region Swatch Selection

        private void Swatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                ApplyHexCode(hex);
            }
        }

        #endregion

        #region Text Input Handling

        private void TxtHexInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            string text = TxtHexInput.Text.Trim();
            if (text.StartsWith("#") && (text.Length == 7))
            {
                ApplyHexCode(text, fromHexInput: true);
            }
        }

        private void TxtRgbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (byte.TryParse(TxtRInput.Text, out byte r) &&
                byte.TryParse(TxtGInput.Text, out byte g) &&
                byte.TryParse(TxtBInput.Text, out byte b))
            {
                SelectedRed = r;
                SelectedGreen = g;
                SelectedBlue = b;

                RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
                UpdateFromHsv(updateInputs: false, updateReticle: true, updateHueThumb: true);

                _isUpdating = true;
                TxtHexInput.Text = $"#{r:X2}{g:X2}{b:X2}";
                _isUpdating = false;
            }
        }

        private void ApplyHexCode(string hex, bool fromHexInput = false)
        {
            string clean = hex.TrimStart('#');
            if (clean.Length == 6 &&
                byte.TryParse(clean[..2], NumberStyles.HexNumber, null, out byte r) &&
                byte.TryParse(clean[2..4], NumberStyles.HexNumber, null, out byte g) &&
                byte.TryParse(clean[4..6], NumberStyles.HexNumber, null, out byte b))
            {
                SelectedRed = r;
                SelectedGreen = g;
                SelectedBlue = b;

                RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
                UpdateFromHsv(updateInputs: !fromHexInput, updateReticle: true, updateHueThumb: true);

                if (!fromHexInput)
                {
                    _isUpdating = true;
                    TxtHexInput.Text = $"#{r:X2}{g:X2}{b:X2}";
                    _isUpdating = false;
                }
            }
        }

        #endregion

        #region Core HSV & Geometry Calculations

        private void UpdateFromHsv(bool updateInputs, bool updateReticle, bool updateHueThumb)
        {
            var (r, g, b) = HsvToRgb(_hue, _saturation, _value);
            SelectedRed = r;
            SelectedGreen = g;
            SelectedBlue = b;

            // 1. Update pure Hue background of 2D SV Box
            var (pureR, pureG, pureB) = HsvToRgb(_hue, 1.0, 1.0);
            StopHueColor.Color = Color.FromRgb(pureR, pureG, pureB);

            // 2. Update New Color Preview Box
            BrdNewColor.Background = new SolidColorBrush(Color.FromRgb(r, g, b));

            // 3. Update Text Inputs if not currently typed by user
            if (updateInputs)
            {
                _isUpdating = true;
                TxtHexInput.Text = $"#{r:X2}{g:X2}{b:X2}";
                TxtRInput.Text = r.ToString();
                TxtGInput.Text = g.ToString();
                TxtBInput.Text = b.ToString();
                _isUpdating = false;
            }

            // 4. Update Reticle Indicator Position on 2D SV Box
            if (updateReticle && GridSvBox.ActualWidth > 0 && GridSvBox.ActualHeight > 0)
            {
                double x = _saturation * GridSvBox.ActualWidth - (Reticle.Width / 2.0);
                double y = (1.0 - _value) * GridSvBox.ActualHeight - (Reticle.Height / 2.0);
                Canvas.SetLeft(Reticle, Math.Clamp(x, -Reticle.Width / 2.0, GridSvBox.ActualWidth - Reticle.Width / 2.0));
                Canvas.SetTop(Reticle, Math.Clamp(y, -Reticle.Height / 2.0, GridSvBox.ActualHeight - Reticle.Height / 2.0));
            }

            // 5. Update Hue Slider Indicator Position
            if (updateHueThumb && GridHueBar.ActualWidth > 0)
            {
                double thumbX = (_hue / 360.0) * (GridHueBar.ActualWidth - HueThumb.Width);
                Canvas.SetLeft(HueThumb, Math.Clamp(thumbX, 0, GridHueBar.ActualWidth - HueThumb.Width));
            }

            // 6. Broadcast Real-time Live Preview to physical keyboard hardware!
            ColorPreviewChanged?.Invoke(r, g, b);
        }

        private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1.0 - Math.Abs((h / 60.0) % 2.0 - 1.0));
            double m = v - c;

            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return ((byte)Math.Round((r + m) * 255.0),
                    (byte)Math.Round((g + m) * 255.0),
                    (byte)Math.Round((b + m) * 255.0));
        }

        private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            v = max;
            s = max == 0.0 ? 0.0 : delta / max;

            if (delta == 0.0)
            {
                h = 0.0;
            }
            else if (max == rd)
            {
                h = 60.0 * (((gd - bd) / delta) % 6.0);
            }
            else if (max == gd)
            {
                h = 60.0 * (((bd - rd) / delta) + 2.0);
            }
            else
            {
                h = 60.0 * (((rd - gd) / delta) + 4.0);
            }

            if (h < 0.0)
            {
                h += 360.0;
            }
        }

        #endregion
    }
}
