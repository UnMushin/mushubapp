using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Mushub.Models;
using Mushub.Services;

// Aliases pour lever les ambiguïtés WPF / WinForms / System.Drawing
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Brush = System.Windows.Media.Brush;
using CheckBox = System.Windows.Controls.CheckBox;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Mushub.Views
{
    public partial class TodoPage : UserControl
    {
        private List<TodoItem> _tasks = new();

        public TodoPage()
        {
            InitializeComponent();
            Loaded += TodoPage_Loaded;
        }

        private async void TodoPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetGreeting();
            await LoadTasksAsync();
        }

        private void SetGreeting()
        {
            var hour = DateTime.Now.Hour;
            DateText.Text = DateTime.Now.ToString("dddd d MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"));

            if (hour >= 5 && hour < 12)
            {
                GreetingText.Text = "Bonjour ! ☕";
                GreetingSubtext.Text = "Belle matinée pour être productif.";
                GreetingEmoji.Text = "🌅";
            }
            else if (hour >= 12 && hour < 14)
            {
                GreetingText.Text = "Bon appétit ! 🍽️";
                GreetingSubtext.Text = "Une petite pause bien méritée.";
                GreetingEmoji.Text = "🌞";
            }
            else if (hour >= 14 && hour < 18)
            {
                GreetingText.Text = "Bonne après-midi !";
                GreetingSubtext.Text = "C'est l'heure de la concentration.";
                GreetingEmoji.Text = "⚡";
            }
            else if (hour >= 18 && hour < 22)
            {
                GreetingText.Text = "Bonne soirée ! 🌙";
                GreetingSubtext.Text = "Terminons ce qu'il reste.";
                GreetingEmoji.Text = "🌆";
            }
            else
            {
                GreetingText.Text = "Bonne nuit... 🌙";
                GreetingSubtext.Text = "Vous travaillez tard ce soir.";
                GreetingEmoji.Text = "🦉";
            }
        }

        private async System.Threading.Tasks.Task LoadTasksAsync()
        {
            try
            {
                SyncStatusText.Text = "⟳ Synchronisation...";
                SyncStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 167, 38));

                _tasks = await GitHubService.LoadTasksAsync();
                RefreshUI();

                SyncStatusText.Text = "● Synchronisé";
                SyncStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            catch
            {
                _tasks = DataService.LoadLocalTasks();
                RefreshUI();

                SyncStatusText.Text = "⚠ Mode local";
                SyncStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 167, 38));
            }
        }

        private void RefreshUI()
        {
            TaskPanel.Children.Clear();

            var sortedTasks = _tasks
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => (int)t.Priority)
                .ToList();

            foreach (var task in sortedTasks)
                TaskPanel.Children.Add(CreateTaskCard(task));

            UpdateStats();
        }

        private Border CreateTaskCard(TodoItem task)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 58)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                Opacity = task.IsCompleted ? 0.6 : 1.0
            };

            var priorityColor = task.Priority switch
            {
                TaskPriority.High => Color.FromRgb(255, 167, 38),
                TaskPriority.Urgent => Color.FromRgb(239, 83, 80),
                TaskPriority.Low => Color.FromRgb(76, 175, 80),
                _ => Color.FromRgb(124, 77, 255)
            };
            card.BorderBrush = new SolidColorBrush(Color.FromArgb(80, priorityColor.R, priorityColor.G, priorityColor.B));

            var innerGrid = new Grid();
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // stripe
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // cb
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // text
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // badge
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // delete

            var stripe = new Border
            {
                Width = 3,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(priorityColor),
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(stripe, 0);
            innerGrid.Children.Add(stripe);

            var cb = new CheckBox
            {
                IsChecked = task.IsCompleted,
                Style = (Style)Application.Current.FindResource("Win11CheckBox"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            cb.Checked += async (s, e) => await ToggleTaskAsync(task, true, card);
            cb.Unchecked += async (s, e) => await ToggleTaskAsync(task, false, card);
            Grid.SetColumn(cb, 1);
            innerGrid.Children.Add(cb);

            var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var taskText = new TextBlock
            {
                Text = task.Title,
                Foreground = new SolidColorBrush(Color.FromRgb(236, 239, 241)),
                FontFamily = new FontFamily("Segoe UI Variable Text"),
                FontSize = 14,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null,
                TextWrapping = TextWrapping.Wrap
            };
            textPanel.Children.Add(taskText);

            if (!string.IsNullOrEmpty(task.Note))
            {
                textPanel.Children.Add(new TextBlock
                {
                    Text = task.Note,
                    Foreground = new SolidColorBrush(Color.FromRgb(84, 110, 122)),
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            textPanel.Children.Add(new TextBlock
            {
                Text = task.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                Foreground = new SolidColorBrush(Color.FromRgb(84, 110, 122)),
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(textPanel, 2);
            innerGrid.Children.Add(textPanel);

            var priorityEmoji = task.Priority switch
            {
                TaskPriority.High => "🟠",
                TaskPriority.Urgent => "🔴",
                TaskPriority.Low => "🟢",
                _ => "🔵"
            };
            var badge = new TextBlock
            {
                Text = priorityEmoji,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(badge, 3);
            innerGrid.Children.Add(badge);

            var deleteBtn = new Button
            {
                Style = (Style)Application.Current.FindResource("IconButton"),
                Content = new TextBlock { Text = "✕", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(84, 110, 122)) },
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += async (s, e) => await DeleteTaskAsync(task);
            Grid.SetColumn(deleteBtn, 4);
            innerGrid.Children.Add(deleteBtn);

            card.Child = innerGrid;
            return card;
        }

        private void NewTaskInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AddTask_Click(sender, e);
        }

        private async void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var title = NewTaskInput.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            var priorityIndex = PriorityCombo.SelectedIndex;
            var priority = priorityIndex switch
            {
                1 => TaskPriority.High,
                2 => TaskPriority.Urgent,
                3 => TaskPriority.Low,
                _ => TaskPriority.Normal
            };

            var task = new TodoItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Priority = priority,
                CreatedAt = DateTime.Now,
                IsCompleted = false
            };

            _tasks.Add(task);
            RefreshUI();
            NewTaskInput.Text = string.Empty;
            await SaveTasksAsync();
        }

        private async System.Threading.Tasks.Task ToggleTaskAsync(TodoItem task, bool completed, Border card)
        {
            task.IsCompleted = completed;
            card.Opacity = completed ? 0.6 : 1.0;
            UpdateStats();
            await SaveTasksAsync();
        }

        private async System.Threading.Tasks.Task DeleteTaskAsync(TodoItem task)
        {
            _tasks.Remove(task);
            RefreshUI();
            await SaveTasksAsync();
        }

        private async void ClearCompleted_Click(object sender, RoutedEventArgs e)
        {
            _tasks.RemoveAll(t => t.IsCompleted);
            RefreshUI();
            await SaveTasksAsync();
        }

        private async System.Threading.Tasks.Task SaveTasksAsync()
        {
            DataService.SaveLocalTasks(_tasks);
            try
            {
                await GitHubService.SaveTasksAsync(_tasks);
                SyncStatusText.Text = "● Synchronisé";
                SyncStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            catch
            {
                SyncStatusText.Text = "⚠ Local uniquement";
                SyncStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 167, 38));
            }
        }

        private void UpdateStats()
        {
            var total = _tasks.Count;
            var done = _tasks.Count(t => t.IsCompleted);
            TotalTasksText.Text = $"{total} tâche{(total > 1 ? "s" : "")}";
            DoneTasksText.Text = $"{done} terminée{(done > 1 ? "s" : "")}";
        }
    }
}
