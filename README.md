# LuminaPlayer

**LuminaPlayer** is a minimalist, high-performance media slideshow application for Windows built with **.NET 8 WPF**. It is designed for a clean "cinema" experience with a focus on ease of use, hardware-accelerated playback, and intuitive "pro-player" navigation.

## 🚀 Features

- **Smart Shuffling:** Uses the Fisher-Yates algorithm to randomize your media every time you load a folder.
- **Adaptive Controls:** Left/Right arrows seek through videos OR skip items depending on whether the video is paused.
- **Spotify-Style "Back":** Pressing Left within the first 5 seconds of a video restarts it; double-clicking Left skips to the previous item.
- **Interactive UI:** A floating, clickable progress bar allows for precise video seeking.
- **Cinema Mode:** The mouse cursor and UI elements automatically hide during inactive playback (2-second idle timer).
- **Multi-Format Support:** - **Images:** `.jpg`, `.jpeg`, `.png`, `.webp`, `.bmp`
  - **Videos:** `.mp4`, `.mkv`, `.webm`, `.mov`

---

## 🛠️ Build and Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10 or 11

### 1. Build the Project
Open your terminal in the project folder and run:
```powershell
dotnet build
```

### 2. Run the App
```powershell
dotnet run
```

*Note: The app will prompt you to select a media folder upon launch.*


### 3. Export to a Portable `.exe`
To package the app into a single, shareable file:
```powershell
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
The output will be located in:
`bin\Release\net8.0-windows\win-x64\publish\LuminaPlayer.exe`

#### 🎮 How to Use
Keyboard Shortcuts
|Key	|Action|
|--------|-------|
|Space	|Play / Pause|
|Right Arrow	|Seek <b>+5s</b> (if playing) or Next Item|
|Left Arrow	|Seek <b>-5s</b> (if playing) or <b>Restart / Previous</b>|
|Escape	|Exit Application|

Mouse Actions
- Click Center: Toggles Play/Pause.

- Click Progress Bar: Jumps to that specific time in the video.

- Move Mouse: Shows the cursor and progress bar (auto-hides after 2 seconds).

#### 📝 License
This project is licensed under the MIT License.