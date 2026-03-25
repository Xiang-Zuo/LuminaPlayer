# LuminaPlayer

**LuminaPlayer** is a minimalist, high-performance media slideshow application for Windows built with **.NET 8 WPF**. It is designed for a clean "cinema" experience with a focus on ease of use, hardware-accelerated playback, and intuitive "pro-player" navigation.

## Features

- **Smart Shuffling:** Uses the Fisher-Yates algorithm to randomize your media every time you load a folder.
- **Adaptive Controls:** Left/Right arrows seek through videos OR skip items depending on whether the video is paused.
- **Spotify-Style "Back":** Pressing Left within the first 5 seconds of a video restarts it; double-pressing Left skips to the previous item.
- **Interactive Progress Bar:** A floating, clickable progress bar allows for precise video seeking.
- **Cinema Mode:** The mouse cursor and UI elements automatically hide during inactive playback (2-second idle timer).
- **Settings Panel (F2):** Configure playback behavior on the fly without restarting:
  - **Play Order** — Random (default) or Original (sorted by filename)
  - **Include Subfolders** — Toggle recursive folder scanning on/off
  - **Source Type Filter** — All, Images Only, or Videos Only
  - **Image Display Duration** — Adjustable from 1 to 30 seconds (default 5s)
- **Playlist Panel (Tab):** A resizable right-side panel to browse and navigate your media:
  - Async-loaded image thumbnails in a 16:9 card layout
  - Compact list mode when in Videos Only filter
  - Click any item to jump directly to it
  - Drag the left edge to resize (200–600px, remembered across toggles)
  - Shows filename and relative path from the root folder
- **Delete to Trash (Delete key):** Move the current media file to the Recycle Bin with a visual indicator. Fully recoverable.
- **Multi-Format Support:**
  - **Images:** `.jpg`, `.jpeg`, `.png`, `.webp`, `.bmp`
  - **Videos:** `.mp4`, `.mkv`, `.webm`, `.mov`

---

## Build and Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or later)
- Windows 10 or 11

### Build
```powershell
dotnet build
```

### Run
```powershell
dotnet run
```
The app will prompt you to select a media folder on launch. You can also pass a folder path as a command-line argument or drag-and-drop a folder onto the exe.

### Publish

**Framework-dependent** (small size, requires .NET 8 runtime on target machine):
```powershell
dotnet publish -c Release
```
Output: `bin\Release\net8.0-windows\publish\LuminaPlayer.exe`

**Self-contained** (no .NET runtime required on target machine):
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```
Output: `bin\Release\net8.0-windows\win-x64\publish\`

**Single file** (one portable exe, no dependencies):
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
Output: `bin\Release\net8.0-windows\win-x64\publish\LuminaPlayer.exe`

---

## How to Use

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Space | Play / Pause |
| Right Arrow | Seek +5s (playing video) or Next Item |
| Left Arrow | Seek -5s (playing video) or Restart / Previous |
| F2 | Open / Close Settings Panel |
| Tab | Open / Close Playlist Panel |
| Delete | Move current media to Recycle Bin |
| Escape | Close Settings Panel (if open), otherwise Exit |

### Mouse Actions

- **Click media area** — Toggle Play / Pause
- **Click progress bar** — Seek to that position in the video
- **Move mouse** — Show cursor and progress bar (auto-hides after 2 seconds)
- **Click playlist toggle arrow** — Open / Close the playlist panel
- **Drag playlist edge** — Resize the playlist panel
- **Click playlist item** — Jump to that media

---

## License

This project is licensed under the [MIT License](LICENSE).

## Privacy & Security

- **Offline First:** LuminaPlayer does not connect to the internet.
- **Zero Telemetry:** No usage data is collected, tracked, or shared.
- **Open Source:** Every line of code is available for audit.
- **Native Tech:** Built using standard Microsoft .NET libraries.
