using System.Windows;
using System.Windows.Input;

namespace Mushub.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
                return;
            }
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide to tray instead of closing
            Hide();
        }

        private void ShowPage(UIElement page)
        {
            TodoPageView.Visibility = Visibility.Collapsed;
            PomodoroPageView.Visibility = Visibility.Collapsed;
            HubPageView.Visibility = Visibility.Collapsed;
            SettingsPageView.Visibility = Visibility.Collapsed;
            page.Visibility = Visibility.Visible;
        }

        private void NavHome_Click(object sender, RoutedEventArgs e) => ShowPage(TodoPageView);
        private void NavPomodoro_Click(object sender, RoutedEventArgs e) => ShowPage(PomodoroPageView);
        private void NavHub_Click(object sender, RoutedEventArgs e) => ShowPage(HubPageView);
        private void NavSettings_Click(object sender, RoutedEventArgs e) => ShowPage(SettingsPageView);
    }
}
