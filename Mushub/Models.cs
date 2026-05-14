using System;

namespace Mushub.Models
{
    public class TodoItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string? Note { get; set; }
        public bool IsCompleted { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }

    public enum TaskPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    public class HubTile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Label { get; set; } = "";
        public string Emoji { get; set; } = "🔗";
        public string Url { get; set; } = "";
        public string? Color { get; set; }
        public int Order { get; set; }
    }

    public class AppSettings
    {
        public string? GitHubToken { get; set; }
        public string? GistId { get; set; }
        public bool LaunchOnStartup { get; set; }
        public string Theme { get; set; } = "dark";
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
    }

    public class GistData
    {
        public System.Collections.Generic.List<TodoItem> Tasks { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}
