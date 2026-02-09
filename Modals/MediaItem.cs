namespace LuminaPlayer.Models
{
    public enum MediaType { Image, Video }
    public record MediaItem(string FilePath, MediaType Type);
}