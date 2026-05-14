using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Mushub.Models;
using Mushub.Services;

// Aliases pour lever les ambiguïtés WPF / WinForms / System.Drawing
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using FontFamily = System.Windows.Media.FontFamily;
using Cursors = System.Windows.Input.Cursors;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace Mushub.Views
{
    public partial class TrayWidget : Window
    {
        private DispatcherTimer _clockTimer = new();

        public TrayWidget()
        {
            InitializeComponent();
            Loaded += TrayWidget_Loaded;
            Deactivated += (s, e) => Hide();
        }

        private void TrayWidget_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTiles();

            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();
        }

        private void UpdateClock()
        {
            WidgetClock.Text = DateTime.Now.ToString("HH:mm — dddd d MMM", new System.Globalization.CultureInfo("fr-FR"));
        }

        private void LoadTiles()
        {
            WidgetGrid.Children.Clear();
            var tiles = DataService.LoadHubTiles();
            foreach (var tile in tiles)
                WidgetGrid.Children.Add(CreateMiniTile(tile));
        }

        private Border CreateMiniTile(HubTile tile)
        {
            Color tileColor;
            try { tileColor = (Color)ColorConverter.ConvertFromString(tile.Color ?? "#FF3A3A60"); }
            catch { tileColor = Color.FromRgb(58, 58, 96); }

            var card = new Border
            {
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(12),
                Cursor = Cursors.Hand,
                Height = 90,
                Background = new LinearGradientBrush(
                    Color.FromArgb(255, 28, 28, 48),
                    Color.FromArgb(60, tileColor.R, tileColor.G, tileColor.B),
                    new Point(0, 0), new Point(1, 1)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, tileColor.R, tileColor.G, tileColor.B)),
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(new TextBlock
            {
                Text = tile.Emoji,
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = tile.Label,
                Foreground = new SolidColorBrush(Color.FromRgb(236, 239, 241)),
                FontFamily = new FontFamily("Segoe UI Variable Text"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4, 2, 4, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            card.Child = stack;

            card.MouseEnter += (s, e) =>
            {
                card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = tileColor, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.5
                };
            };
            card.MouseLeave += (s, e) => card.Effect = null;
            card.MouseLeftButtonUp += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(tile.Url) { UseShellExecute = true }); }
                catch { }
            };

            return card;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        private void CloseWidget_Click(object sender, RoutedEventArgs e) => Hide();

        private void OpenMainApp_Click(object sender, RoutedEventArgs e)
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win is MainWindow mainWin)
                {
                    mainWin.Show();
                    mainWin.WindowState = WindowState.Normal;
                    mainWin.Activate();
                    break;
                }
            }
            Hide();
        }
    }
}
