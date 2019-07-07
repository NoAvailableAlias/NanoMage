using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NanoMage.Core
{
    /// <summary>
    /// Main class for handling image browsing.
    /// </summary>
    public class ImageController
    {
        // GIF maybe in the future, for now they are stills
        // These are the only extensions that NanoMage cares about
        private static readonly Regex VALID_EXTENSIONS_REGEX = new Regex(
            @"^.+\.(?i)(gif|png|jpg|jpeg)(?-i)$", RegexOptions.Compiled);

        private MainWindow moMainWindow { get; set; }

        private object[] moImageBytes { get; set; }

        private string[] moImagePaths { get; set; }

        private bool mbIsInitialImage { get; set; }

        private int miCurrentSeek { get; set; }

        public ImageController(MainWindow poMainWindow)
        {
            moMainWindow = poMainWindow;
            mbIsInitialImage = true;
        }

        //----------------------------------------------------------------------

        public async Task LoadImagesAsync(string[] poFilePaths, string psFirst = "")
        {
            moImagePaths = poFilePaths
                .Where(p => VALID_EXTENSIONS_REGEX.IsMatch(p))
                .OrderBy(p => p)
                .ToArray();

            moImageBytes = new object[moImagePaths.Length];
            miCurrentSeek = Array.BinarySearch(moImagePaths, psFirst);

            await _seekToImageAsync(miCurrentSeek);
        }

        public async Task LoadImageAsync(string psFilePath)
        {
            try
            {
                await LoadImagesAsync(Directory.GetFiles(Path.GetDirectoryName(
                    psFilePath), "*.*", SearchOption.TopDirectoryOnly), psFilePath);
            }
            catch (Exception toException)
            {
                // Maybe an issue with the provided file path?
                System.Diagnostics.Debug.WriteLine(toException);
            }
        }

        public async Task LoadPreviousAsync()
        {
            if (moImagePaths != null)
            {
                await _seekToImageAsync(--miCurrentSeek);
            }
        }

        public async Task LoadNextAsync()
        {
            if (moImagePaths != null)
            {
                await _seekToImageAsync(++miCurrentSeek);
            }
        }

        //----------------------------------------------------------------------

        private async Task _seekToImageAsync(int piCurrentSeek)
        {
            int _localWrapIndex(int piSeek)
            {
                var tiCount = moImagePaths.Length;
                return ((piSeek % tiCount) + tiCount) % tiCount;
            }

            var tiCurrentIndex = _localWrapIndex(piCurrentSeek);

            if (moImageBytes[tiCurrentIndex] == null)
            {
                await _loadImageAsync(piCurrentSeek, tiCurrentIndex);
            }
            _renderCurrentSeek(piCurrentSeek, tiCurrentIndex);

            var tiPreviousSeek = piCurrentSeek - 1;
            var tiPreviousIndex = _localWrapIndex(tiPreviousSeek);

            var tiNextSeek = piCurrentSeek + 1;
            var tiNextIndex = _localWrapIndex(tiNextSeek);

            if (tiPreviousIndex != tiNextIndex)
            {
                if (moImageBytes[tiPreviousIndex] == null)
                {
                    await _loadImageAsync(tiPreviousSeek, tiPreviousIndex);
                }
                if (moImageBytes[tiNextIndex] == null)
                {
                    await _loadImageAsync(tiNextSeek, tiNextIndex);
                }
            }
        }

        private async Task _loadImageAsync(int piCurrentSeek, int piCurrentIndex)
        {
            var tsCurrentPath = moImagePaths[piCurrentIndex];
            var toBitmapImage = new BitmapImage();

            try
            {
                using (var toFS = File.Open(tsCurrentPath,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var toMS = new MemoryStream((int)toFS.Length))
                    {
                        // Cause a null cast on line 163 to prevent duplicate reads
                        moImageBytes[piCurrentIndex] = tsCurrentPath;

                        await toFS.CopyToAsync(toMS);
                        toMS.Position = 0;

                        toBitmapImage.BeginInit();
                        toBitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        toBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        toBitmapImage.StreamSource = toMS;
                        toBitmapImage.EndInit();
                        toBitmapImage.Freeze();
                    }
                }
            }
            catch (Exception toException)
            {
                // Possibly a filesystem error or a bad image file?
                System.Diagnostics.Debug.WriteLine(toException);
                toBitmapImage = null;
            }
            moImageBytes[piCurrentIndex] = toBitmapImage;
            _renderCurrentSeek(piCurrentSeek, piCurrentIndex);
        }

        //----------------------------------------------------------------------

        private void _renderCurrentSeek(int piCurrentSeek, int piCurrentIndex)
        {
            if (miCurrentSeek == piCurrentSeek)
            {
                var toBitmapImage = moImageBytes[piCurrentIndex] as BitmapImage;

                if (toBitmapImage == null)
                {
                    moMainWindow.ImageControl.ClearValue(Image.SourceProperty);
                }
                else
                {
                    if (mbIsInitialImage)
                    {
                        // Clamp dimensions if needed while preserving aspect ratio
                        if (toBitmapImage.PixelHeight > moMainWindow.MaxHeight)
                        {
                            moMainWindow.Height = moMainWindow.MaxHeight;
                            moMainWindow.Width = (moMainWindow.MaxHeight * toBitmapImage.PixelWidth) / toBitmapImage.PixelHeight;
                        }
                        else if (toBitmapImage.PixelWidth > moMainWindow.MaxWidth)
                        {
                            moMainWindow.Height = (moMainWindow.MaxWidth * toBitmapImage.PixelHeight) / toBitmapImage.PixelWidth;
                            moMainWindow.Width = moMainWindow.MaxWidth;
                        }
                        else
                        {
                            moMainWindow.Height = toBitmapImage.PixelHeight;
                            moMainWindow.Width = toBitmapImage.PixelWidth;
                        }
                        mbIsInitialImage = false;
                    }
                    moMainWindow.ImageControl.SetCurrentValue(Image.SourceProperty, toBitmapImage);
                }
                moMainWindow.Title = Path.GetFileName(moImagePaths[piCurrentIndex]);
                moMainWindow.TitleBarLabel.Text = moMainWindow.Title;
            }
        }
    }
}
