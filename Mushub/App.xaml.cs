using System;
using System.Threading;
using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using Mushub.Views;
using Mushub.Services;

namespace Mushub
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        private NotifyIcon? _trayIcon;
        private MainWindow? _mainWindow;
        private TrayWidget? _trayWidget;
        private bool _trayWidgetVisible = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Single instance guard
            _mutex = new Mutex(true, "MushubApp_SingleInstance", out bool isNewInstance);
            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show("Mushub est déjà en cours d'exécution.", "Mushub", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // Initialize services
            DataService.Initialize();

            // Setup System Tray
            SetupTrayIcon();

            // Launch main window
            _mainWindow = new MainWindow();
            _mainWindow.Show();
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Text = "Mushub",
                Visible = true,
                Icon = CreateFallbackIcon()
            };

            var contextMenu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("🍄 Ouvrir Mushub");
            openItem.Font = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            openItem.Click += (s, e) => ShowMainWindow();

            var hubItem = new ToolStripMenuItem("⚡ Mini-Hub");
            hubItem.Click += (s, e) => ToggleTrayWidget();

            var separator = new ToolStripSeparator();

            var quitItem = new ToolStripMenuItem("✕ Quitter");
            quitItem.Click += (s, e) => ExitApp();

            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(hubItem);
            contextMenu.Items.Add(separator);
            contextMenu.Items.Add(quitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            // Left click = toggle widget
            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    ToggleTrayWidget();
            };

            // Double click = open main window
            _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private Icon CreateFallbackIcon()
        {
            // Create a simple mushroom-colored icon programmatically as fallback
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.Transparent);
            // Cap
            using var capBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 180, 100, 200));
            g.FillEllipse(capBrush, 1, 1, 14, 9);
            // Stem
            using var stemBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 240, 220, 200));
            g.FillRectangle(stemBrush, 5, 8, 6, 7);
            var handle = bmp.GetHicon();
            return Icon.FromHandle(handle);
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
            }
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        private void ToggleTrayWidget()
        {
            if (_trayWidget == null || !_trayWidget.IsLoaded)
            {
                _trayWidget = new TrayWidget();
                _trayWidget.Closed += (s, e) => { _trayWidgetVisible = false; _trayWidget = null; };
            }

            if (_trayWidgetVisible)
            {
                _trayWidget.Hide();
                _trayWidgetVisible = false;
            }
            else
            {
                PositionWidgetNearTray();
                _trayWidget.Show();
                _trayWidget.Activate();
                _trayWidgetVisible = true;
            }
        }

        private void PositionWidgetNearTray()
        {
            if (_trayWidget == null) return;
            var screen = Screen.PrimaryScreen;
            if (screen == null) return;

            var workArea = screen.WorkingArea;
            _trayWidget.Left = workArea.Right - _trayWidget.Width - 16;
            _trayWidget.Top = workArea.Bottom - _trayWidget.Height - 16;
        }

        private void ExitApp()
        {
            _trayIcon?.Dispose();
            _mutex?.ReleaseMutex();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}
