using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32;
using Mushub.Services;

// Aliases pour lever les ambiguïtés WPF / WinForms / System.Drawing
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace Mushub.Views
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = DataService.LoadSettings();
            GitHubTokenInput.Text = settings.GitHubToken ?? "";
            GistIdInput.Text = settings.GistId ?? "";
            StartupCheckBox.IsChecked = settings.LaunchOnStartup;

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
        }

        private void SaveGitHubSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = DataService.LoadSettings();
            settings.GitHubToken = GitHubTokenInput.Text.Trim();
            settings.GistId = GistIdInput.Text.Trim();
            DataService.SaveSettings(settings);

            GitHubStatusText.Text = "✅ Paramètres sauvegardés.";
            GitHubStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }

        private async void TestGitHubConnection_Click(object sender, RoutedEventArgs e)
        {
            GitHubStatusText.Text = "⟳ Test en cours...";
            GitHubStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 167, 38));

            try
            {
                var settings = DataService.LoadSettings();
                GitHubService.Configure(settings.GitHubToken ?? "", settings.GistId ?? "");
                await GitHubService.LoadTasksAsync();
                GitHubStatusText.Text = "✅ Connexion GitHub réussie !";
                GitHubStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            catch (Exception ex)
            {
                GitHubStatusText.Text = $"❌ Erreur : {ex.Message}";
                GitHubStatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 83, 80));
            }
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var (hasUpdate, latestVersion) = await UpdateService.CheckForUpdateAsync();
                if (hasUpdate)
                {
                    var res = MessageBox.Show(
                        $"Une mise à jour est disponible : {latestVersion}\nVoulez-vous aller sur GitHub pour la télécharger ?",
                        "Mushub — Mise à jour", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo("https://github.com/UnMushin/mushubapp/releases/latest") { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Mushub est déjà à jour ! 🎉", "Mushub", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de vérifier les mises à jour :\n{ex.Message}", "Mushub", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartupCheckBox_Checked(object sender, RoutedEventArgs e) => SetStartup(true);
        private void StartupCheckBox_Unchecked(object sender, RoutedEventArgs e) => SetStartup(false);

        private void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (enable)
                    key?.SetValue("Mushub", $"\"{exePath}\"");
                else
                    key?.DeleteValue("Mushub", false);

                var settings = DataService.LoadSettings();
                settings.LaunchOnStartup = enable;
                DataService.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la configuration du démarrage :\n{ex.Message}", "Mushub", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
