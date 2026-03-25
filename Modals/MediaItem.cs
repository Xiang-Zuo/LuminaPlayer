using System.IO;

namespace LuminaPlayer.Models
{
    public enum MediaType { Image, Video }

    public class MediaItem
    {
        public string FilePath { get; }
        public MediaType Type { get; }
        public string FileName => Path.GetFileName(FilePath);
        public string TypeIcon => Type == MediaType.Image ? "IMG" : "VID";

        public MediaItem(string filePath, MediaType type)
        {
            FilePath = filePath;
            Type = type;
        }
    }
}
