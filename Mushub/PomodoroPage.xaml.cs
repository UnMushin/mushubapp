using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

// Aliases pour lever les ambiguïtés WPF / WinForms / System.Drawing
using UserControl = System.Windows.Controls.UserControl;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Application = System.Windows.Application;

namespace Mushub.Views
{
    public partial class PomodoroPage : UserControl
    {
        private DispatcherTimer _timer = new();
        private TimeSpan _remaining;
        private TimeSpan _total;
        private bool _isRunning;
        private int _sessionCount = 0;
        private PomodoroPhase _currentPhase = PomodoroPhase.Work;

        private int _workMinutes = 25;
        private int _shortBreakMinutes = 5;
        private int _longBreakMinutes = 15;

        private const double ArcRadius = 120;
        private const double ArcCenterX = 120;
        private const double ArcCenterY = 120;

        public PomodoroPage()
        {
            InitializeComponent();
            Loaded += PomodoroPage_Loaded;
        }

        private void PomodoroPage_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            ResetToPhase(_currentPhase);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remaining <= TimeSpan.Zero)
            {
                OnPhaseComplete();
                return;
            }
            _remaining -= TimeSpan.FromSeconds(1);
            UpdateDisplay();
        }

        private void OnPhaseComplete()
        {
            _timer.Stop();
            _isRunning = false;
            StartStopText.Text = "▶ Start";

            if (_currentPhase == PomodoroPhase.Work)
            {
                _sessionCount++;
                UpdateSessionDots();
                _currentPhase = (_sessionCount % 4 == 0) ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
                PlayNotificationBeep(false);
            }
            else
            {
                _currentPhase = PomodoroPhase.Work;
                PlayNotificationBeep(true);
            }

            ResetToPhase(_currentPhase);
            StartTimer();
        }

        private void PlayNotificationBeep(bool isWorkStart)
        {
            try
            {
                if (isWorkStart)
                    System.Media.SystemSounds.Exclamation.Play();
                else
                    System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }
        }

        private void ResetToPhase(PomodoroPhase phase)
        {
            _currentPhase = phase;
            _total = phase switch
            {
                PomodoroPhase.ShortBreak => TimeSpan.FromMinutes(_shortBreakMinutes),
                PomodoroPhase.LongBreak => TimeSpan.FromMinutes(_longBreakMinutes),
                _ => TimeSpan.FromMinutes(_workMinutes)
            };
            _remaining = _total;
            UpdateDisplay();
            UpdatePhaseUI();
        }

        private void UpdatePhaseUI()
        {
            switch (_currentPhase)
            {
                case PomodoroPhase.Work:
                    SessionLabel.Text = "SESSION DE TRAVAIL";
                    TimerPhaseText.Text = "Travail";
                    SessionLabel.Foreground = (Brush)Application.Current.FindResource("AccentLightBrush");
                    ProgressArc.Stroke = (Brush)Application.Current.FindResource("AccentGradientBrush");
                    break;
                case PomodoroPhase.ShortBreak:
                    SessionLabel.Text = "PAUSE COURTE ☕";
                    TimerPhaseText.Text = "Pause";
                    SessionLabel.Foreground = (Brush)Application.Current.FindResource("SuccessBrush");
                    ProgressArc.Stroke = (Brush)Application.Current.FindResource("SuccessBrush");
                    break;
                case PomodoroPhase.LongBreak:
                    SessionLabel.Text = "GRANDE PAUSE 🛌";
                    TimerPhaseText.Text = "Grande Pause";
                    SessionLabel.Foreground = (Brush)Application.Current.FindResource("WarningBrush");
                    ProgressArc.Stroke = (Brush)Application.Current.FindResource("WarningBrush");
                    break;
            }
            SessionCountText.Text = $"Session {(_sessionCount % 4) + 1}/4";
        }

        private void UpdateDisplay()
        {
            TimerDisplay.Text = $"{(int)_remaining.TotalMinutes:D2}:{_remaining.Seconds:D2}";
            DrawArc();
        }

        private void DrawArc()
        {
            double progress = _total.TotalSeconds > 0
                ? 1.0 - (_remaining.TotalSeconds / _total.TotalSeconds)
                : 0;

            double angle = progress * 360;
            if (angle >= 360) angle = 359.9999;

            double startAngle = -90;
            double endAngle = startAngle + angle;

            double startRad = startAngle * Math.PI / 180;
            double endRad = endAngle * Math.PI / 180;

            var startPoint = new Point(
                ArcCenterX + ArcRadius * Math.Cos(startRad),
                ArcCenterY + ArcRadius * Math.Sin(startRad));

            var endPoint = new Point(
                ArcCenterX + ArcRadius * Math.Cos(endRad),
                ArcCenterY + ArcRadius * Math.Sin(endRad));

            bool isLargeArc = angle > 180;

            var arcSegment = new ArcSegment(
                endPoint,
                new Size(ArcRadius, ArcRadius),
                0, isLargeArc,
                SweepDirection.Clockwise,
                true);

            var pathFig = new PathFigure { StartPoint = startPoint, IsClosed = false };
            pathFig.Segments.Add(arcSegment);

            ArcGeometry.Figures.Clear();
            ArcGeometry.Figures.Add(pathFig);

            PulseDot.SetValue(System.Windows.Controls.Canvas.LeftProperty, endPoint.X + 10 - 6);
            PulseDot.SetValue(System.Windows.Controls.Canvas.TopProperty, endPoint.Y + 10 - 6);
        }

        private void UpdateSessionDots()
        {
            var dots = new[] { Dot1, Dot2, Dot3, Dot4 };
            var accentBrush = (Brush)Application.Current.FindResource("AccentBrush");
            var borderBrush = (Brush)Application.Current.FindResource("BorderBrush2");

            for (int i = 0; i < dots.Length; i++)
                dots[i].Fill = i < (_sessionCount % 4 == 0 && _sessionCount > 0 ? 4 : _sessionCount % 4)
                    ? accentBrush : borderBrush;

            if (_sessionCount % 4 == 0 && _sessionCount > 0)
                foreach (var d in dots) d.Fill = borderBrush;
        }

        private void StartTimer()
        {
            _timer.Start();
            _isRunning = true;
            StartStopText.Text = "⏸ Pause";
        }

        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                _isRunning = false;
                StartStopText.Text = "▶ Start";
            }
            else
            {
                StartTimer();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            StartStopText.Text = "▶ Start";
            _sessionCount = 0;
            _currentPhase = PomodoroPhase.Work;
            UpdateSessionDots();
            ResetToPhase(PomodoroPhase.Work);
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            StartStopText.Text = "▶ Start";

            if (_currentPhase == PomodoroPhase.Work)
            {
                _sessionCount++;
                UpdateSessionDots();
                _currentPhase = (_sessionCount % 4 == 0) ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
            }
            else
            {
                _currentPhase = PomodoroPhase.Work;
            }
            ResetToPhase(_currentPhase);
        }

        private void WorkSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _workMinutes = (int)e.NewValue;
            if (WorkLabel != null) WorkLabel.Text = $"{_workMinutes} min";
            if (!_isRunning && _currentPhase == PomodoroPhase.Work) ResetToPhase(PomodoroPhase.Work);
        }

        private void ShortBreakSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _shortBreakMinutes = (int)e.NewValue;
            if (ShortBreakLabel != null) ShortBreakLabel.Text = $"{_shortBreakMinutes} min";
            if (!_isRunning && _currentPhase == PomodoroPhase.ShortBreak) ResetToPhase(PomodoroPhase.ShortBreak);
        }

        private void LongBreakSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _longBreakMinutes = (int)e.NewValue;
            if (LongBreakLabel != null) LongBreakLabel.Text = $"{_longBreakMinutes} min";
            if (!_isRunning && _currentPhase == PomodoroPhase.LongBreak) ResetToPhase(PomodoroPhase.LongBreak);
        }
    }

    public enum PomodoroPhase { Work, ShortBreak, LongBreak }
}
