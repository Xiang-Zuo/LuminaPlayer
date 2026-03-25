using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LuminaPlayer.Models
{
    public enum MediaType { Image, Video }

    public class MediaItem : INotifyPropertyChanged
    {
        public string FilePath { get; }
        public MediaType Type { get; }
        public string FileName => Path.GetFileName(FilePath);
        public string RelativePath { get; }
        public string DisplayLabel => string.IsNullOrEmpty(RelativePath)
            ? FileName
            : $"{FileName}  —  {RelativePath}";
        public bool IsVideo => Type == MediaType.Video;

        private BitmapSource? _thumbnail;
        private bool _thumbnailLoading;

        public BitmapSource? Thumbnail
        {
            get
            {
                if (_thumbnail == null && !_thumbnailLoading && Type == MediaType.Image)
                {
                    _thumbnailLoading = true;
                    Task.Run(() => LoadThumbnailAsync());
                }
                return _thumbnail;
            }
        }

        private void LoadThumbnailAsync()
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(FilePath);
                bmp.DecodePixelWidth = 250;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze(); // Thread-safe after freeze

                _thumbnail = bmp;
                Application.Current.Dispatcher.BeginInvoke(() =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail))));
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MediaItem(string filePath, MediaType type, string rootFolder)
        {
            FilePath = filePath;
            Type = type;

            var dir = Path.GetDirectoryName(filePath) ?? rootFolder;
            var rel = Path.GetRelativePath(rootFolder, dir);
            RelativePath = (rel == ".") ? "" : rel;
        }
    }
}
