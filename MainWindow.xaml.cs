using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LuminaPlayer.Models;

namespace LuminaPlayer
{
    public partial class MainWindow : Window
    {
        private List<MediaItem> _playlist = new();
        private int _currentIndex = -1;
        private bool _isPaused = false;
        private readonly Random _rng = new();

        private DispatcherTimer _progressTimer;
        private double _secondsElapsed = 0;
        private const double ImageDurationSeconds = 5.0;
        private DispatcherTimer _cursorTimer;
        private DateTime _lastLeftClickTime = DateTime.MinValue;

        public MainWindow(string folderPath)
        {
            InitializeComponent();
            
            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _progressTimer.Tick += ProgressTimer_Tick;

            _cursorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _cursorTimer.Tick += (s, e) => { 
                this.Cursor = Cursors.None; 
                _cursorTimer.Stop(); 
            };

            LoadAndShuffle(folderPath);

            if (_playlist.Any()) ShowNext();
            else {
                MessageBox.Show("No media found.");
                this.Loaded += (s, e) => this.Close();
            }
        }

        private void LoadAndShuffle(string path)
        {
            var imgExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };
            var vidExt = new[] { ".mp4", ".mkv", ".webm", ".mov" };

            try {
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Select(f => new { Path = f, Ext = Path.GetExtension(f).ToLower() })
                    .Where(f => imgExt.Contains(f.Ext) || vidExt.Contains(f.Ext))
                    .Select(f => new MediaItem(f.Path, imgExt.Contains(f.Ext) ? MediaType.Image : MediaType.Video))
                    .ToList();

                for (int n = files.Count - 1; n > 0; n--) {
                    int k = _rng.Next(n + 1);
                    (files[k], files[n]) = (files[n], files[k]);
                }
                _playlist = files;
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_playlist[_currentIndex].Type == MediaType.Image) {
                _secondsElapsed += 0.05;
                PlayProgressBar.Value = (_secondsElapsed / ImageDurationSeconds) * 100;
                if (_secondsElapsed >= ImageDurationSeconds) ShowNext();
            } else if (VideoPlayer.NaturalDuration.HasTimeSpan) {
                PlayProgressBar.Value = (VideoPlayer.Position.TotalSeconds / VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds) * 100;
            }
        }

        private void PlayProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_playlist[_currentIndex].Type != MediaType.Video) return;
            
            double mouseX = e.GetPosition(PlayProgressBar).X;
            double ratio = mouseX / PlayProgressBar.ActualWidth;
            
            if (VideoPlayer.NaturalDuration.HasTimeSpan) {
                double newSeconds = ratio * VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                VideoPlayer.Position = TimeSpan.FromSeconds(newSeconds);
                PlayProgressBar.Value = ratio * 100;
            }

            // IMPORTANT: Tell Windows we handled this click so the Grid doesn't see it
            e.Handled = true;
        }

        private void DisplayMedia(MediaItem item)
        {
            _progressTimer.Stop();
            _secondsElapsed = 0;
            PlayProgressBar.Value = 0;
            VideoPlayer.Stop();
            VideoPlayer.Visibility = Visibility.Collapsed;
            ImageViewer.Visibility = Visibility.Collapsed;
            PlayProgressBar.Visibility = Visibility.Visible;

            if (item.Type == MediaType.Image) {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.FilePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                ImageViewer.Source = bitmap;
                ImageViewer.Visibility = Visibility.Visible;
                if (!_isPaused) _progressTimer.Start();
            } else {
                VideoPlayer.Source = new Uri(item.FilePath);
                VideoPlayer.Visibility = Visibility.Visible;
                VideoPlayer.Play();
                _progressTimer.Start();
                if (_isPaused) VideoPlayer.Pause();
            }
        }

        private void ShowNext() {
            _currentIndex = (_currentIndex + 1) % _playlist.Count;
            DisplayMedia(_playlist[_currentIndex]);
        }

        private void ShowPrevious() {
            _currentIndex = (_currentIndex - 1 + _playlist.Count) % _playlist.Count;
            DisplayMedia(_playlist[_currentIndex]);
        }

        private void TogglePause() {
            _isPaused = !_isPaused;
            if (_isPaused) {
                _progressTimer.Stop();
                if (_playlist[_currentIndex].Type == MediaType.Video) VideoPlayer.Pause();
            } else {
                _progressTimer.Start();
                if (_playlist[_currentIndex].Type == MediaType.Video) VideoPlayer.Play();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var current = _playlist[_currentIndex];

            switch (e.Key) {
                case Key.Space: TogglePause(); break;
                case Key.Right:
                    // Seek if video is playing, skip if image OR paused video
                    if (current.Type == MediaType.Video && !_isPaused)
                        VideoPlayer.Position += TimeSpan.FromSeconds(5);
                    else 
                        ShowNext();
                    break;
                case Key.Left:
                    if (current.Type == MediaType.Video && !_isPaused)
                    {
                        // Check if we are at the very beginning of the video (less than 5s)
                        if (VideoPlayer.Position.TotalSeconds < 5)
                        {
                            TimeSpan timeSinceLastClick = DateTime.Now - _lastLeftClickTime;

                            // If the last click was less than 1 second ago
                            if (timeSinceLastClick.TotalMilliseconds < 1000)
                            {
                                ShowPrevious(); // Move to previous file
                            }
                            else
                            {
                                VideoPlayer.Position = TimeSpan.Zero; // Restart current video
                            }
                        }
                        else
                        {
                            // Normal seek backward
                            VideoPlayer.Position -= TimeSpan.FromSeconds(5);
                        }
                        
                        // Update the timestamp for the double-click check
                        _lastLeftClickTime = DateTime.Now;
                    }
                    else 
                    {
                        ShowPrevious(); // Image or Paused Video always goes back
                    }
                    break;
                case Key.Escape: this.Close(); break;
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e) => ShowNext();

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow; // Show cursor when moving
            _cursorTimer.Stop();         // Reset the "hide" countdown
            _cursorTimer.Start();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if the user clicked the progress bar specifically
            // We use VisualTreeHelper or OriginalSource to ensure we don't 
            // toggle pause when just trying to seek.
            if (e.OriginalSource == PlayProgressBar) 
            {
                return; 
            }

            // If they clicked the background or the media, toggle pause
            TogglePause();

            // Visual feedback: brief flicker or logic to show it worked
            // (Optional: you could show a play/pause icon here)
        }
    }
}