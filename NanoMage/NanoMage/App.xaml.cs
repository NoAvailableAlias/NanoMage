using System.Windows;

namespace NanoMage
{
    /// <summary>
    /// Main class for handling application core behaviors.
    /// </summary>
    public partial class App : Application
    {
        private static readonly MainWindow moMainWindow = new MainWindow();

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args?.Length == 1)
            {
                // Start single file + folder mode
                await moMainWindow.moImageController.LoadImageAsync(e.Args[0]);
            }
            else if (e.Args?.Length > 1)
            {
                // Start file selection mode
                await moMainWindow.moImageController.LoadImagesAsync(e.Args);
            }
            moMainWindow.Show();
        }
    }
}
