using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using NanoMage.Core;

namespace NanoMage
{
    /// <summary>
    /// Main class for handling application WPF interactions.
    /// </summary>
    public partial class MainWindow : Window
    {
        public ImageController moImageController { get; set; }

        //----------------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            moImageController = new ImageController(this);

            RenderOptions.SetBitmapScalingMode(ImageControl, BitmapScalingMode.Fant);
        }

        //----------------------------------------------------------------------

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Back:
                    await moImageController.LoadPreviousAsync();
                    break;
                case Key.Escape:
                    Close();
                    break;
                case Key.Space:
                    await moImageController.LoadNextAsync();
                    break;
                case Key.OemPlus:
                    ImageBorder.ZoomIn();
                    break;
                case Key.OemMinus:
                    ImageBorder.ZoomOut();
                    break;
            }
        }

        private async void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1:
                    await moImageController.LoadPreviousAsync();
                    break;
                case MouseButton.XButton2:
                    await moImageController.LoadNextAsync();
                    break;
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        // Instead of moving the ZoomBorder on left click drag,
                        // Now when ctrl is held, left click moves the window
                        DragMove();
                    }
                    break;
            }
        }

        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Fix the windowchrome border gap issue
                var toBorderThickness = SystemParameters.WindowResizeBorderThickness;
                MaxWidth = SystemParameters.PrimaryScreenWidth + toBorderThickness.Right + 3;
                MaxHeight = SystemParameters.PrimaryScreenHeight + toBorderThickness.Bottom + 3;
            }
            else
            {
                MaxWidth = SystemParameters.PrimaryScreenWidth;
                MaxHeight = SystemParameters.PrimaryScreenHeight;
            }
        }

        //----------------------------------------------------------------------

        private void TitleBarCopy_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(Title);
        }

        private void TitleBtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TitleBtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Maximized)
                ? WindowState.Normal : WindowState.Maximized;
        }

        private void TitleBtnDestruct_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //----------------------------------------------------------------------

        private void TitleBar_MouseEnter(object sender, MouseEventArgs e)
        {
            TitleBar.Height = TitleBtnMinimize.Width;
            TitleBarLabel.Opacity = 1.0;
        }

        private void TitleBar_MouseLeave(object sender, MouseEventArgs e)
        {
            TitleBar.Height = TitleBtnMinimize.Width / 3;
            TitleBarLabel.Opacity = 0.0;
        }

        //----------------------------------------------------------------------

        private async void ImageGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var toFilePaths = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (toFilePaths?.Length == 1)
                {
                    // Start single file + folder mode
                    await moImageController.LoadImageAsync(toFilePaths[0]);
                }
                else
                {
                    // Start file selection mode
                    await moImageController.LoadImagesAsync(toFilePaths);
                }
            }
        }
    }
}
