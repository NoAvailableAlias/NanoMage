using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace NanoMage.Core
{
    /// <summary>
    /// Main class for handling image browsing.
    /// </summary>
    public class ImageController
    {
        #region properties

        private static readonly Regex VALID_EXTENSIONS_REGEX = new Regex(
            @"^.+\.(?i)(gif|png|jpg|jpeg)(?-i)$", RegexOptions.Compiled
        );

        private Action<ImageSource> moFirstImageLoaded;

        private MainWindow moMainWindow { get; set; }

        private string[] moImagePaths { get; set; }

        private object[] moImageDatas { get; set; }

        private int miCurrentSeek { get; set; }

        public string CurrentPath => moImagePaths?[_convertSeekToIndex(miCurrentSeek)];

        #endregion

        //----------------------------------------------------------------------

        #region construction

        public ImageController(MainWindow poMainWindow)
        {
            moMainWindow = poMainWindow;

            // Clamp dimensions if needed while preserving aspect ratio
            moFirstImageLoaded = (poImageSource) =>
            {
                if (poImageSource.Height > moMainWindow.MaxHeight)
                {
                    moMainWindow.Height = moMainWindow.MaxHeight;
                    moMainWindow.Width = (moMainWindow.MaxHeight * poImageSource.Width) / poImageSource.Height;
                }
                else if (poImageSource.Width > moMainWindow.MaxWidth)
                {
                    moMainWindow.Height = (moMainWindow.MaxWidth * poImageSource.Height) / poImageSource.Width;
                    moMainWindow.Width = moMainWindow.MaxWidth;
                }
                else
                {
                    moMainWindow.Height = poImageSource.Height;
                    moMainWindow.Width = poImageSource.Width;
                }
                moFirstImageLoaded = null;
            };
        }

        #endregion

        //----------------------------------------------------------------------

        #region public interface

        public async Task LoadImagesAsync(string[] poFilePaths, string psFirst = "")
        {
            moImagePaths = poFilePaths
                .Where(p => VALID_EXTENSIONS_REGEX.IsMatch(p))
                .OrderBy(p => p)
                .ToArray();

            moImageDatas = new object[moImagePaths.Length];
            miCurrentSeek = Array.BinarySearch(moImagePaths, psFirst);

            await _seekToImageAsync(miCurrentSeek, miCurrentSeek);
        }

        public async Task LoadImageAsync(string psFilePath)
        {
            try
            {
                await LoadImagesAsync(
                    Directory.GetFiles(Path.GetDirectoryName(psFilePath)),
                    psFilePath
                );
            }
            catch (Exception toException)
            {
                System.Diagnostics.Debug.WriteLine(toException);
            }
        }

        public async Task LoadPreviousAsync()
        {
            await _seekToImageAsync(miCurrentSeek, --miCurrentSeek);
        }

        public async Task LoadNextAsync()
        {
            await _seekToImageAsync(miCurrentSeek, ++miCurrentSeek);
        }

        #endregion

        //----------------------------------------------------------------------

        #region private interface

        private async Task _seekToImageAsync(int piLastSeek, int piCurrentSeek)
        {
            if (moImagePaths?.Length > 0)
            {
                var tiCurrentIndex = _convertSeekToIndex(piCurrentSeek);

                if (moImageDatas[tiCurrentIndex] == null)
                {
                    await _loadImageAsync(piLastSeek, piCurrentSeek, tiCurrentIndex);
                }
                else
                {
                    _renderCurrentSeek(piLastSeek, piCurrentSeek, tiCurrentIndex);
                }

                var tiPreviousSeek = piCurrentSeek - 1;
                var tiPreviousIndex = _convertSeekToIndex(tiPreviousSeek);

                var tiNextSeek = piCurrentSeek + 1;
                var tiNextIndex = _convertSeekToIndex(tiNextSeek);

                if (tiPreviousIndex != tiNextIndex)
                {
                    if (moImageDatas[tiPreviousIndex] == null)
                    {
                        await _loadImageAsync(piLastSeek, tiPreviousSeek, tiPreviousIndex);
                    }
                    if (moImageDatas[tiNextIndex] == null)
                    {
                        await _loadImageAsync(piLastSeek, tiNextSeek, tiNextIndex);
                    }
                }
            }
        }

        //----------------------------------------------------------------------

        private async Task _loadImageAsync(int piLastSeek, int piCurrentSeek, int piCurrentIndex)
        {
            var tsCurrentPath = moImagePaths[piCurrentIndex];

            try
            {
                using (var toFS = File.Open(tsCurrentPath,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var toMS = new MemoryStream((int)toFS.Length))
                    {
                        // Prevent duplicate reads of this image file
                        moImageDatas[piCurrentIndex] = tsCurrentPath;

                        await toFS.CopyToAsync(toMS);
                        moImageDatas[piCurrentIndex] = toMS.ToArray();
                    }
                }
            }
            catch (Exception toException)
            {
                System.Diagnostics.Debug.WriteLine(toException);
            }
            _renderCurrentSeek(piLastSeek, piCurrentSeek, piCurrentIndex);
        }

        //----------------------------------------------------------------------

        private void _renderCurrentSeek(int piLastSeek, int piCurrentSeek, int piCurrentIndex)
        {
            // If this render call is still for the current seek
            if (miCurrentSeek == piCurrentSeek)
            {
                // Stop any previous seek animations that might be running
                if (moImageDatas[_convertSeekToIndex(piLastSeek)] is Storyboard toAnimation)
                {
                    toAnimation.Stop();
                }

                var toImageData = moImageDatas[piCurrentIndex];
                if (toImageData is string)
                {
                    // Something went wrong or the current seek is still loading
                    moMainWindow.ImageControl.ClearValue(Image.SourceProperty);
                }
                else if (toImageData is Storyboard toStoryboard)
                {
                    toStoryboard.Begin();
                }
                else if (toImageData is BitmapImage toBitmapImage)
                {
                    moMainWindow.ImageControl.SetValue(Image.SourceProperty, toBitmapImage);
                }
                else if (toImageData is byte[] toImageBytes)
                {
                    toStoryboard = ImageAnimator.GetStoryboard(
                        moMainWindow.ImageControl, toImageBytes, moFirstImageLoaded
                    );

                    if (toStoryboard is null)
                    {
                        using (var toMS = new MemoryStream(toImageBytes))
                        {
                            toBitmapImage = new BitmapImage();
                            toBitmapImage.BeginInit();
                            toBitmapImage.CreateOptions = BitmapCreateOptions.None;
                            toBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            toBitmapImage.StreamSource = toMS;
                            toBitmapImage.EndInit();
                            toBitmapImage.Freeze();

                            moImageDatas[piCurrentIndex] = toBitmapImage;
                        }
                        moFirstImageLoaded?.Invoke(toBitmapImage);
                        moMainWindow.ImageControl.SetValue(Image.SourceProperty, toBitmapImage);
                    }
                    else
                    {
                        moImageDatas[piCurrentIndex] = toStoryboard;
                        toStoryboard.Begin();
                    }
                }
                moMainWindow.Title = Path.GetFileName(moImagePaths[piCurrentIndex]);
            }
        }

        //----------------------------------------------------------------------

        private int _convertSeekToIndex(int piSeek)
        {
            var tiCount = moImagePaths.Length;
            return ((piSeek % tiCount) + tiCount) % tiCount;
        }

        #endregion
    }
}
