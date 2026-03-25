using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LuminaPlayer.Models;
using Microsoft.VisualBasic.FileIO;

namespace LuminaPlayer
{
    public enum SourceTypeFilter { All, ImagesOnly, VideosOnly }

    public partial class MainWindow : Window
    {
        private readonly string _folderPath;
        private List<MediaItem> _playlist = new();
        private int _currentIndex = -1;
        private bool _isPaused = false;
        private readonly Random _rng = new();

        // Settings
        private bool _isRandom = true;
        private bool _includeSubfolders = true;
        private double _imageDuration = 5.0;
        private SourceTypeFilter _sourceType = SourceTypeFilter.All;

        // Timers
        private readonly DispatcherTimer _progressTimer;
        private double _secondsElapsed = 0;
        private readonly DispatcherTimer _cursorTimer;
        private readonly DispatcherTimer _deleteIndicatorTimer;

        // State
        private DateTime _lastLeftClickTime = DateTime.MinValue;
        private bool _isUpdatingSelection = false;
        private bool _wasPausedBeforeConfig = false;
        private const double VideoDoublePressThreshold = 5.0;

        public MainWindow(string folderPath)
        {
            InitializeComponent();

            _folderPath = folderPath;

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _progressTimer.Tick += ProgressTimer_Tick;

            _cursorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _cursorTimer.Tick += (s, e) => {
                // Only hide cursor when no panels are open
                if (ConfigOverlay.Visibility != Visibility.Visible && !_playlistOpen)
                    this.Cursor = Cursors.None;
                _cursorTimer.Stop();
            };

            _deleteIndicatorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _deleteIndicatorTimer.Tick += (s, e) => {
                DeleteIndicator.Visibility = Visibility.Collapsed;
                _deleteIndicatorTimer.Stop();
            };

            LoadPlaylist();

            if (_playlist.Any()) ShowNext();
            else
            {
                MessageBox.Show("No media found.");
                this.Loaded += (s, e) => this.Close();
            }
        }

        #region Playlist Loading

        private void LoadPlaylist()
        {
            var imgExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };
            var vidExt = new[] { ".mp4", ".mkv", ".webm", ".mov" };

            try
            {
                var searchOption = _includeSubfolders
                    ? System.IO.SearchOption.AllDirectories
                    : System.IO.SearchOption.TopDirectoryOnly;

                var files = Directory.EnumerateFiles(_folderPath, "*.*", searchOption)
                    .Select(f => new { Path = f, Ext = Path.GetExtension(f).ToLower() })
                    .Where(f => imgExt.Contains(f.Ext) || vidExt.Contains(f.Ext))
                    .Select(f => new MediaItem(f.Path, imgExt.Contains(f.Ext) ? MediaType.Image : MediaType.Video))
                    .Where(f => _sourceType == SourceTypeFilter.All
                        || (_sourceType == SourceTypeFilter.ImagesOnly && f.Type == MediaType.Image)
                        || (_sourceType == SourceTypeFilter.VideosOnly && f.Type == MediaType.Video))
                    .ToList();

                if (_isRandom)
                {
                    for (int n = files.Count - 1; n > 0; n--)
                    {
                        int k = _rng.Next(n + 1);
                        (files[k], files[n]) = (files[n], files[k]);
                    }
                }
                else
                {
                    files = files.OrderBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
                }

                _playlist = files;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            RefreshPlaylistPanel();
        }

        private void RefreshPlaylistPanel()
        {
            _isUpdatingSelection = true;
            PlaylistListBox.ItemsSource = null;
            PlaylistListBox.ItemsSource = _playlist;
            PlaylistCountText.Text = $"{_playlist.Count} items";
            if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                PlaylistListBox.SelectedIndex = _currentIndex;
            _isUpdatingSelection = false;
        }

        #endregion

        #region Media Display

        private void ShowEmptyState()
        {
            _progressTimer.Stop();
            VideoPlayer.Stop();
            VideoPlayer.Visibility = Visibility.Collapsed;
            ImageViewer.Visibility = Visibility.Collapsed;
            PlayProgressBar.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Visible;
        }

        private void HideEmptyState()
        {
            EmptyStateText.Visibility = Visibility.Collapsed;
        }

        private void DisplayMedia(MediaItem item)
        {
            HideEmptyState();
            _progressTimer.Stop();
            _secondsElapsed = 0;
            PlayProgressBar.Value = 0;
            VideoPlayer.Stop();
            VideoPlayer.Visibility = Visibility.Collapsed;
            ImageViewer.Visibility = Visibility.Collapsed;
            PlayProgressBar.Visibility = Visibility.Visible;

            if (item.Type == MediaType.Image)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.FilePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                ImageViewer.Source = bitmap;
                ImageViewer.Visibility = Visibility.Visible;
                if (!_isPaused) _progressTimer.Start();
            }
            else
            {
                VideoPlayer.Source = new Uri(item.FilePath);
                VideoPlayer.Visibility = Visibility.Visible;
                VideoPlayer.Play();
                _progressTimer.Start();
                if (_isPaused) VideoPlayer.Pause();
            }

            SyncPlaylistSelection();
        }

        private void SyncPlaylistSelection()
        {
            _isUpdatingSelection = true;
            PlaylistListBox.SelectedIndex = _currentIndex;
            PlaylistListBox.ScrollIntoView(PlaylistListBox.SelectedItem);
            _isUpdatingSelection = false;
        }

        private void ShowNext()
        {
            if (_playlist.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _playlist.Count;
            DisplayMedia(_playlist[_currentIndex]);
        }

        private void ShowPrevious()
        {
            if (_playlist.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _playlist.Count) % _playlist.Count;
            DisplayMedia(_playlist[_currentIndex]);
        }

        private void NavigateTo(int index)
        {
            if (index < 0 || index >= _playlist.Count) return;
            _currentIndex = index;
            DisplayMedia(_playlist[_currentIndex]);
        }

        #endregion

        #region Progress & Timers

        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _playlist.Count) return;

            if (_playlist[_currentIndex].Type == MediaType.Image)
            {
                _secondsElapsed += 0.05;
                PlayProgressBar.Value = (_secondsElapsed / _imageDuration) * 100;
                if (_secondsElapsed >= _imageDuration) ShowNext();
            }
            else if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                PlayProgressBar.Value = (VideoPlayer.Position.TotalSeconds / VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds) * 100;
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e) => ShowNext();

        #endregion

        #region Pause / Resume

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            if (_isPaused)
            {
                _progressTimer.Stop();
                if (_playlist[_currentIndex].Type == MediaType.Video) VideoPlayer.Pause();
            }
            else
            {
                _progressTimer.Start();
                if (_playlist[_currentIndex].Type == MediaType.Video) VideoPlayer.Play();
            }
        }

        #endregion

        #region Delete (Move to Trash)

        private void DeleteCurrentMedia()
        {
            if (_currentIndex < 0 || _currentIndex >= _playlist.Count) return;

            var item = _playlist[_currentIndex];

            // Stop playback if video
            if (item.Type == MediaType.Video) VideoPlayer.Stop();

            try
            {
                FileSystem.DeleteFile(item.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not move to trash:\n{ex.Message}");
                return;
            }

            _playlist.RemoveAt(_currentIndex);

            // Show delete indicator
            _deleteIndicatorTimer.Stop();
            DeleteIndicator.Visibility = Visibility.Visible;
            _deleteIndicatorTimer.Start();

            if (_playlist.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            // Adjust index so we show the next item (which slid into current position)
            if (_currentIndex >= _playlist.Count)
                _currentIndex = 0;

            RefreshPlaylistPanel();
            DisplayMedia(_playlist[_currentIndex]);
        }

        #endregion

        #region Config Panel

        private void OpenConfig()
        {
            // Pause playback while config is open
            _wasPausedBeforeConfig = _isPaused;
            if (!_isPaused) TogglePause();

            // Sync UI to current settings
            RandomOrderRadio.IsChecked = _isRandom;
            OriginalOrderRadio.IsChecked = !_isRandom;
            SubfoldersCheckBox.IsChecked = _includeSubfolders;
            SourceAllRadio.IsChecked = _sourceType == SourceTypeFilter.All;
            SourceImageRadio.IsChecked = _sourceType == SourceTypeFilter.ImagesOnly;
            SourceVideoRadio.IsChecked = _sourceType == SourceTypeFilter.VideosOnly;
            DurationSlider.Value = _imageDuration;
            DurationValueText.Text = _imageDuration.ToString("0");

            ConfigOverlay.Visibility = Visibility.Visible;
            this.Cursor = Cursors.Arrow;
        }

        private void CloseConfig()
        {
            ConfigOverlay.Visibility = Visibility.Collapsed;
            _cursorTimer.Stop();

            // Only hide cursor if playlist panel is also closed
            if (!_playlistOpen)
                this.Cursor = Cursors.None;

            // Restore pause state
            if (!_wasPausedBeforeConfig && _isPaused) TogglePause();
        }

        private void ConfigApply_Click(object sender, RoutedEventArgs e)
        {
            bool newRandom = RandomOrderRadio.IsChecked == true;
            bool newSubfolders = SubfoldersCheckBox.IsChecked == true;
            double newDuration = DurationSlider.Value;
            SourceTypeFilter newSourceType = SourceAllRadio.IsChecked == true ? SourceTypeFilter.All
                : SourceImageRadio.IsChecked == true ? SourceTypeFilter.ImagesOnly
                : SourceTypeFilter.VideosOnly;

            bool orderChanged = newRandom != _isRandom;
            bool subfoldersChanged = newSubfolders != _includeSubfolders;
            bool durationChanged = Math.Abs(newDuration - _imageDuration) > 0.01;
            bool sourceChanged = newSourceType != _sourceType;

            // If nothing changed, just close
            if (!orderChanged && !subfoldersChanged && !durationChanged && !sourceChanged)
            {
                CloseConfig();
                return;
            }

            // Duration-only change: no reload needed
            if (durationChanged && !orderChanged && !subfoldersChanged && !sourceChanged)
            {
                _imageDuration = newDuration;
                CloseConfig();
                return;
            }

            // Remember current file to try to restore position
            string? currentFile = (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                ? _playlist[_currentIndex].FilePath : null;

            _isRandom = newRandom;
            _includeSubfolders = newSubfolders;
            _imageDuration = newDuration;
            _sourceType = newSourceType;

            // Reload playlist with new settings
            _currentIndex = -1;
            LoadPlaylist();

            // Close config overlay WITHOUT restoring pause state —
            // we're about to start fresh playback anyway
            ConfigOverlay.Visibility = Visibility.Collapsed;
            _cursorTimer.Stop();
            if (!_playlistOpen)
                this.Cursor = Cursors.None;

            // Reset pause so new playback starts fresh
            _isPaused = false;

            // If new playlist is empty, show empty state
            if (!_playlist.Any())
            {
                ShowEmptyState();
                return;
            }

            HideEmptyState();

            // Try to find the previously playing file in the new playlist
            if (currentFile != null)
            {
                int idx = _playlist.FindIndex(m => m.FilePath == currentFile);
                if (idx >= 0)
                {
                    _currentIndex = idx;
                    DisplayMedia(_playlist[_currentIndex]);
                    return;
                }
            }

            // Current file not in new playlist (e.g. subfolder item after disabling subfolders)
            // Start from the beginning
            ShowNext();
        }

        private void ConfigClose_Click(object sender, RoutedEventArgs e) => CloseConfig();

        private void ConfigOverlayBg_Click(object sender, MouseButtonEventArgs e) => CloseConfig();

        private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DurationValueText != null)
                DurationValueText.Text = e.NewValue.ToString("0");
        }

        #endregion

        #region Playlist Panel

        private bool _playlistOpen = false;

        private void TogglePlaylistPanel()
        {
            _playlistOpen = !_playlistOpen;

            if (_playlistOpen)
            {
                PlaylistPanel.Visibility = Visibility.Visible;
                PlaylistToggleButton.Content = "\u25B6"; // right arrow = close
                this.Cursor = Cursors.Arrow;
                SyncPlaylistSelection();
            }
            else
            {
                PlaylistPanel.Visibility = Visibility.Collapsed;
                PlaylistToggleButton.Content = "\u25C0"; // left arrow = open
                if (ConfigOverlay.Visibility != Visibility.Visible)
                    this.Cursor = Cursors.None;
            }
        }

        private void PlaylistToggleButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlaylistPanel();
        }

        private void PlaylistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;
            if (PlaylistListBox.SelectedIndex >= 0 && PlaylistListBox.SelectedIndex != _currentIndex)
            {
                NavigateTo(PlaylistListBox.SelectedIndex);
            }
        }

        #endregion

        #region Input Handling

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Config panel toggle (always available)
            if (e.Key == Key.F2)
            {
                if (ConfigOverlay.Visibility == Visibility.Visible)
                    CloseConfig();
                else
                    OpenConfig();
                return;
            }

            // Escape: close config first, then playlist, then app
            if (e.Key == Key.Escape)
            {
                if (ConfigOverlay.Visibility == Visibility.Visible)
                    CloseConfig();
                else if (_playlistOpen)
                    TogglePlaylistPanel();
                else
                    this.Close();
                return;
            }

            // Block other media keys while config is open
            if (ConfigOverlay.Visibility == Visibility.Visible) return;

            // Playlist panel toggle
            if (e.Key == Key.Tab)
            {
                TogglePlaylistPanel();
                e.Handled = true;
                return;
            }

            if (_currentIndex < 0 || _currentIndex >= _playlist.Count) return;
            var current = _playlist[_currentIndex];

            switch (e.Key)
            {
                case Key.Space:
                    TogglePause();
                    break;

                case Key.Right:
                    if (current.Type == MediaType.Video && !_isPaused)
                        VideoPlayer.Position += TimeSpan.FromSeconds(5);
                    else
                        ShowNext();
                    break;

                case Key.Left:
                    if (current.Type == MediaType.Video && !_isPaused)
                    {
                        if (VideoPlayer.Position.TotalSeconds < VideoDoublePressThreshold)
                        {
                            TimeSpan timeSinceLastClick = DateTime.Now - _lastLeftClickTime;
                            if (timeSinceLastClick.TotalMilliseconds < 1000)
                                ShowPrevious();
                            else
                                VideoPlayer.Position = TimeSpan.Zero;
                        }
                        else
                        {
                            VideoPlayer.Position -= TimeSpan.FromSeconds(5);
                        }
                        _lastLeftClickTime = DateTime.Now;
                    }
                    else
                    {
                        ShowPrevious();
                    }
                    break;

                case Key.Delete:
                    DeleteCurrentMedia();
                    break;
            }
        }

        private void PlayProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_playlist[_currentIndex].Type != MediaType.Video) return;

            double mouseX = e.GetPosition(PlayProgressBar).X;
            double ratio = mouseX / PlayProgressBar.ActualWidth;

            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                double newSeconds = ratio * VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                VideoPlayer.Position = TimeSpan.FromSeconds(newSeconds);
                PlayProgressBar.Value = ratio * 100;
            }

            e.Handled = true;
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            _cursorTimer.Stop();
            _cursorTimer.Start();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ConfigOverlay.Visibility == Visibility.Visible) return;
            if (e.OriginalSource == PlayProgressBar) return;

            TogglePause();
        }

        #endregion
    }
}
