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

        private int miCurrentIndex { get; set; }

        public ImageController(MainWindow poMainWindow)
        {
            moMainWindow = poMainWindow;
            mbIsInitialImage = true;
            miCurrentIndex = -1;
        }

        //----------------------------------------------------------------------

        public async Task LoadImagesAsync(string[] poFilePaths, string psFirst = "")
        {
            moImagePaths = poFilePaths
                .Where(p => VALID_EXTENSIONS_REGEX.IsMatch(p))
                .OrderBy(p => p)
                .ToArray();

            moImageBytes = new object[moImagePaths.Length];
            miCurrentIndex = Array.BinarySearch(moImagePaths, psFirst);

            await _seekToImageAsync(miCurrentIndex);
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
                await _seekToImageAsync(--miCurrentIndex);
            }
        }

        public async Task LoadNextAsync()
        {
            if (moImagePaths != null)
            {
                await _seekToImageAsync(++miCurrentIndex);
            }
        }

        //----------------------------------------------------------------------

        private async Task _loadImageAsync(int piCurrentSeek)
        {
            var tsCurrentPath = moImagePaths[piCurrentSeek];
            var toBitmapImage = new BitmapImage();

            try
            {
                using (var toFS = File.Open(tsCurrentPath,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var toMS = new MemoryStream((int)toFS.Length))
                    {
                        // Prevent dupe file reads by causing null casts
                        moImageBytes[piCurrentSeek] = tsCurrentPath;

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
                // Possibly a bad image file?
                System.Diagnostics.Debug.WriteLine(toException);
                toBitmapImage = null;
            }
            moImageBytes[piCurrentSeek] = toBitmapImage;
        }

        private async Task _seekToImageAsync(int piCurrentSeek)
        {
            int _localWrapIndex(int piIndex)
            {
                var tiCount = moImagePaths.Length;
                return ((piIndex % tiCount) + tiCount) % tiCount;
            }

            var tiCurrentSeek = _localWrapIndex(piCurrentSeek);

            if (moImageBytes[tiCurrentSeek] == null)
            {
                await _loadImageAsync(tiCurrentSeek);
            }

            var toBitmapImage = moImageBytes[tiCurrentSeek] as BitmapImage;

            if (toBitmapImage == null)
            {
                // Something went wrong or the current seek is still loading
                moMainWindow.ImageControl.ClearValue(Image.SourceProperty);
            }
            else
            {
                if (mbIsInitialImage)
                {
                    if (toBitmapImage.PixelHeight > moMainWindow.MaxHeight &&
                        toBitmapImage.PixelWidth > moMainWindow.MaxWidth)
                    {
                        // Cap the height dimension and preserve the aspect ratio
                        moMainWindow.Height = moMainWindow.MaxHeight;
                        moMainWindow.Width = (moMainWindow.MaxHeight * toBitmapImage.PixelWidth) / toBitmapImage.PixelHeight;
                    }
                    else
                    {
                        moMainWindow.Height = toBitmapImage.PixelHeight > moMainWindow.MaxHeight
                            ? moMainWindow.MaxHeight : toBitmapImage.PixelHeight;

                        moMainWindow.Width = toBitmapImage.PixelWidth > moMainWindow.MaxWidth
                            ? moMainWindow.MaxWidth : toBitmapImage.PixelWidth;
                    }
                    mbIsInitialImage = false;
                }
                moMainWindow.ImageControl.SetCurrentValue(Image.SourceProperty, toBitmapImage);
            }
            moMainWindow.Title = Path.GetFileName(moImagePaths[tiCurrentSeek]);
            moMainWindow.TitleBarLabel.Text = moMainWindow.Title;

            var tiPreviousIndex = _localWrapIndex(tiCurrentSeek - 1);
            var tiNextIndex = _localWrapIndex(tiCurrentSeek + 1);

            if (tiPreviousIndex != tiNextIndex)
            {
                if (moImageBytes[tiPreviousIndex] == null)
                {
                    await _loadImageAsync(tiPreviousIndex);
                }
                if (moImageBytes[tiNextIndex] == null)
                {
                    await _loadImageAsync(tiNextIndex);
                }
            }
        }
    }
}
