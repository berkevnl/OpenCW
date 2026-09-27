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

        private bool _isUpdating = true;
        private bool _isDraggingSv;
        private bool _isDraggingHue;

        public ColorPickerWindow(byte initialR, byte initialG, byte initialB)
        {
            _isUpdating = true;
            InitializeComponent();

            OriginalRed = initialR;
            OriginalGreen = initialG;
            OriginalBlue = initialB;

            SelectedRed = initialR;
            SelectedGreen = initialG;
            SelectedBlue = initialB;

            if (BrdOldColor != null)
            {
                BrdOldColor.Background = new SolidColorBrush(Color.FromRgb(initialR, initialG, initialB));
            }

            RgbToHsv(initialR, initialG, initialB, out _hue, out _saturation, out _value);

            // Populate text inputs safely before wiring events
            if (TxtHexInput != null) TxtHexInput.Text = $"#{initialR:X2}{initialG:X2}{initialB:X2}";
            if (TxtRInput != null) TxtRInput.Text = initialR.ToString();
            if (TxtGInput != null) TxtGInput.Text = initialG.ToString();
            if (TxtBInput != null) TxtBInput.Text = initialB.ToString();

            // Wire events after control tree is established
            if (TxtHexInput != null) TxtHexInput.TextChanged += TxtHexInput_TextChanged;
            if (TxtRInput != null) TxtRInput.TextChanged += TxtRgbInput_TextChanged;
            if (TxtGInput != null) TxtGInput.TextChanged += TxtRgbInput_TextChanged;
            if (TxtBInput != null) TxtBInput.TextChanged += TxtRgbInput_TextChanged;

            Loaded += (s, e) =>
            {
                _isUpdating = false;
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
            if (e.LeftButton == MouseButtonState.Pressed && GridSvBox != null)
            {
                _isDraggingSv = true;
                ((UIElement)sender).CaptureMouse();
                UpdateSvFromMouse(e.GetPosition(GridSvBox));
            }
        }

        private void SvBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingSv && e.LeftButton == MouseButtonState.Pressed && GridSvBox != null)
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
            if (GridSvBox == null) return;
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
            if (e.LeftButton == MouseButtonState.Pressed && GridHueBar != null)
            {
                _isDraggingHue = true;
                ((UIElement)sender).CaptureMouse();
                UpdateHueFromMouse(e.GetPosition(GridHueBar));
            }
        }

        private void HueBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingHue && e.LeftButton == MouseButtonState.Pressed && GridHueBar != null)
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
            if (GridHueBar == null) return;
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
            if (_isUpdating || TxtHexInput == null) return;
            string text = TxtHexInput.Text.Trim();
            if (text.StartsWith("#") && (text.Length == 7))
            {
                ApplyHexCode(text, fromHexInput: true);
            }
        }

        private void TxtRgbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || TxtRInput == null || TxtGInput == null || TxtBInput == null) return;

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
                if (TxtHexInput != null) TxtHexInput.Text = $"#{r:X2}{g:X2}{b:X2}";
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
                    if (TxtHexInput != null) TxtHexInput.Text = $"#{r:X2}{g:X2}{b:X2}";
                    _isUpdating = false;
                }
            }
        }

        #endregion

        #region Core HSV & Geometry Calculations

        private double _lastRenderedHue = -1;
        private byte _lastReportedR = 255, _lastReportedG = 255, _lastReportedB = 255;
        private readonly SolidColorBrush _newColorBrush = new SolidColorBrush();

        private void UpdateFromHsv(bool updateInputs, bool updateReticle, bool updateHueThumb)
        {
            try
            {
                var (r, g, b) = HsvToRgb(_hue, _saturation, _value);
                SelectedRed = r;
                SelectedGreen = g;
                SelectedBlue = b;

                // 1. Update pure Hue background of 2D SV Box only if Hue actually changed
                if (StopHueColor != null && Math.Abs(_hue - _lastRenderedHue) > 0.05)
                {
                    _lastRenderedHue = _hue;
                    var (pureR, pureG, pureB) = HsvToRgb(_hue, 1.0, 1.0);
                    StopHueColor.Color = Color.FromRgb(pureR, pureG, pureB);
                }

                // 2. Update New Color Preview Box with reusable brush (0 allocations)
                if (BrdNewColor != null)
                {
                    _newColorBrush.Color = Color.FromRgb(r, g, b);
                    if (!ReferenceEquals(BrdNewColor.Background, _newColorBrush))
                    {
                        BrdNewColor.Background = _newColorBrush;
                    }
                }

                // 3. Update Text Inputs if requested
                if (updateInputs)
                {
                    _isUpdating = true;
                    if (TxtHexInput != null) TxtHexInput.Text = $"#{r:X2}{g:X2}{b:X2}";
                    if (TxtRInput != null) TxtRInput.Text = r.ToString();
                    if (TxtGInput != null) TxtGInput.Text = g.ToString();
                    if (TxtBInput != null) TxtBInput.Text = b.ToString();
                    _isUpdating = false;
                }

                // 4. Update Reticle Indicator Position on 2D SV Box
                if (updateReticle && GridSvBox != null && Reticle != null && GridSvBox.ActualWidth > 0 && GridSvBox.ActualHeight > 0)
                {
                    double x = _saturation * GridSvBox.ActualWidth - (Reticle.Width / 2.0);
                    double y = (1.0 - _value) * GridSvBox.ActualHeight - (Reticle.Height / 2.0);
                    Canvas.SetLeft(Reticle, Math.Clamp(x, -Reticle.Width / 2.0, GridSvBox.ActualWidth - Reticle.Width / 2.0));
                    Canvas.SetTop(Reticle, Math.Clamp(y, -Reticle.Height / 2.0, GridSvBox.ActualHeight - Reticle.Height / 2.0));
                }

                // 5. Update Hue Slider Indicator Position
                if (updateHueThumb && GridHueBar != null && HueThumb != null && GridHueBar.ActualWidth > 0)
                {
                    double thumbX = (_hue / 360.0) * (GridHueBar.ActualWidth - HueThumb.Width);
                    Canvas.SetLeft(HueThumb, Math.Clamp(thumbX, 0, GridHueBar.ActualWidth - HueThumb.Width));
                }

                // 6. Broadcast Real-time Live Preview only if RGB integer values actually changed
                if (r != _lastReportedR || g != _lastReportedG || b != _lastReportedB)
                {
                    _lastReportedR = r;
                    _lastReportedG = g;
                    _lastReportedB = b;
                    ColorPreviewChanged?.Invoke(r, g, b);
                }
            }
            catch
            {
                // Defensive guard against layout cycle interruptions
            }
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
