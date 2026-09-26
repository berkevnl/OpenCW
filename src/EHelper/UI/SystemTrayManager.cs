using System;
using System.Drawing;
using System.Windows.Forms;
using EHelper.Hardware;

namespace EHelper.UI
{
    public class SystemTrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly MainWindow _mainWindow;
        private readonly Action _onExit;
        private bool _isDisposed;

        public SystemTrayManager(MainWindow mainWindow, Action onExit)
        {
            _mainWindow = mainWindow;
            _onExit = onExit;

            _notifyIcon = new NotifyIcon
            {
                Icon = IconHelper.CreateAppIcon(),
                Text = "CPU: --°C Fan: --RPM\nGPU: --°C Fan: --RPM",
                Visible = true
            };

            SetupContextMenu();
            SetupEvents();
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("E-Helper'ı Aç", null, (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() => _mainWindow.ToggleFlyout());
            })
            {
                Font = new Font(menu.Font, System.Drawing.FontStyle.Bold)
            };

            var exitItem = new ToolStripMenuItem("Çıkış", null, (s, e) =>
            {
                _onExit();
            });

            menu.Items.Add(openItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        private void SetupEvents()
        {
            _notifyIcon.MouseClick += (s, e) =>
            {
                // Left click toggles flyout panel
                if (e.Button == MouseButtons.Left)
                {
                    _mainWindow.Dispatcher.Invoke(() => _mainWindow.ToggleFlyout());
                }
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                _mainWindow.Dispatcher.Invoke(() => _mainWindow.ToggleFlyout());
            };
        }

        public void UpdateTooltip(string text)
        {
            if (!_isDisposed && _notifyIcon != null)
            {
                try
                {
                    _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
                }
                catch
                {
                    // Ignore transient Win32 tray notification errors
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
