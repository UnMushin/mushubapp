using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Mushub.Models;
using Mushub.Services;

// Aliases pour lever les ambiguïtés WPF / WinForms / System.Drawing
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using FontFamily = System.Windows.Media.FontFamily;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace Mushub.Views
{
    public partial class HubPage : UserControl
    {
        private List<HubTile> _tiles = new();

        public HubPage()
        {
            InitializeComponent();
            Loaded += HubPage_Loaded;
        }

        private void HubPage_Loaded(object sender, RoutedEventArgs e)
        {
            _tiles = DataService.LoadHubTiles();
            if (_tiles.Count == 0)
            {
                _tiles = new List<HubTile>
                {
                    new HubTile { Id = "1", Label = "GitHub", Emoji = "🐙", Url = "https://github.com", Color = "#FF333333" },
                    new HubTile { Id = "2", Label = "YouTube", Emoji = "▶️", Url = "https://youtube.com", Color = "#FFCC0000" },
                    new HubTile { Id = "3", Label = "ChatGPT", Emoji = "🤖", Url = "https://chat.openai.com", Color = "#FF10A37F" },
                    new HubTile { Id = "4", Label = "Notion", Emoji = "📝", Url = "https://notion.so", Color = "#FF000000" },
                    new HubTile { Id = "5", Label = "Spotify", Emoji = "🎵", Url = "https://open.spotify.com", Color = "#FF1DB954" },
                    new HubTile { Id = "6", Label = "Gmail", Emoji = "📧", Url = "https://mail.google.com", Color = "#FFD44638" },
                };
                DataService.SaveHubTiles(_tiles);
            }
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            HubGrid.Children.Clear();
            foreach (var tile in _tiles)
                HubGrid.Children.Add(CreateTileCard(tile));
        }

        private Border CreateTileCard(HubTile tile)
        {
            Color tileColor;
            try { tileColor = (Color)ColorConverter.ConvertFromString(tile.Color ?? "#FF3A3A60"); }
            catch { tileColor = Color.FromRgb(58, 58, 96); }

            var card = new Border
            {
                Margin = new Thickness(6),
                CornerRadius = new CornerRadius(16),
                Cursor = Cursors.Hand,
                MinHeight = 140,
                ClipToBounds = true,
                Background = new LinearGradientBrush(
                    Color.FromArgb(255, 32, 32, 58),
                    Color.FromArgb(80, tileColor.R, tileColor.G, tileColor.B),
                    new Point(0, 0), new Point(1, 1)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, tileColor.R, tileColor.G, tileColor.B)),
                BorderThickness = new Thickness(1.5),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = tileColor,
                    BlurRadius = 16,
                    ShadowDepth = 0,
                    Opacity = 0.2
                }
            };

            var content = new Grid();
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var emoji = new TextBlock
            {
                Text = tile.Emoji,
                FontSize = 42,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 8)
            };
            Grid.SetRow(emoji, 0);
            content.Children.Add(emoji);

            var label = new TextBlock
            {
                Text = tile.Label,
                Foreground = new SolidColorBrush(Color.FromRgb(236, 239, 241)),
                FontFamily = new FontFamily("Segoe UI Variable Display"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(8, 0, 8, 4)
            };
            Grid.SetRow(label, 1);
            content.Children.Add(label);

            var urlHint = new TextBlock
            {
                Text = GetDomain(tile.Url),
                Foreground = new SolidColorBrush(Color.FromArgb(120, 144, 164, 174)),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 12),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(urlHint, 2);
            content.Children.Add(urlHint);

            card.Child = content;

            card.MouseEnter += (s, e) =>
            {
                card.RenderTransform = new ScaleTransform(1.03, 1.03, 0.5, 0.5);
                card.RenderTransformOrigin = new Point(0.5, 0.5);
            };
            card.MouseLeave += (s, e) => card.RenderTransform = null;
            card.MouseLeftButtonUp += (s, e) => LaunchTile(tile);

            var ctxMenu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "🗑️ Supprimer ce raccourci" };
            deleteItem.Click += (s, e) =>
            {
                _tiles.Remove(tile);
                DataService.SaveHubTiles(_tiles);
                RefreshGrid();
            };
            ctxMenu.Items.Add(deleteItem);
            card.ContextMenu = ctxMenu;

            return card;
        }

        private void LaunchTile(HubTile tile)
        {
            try
            {
                Process.Start(new ProcessStartInfo(tile.Url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le raccourci :\n{ex.Message}", "Mushub", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddTile_Click(object sender, RoutedEventArgs e)
        {
            var label = NewTileLabel.Text.Trim();
            var url = NewTileUrl.Text.Trim();
            var emoji = NewTileEmoji.Text.Trim();

            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Veuillez remplir le nom et l'URL du raccourci.", "Mushub", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_tiles.Count >= 9)
            {
                MessageBox.Show("Maximum 9 raccourcis dans le mini-hub.", "Mushub", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var tile = new HubTile
            {
                Id = Guid.NewGuid().ToString(),
                Label = label,
                Url = url,
                Emoji = string.IsNullOrEmpty(emoji) ? "🔗" : emoji,
                Color = GenerateColorFromUrl(url)
            };

            _tiles.Add(tile);
            DataService.SaveHubTiles(_tiles);
            RefreshGrid();

            NewTileLabel.Text = string.Empty;
            NewTileUrl.Text = string.Empty;
            NewTileEmoji.Text = "🌐";
        }

        private string GetDomain(string url)
        {
            try
            {
                var uri = new Uri(url.StartsWith("http") ? url : "https://" + url);
                return uri.Host.Replace("www.", "");
            }
            catch { return url.Length > 20 ? url[..20] + "…" : url; }
        }

        private string GenerateColorFromUrl(string url)
        {
            var hash = url.GetHashCode();
            var r = (hash & 0xFF0000) >> 16;
            var g = (hash & 0x00FF00) >> 8;
            var b = hash & 0x0000FF;
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
