using System.Windows;
using Microsoft.Win32; // Required for OpenFolderDialog

namespace LuminaPlayer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string folderToScan = "";

            // 1. Check if a folder was passed via command line or Drag-and-Drop
            if (e.Args.Length > 0 && System.IO.Directory.Exists(e.Args[0]))
            {
                folderToScan = e.Args[0];
            }
            else
            {
                // 2. Otherwise, prompt the user to pick a folder
                OpenFolderDialog dialog = new OpenFolderDialog
                {
                    Title = "Select Media Folder for Slideshow",
                    InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == true)
                {
                    folderToScan = dialog.FolderName;
                }
            }

            // 3. Start the app if we have a valid path
            if (!string.IsNullOrEmpty(folderToScan) && System.IO.Directory.Exists(folderToScan))
            {
                this.MainWindow = new MainWindow(folderToScan);
                this.MainWindow.Show();
            }
            else
            {
                // If user cancels or folder is invalid, close the app
                this.Shutdown();
            }
        }
    }
}