using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace LuminaPlayer.Models
{
    public enum MediaType { Image, Video }

    public class MediaItem
    {
        public string FilePath { get; }
        public MediaType Type { get; }
        public string FileName => Path.GetFileName(FilePath);
        public bool IsVideo => Type == MediaType.Video;

        private BitmapSource? _thumbnail;
        private bool _thumbnailLoaded;

        public BitmapSource? Thumbnail
        {
            get
            {
                if (!_thumbnailLoaded)
                {
                    _thumbnailLoaded = true;
                    if (Type == MediaType.Image)
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(FilePath);
                            bmp.DecodePixelWidth = 60;
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();
                            _thumbnail = bmp;
                        }
                        catch { }
                    }
                }
                return _thumbnail;
            }
        }

        public MediaItem(string filePath, MediaType type)
        {
            FilePath = filePath;
            Type = type;
        }
    }
}
