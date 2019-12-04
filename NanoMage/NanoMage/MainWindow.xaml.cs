using System.Diagnostics;
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

            // "Fix" the windowchrome border gap issue... for now...
            var toBorderThickness = SystemParameters.WindowResizeBorderThickness;
            MaxWidth = SystemParameters.PrimaryScreenWidth + toBorderThickness.Right + 3;
            MaxHeight = SystemParameters.PrimaryScreenHeight + toBorderThickness.Bottom + 3;

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

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                // This is required to play nice with TitleBar_MouseLeftButtonDown
                (Mouse.LeftButton == MouseButtonState.Pressed))
            {
                _mouseWindowStateDragMove(e);
            }
        }

        //----------------------------------------------------------------------

        private void TitleBarCopy_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Process.Start("explorer.exe", "/select, " + moImageController.CurrentPath);
                e.Handled = true;
            }
            else
            {
                Clipboard.SetText(Title);
            }
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
            TitleBar.Margin = new Thickness();
            TitleBar.Height = TitleBtnMinimize.Width;
            TitleBarCopy.Visibility = Visibility.Visible;
        }

        private void TitleBar_MouseLeave(object sender, MouseEventArgs e)
        {
            TitleBar.Margin = new Thickness { Top = 5 };
            TitleBar.Height = 5;
            TitleBarCopy.Visibility = Visibility.Collapsed;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control &&
                // This is required to play nice with Window_PreviewMouseLeftButtonDown
                (Mouse.LeftButton == MouseButtonState.Pressed))
            {
                _mouseWindowStateDragMove(e);
            }
        }

        private void _mouseWindowStateDragMove(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = (WindowState == WindowState.Maximized)
                    ? WindowState.Normal : WindowState.Maximized;
            }
            DragMove();
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
