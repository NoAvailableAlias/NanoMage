using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace NanoMage.Core
{
    public static class ImageAnimator
    {
        #region private types

        private enum eDisposalMethod
        {
            None = 0,
            NoDispose = 1,
            RestoreBackground = 2,
            RestorePrevious = 3
        }

        private class Metadata
        {
            public int miWidth { get; set; }

            public int miHeight { get; set; }
        }

        private class FrameMetadata
        {
            public int miPositionX { get; set; }

            public int miPositionY { get; set; }

            public Metadata moMetadata { get; set; }

            public TimeSpan moDelay { get; set; }

            public eDisposalMethod meDisposalMethod { get; set; }
        }

        #endregion

        //----------------------------------------------------------------------

        #region public interface

        public static Storyboard GetStoryboard(
            Image poImage,
            byte[] poImageBytes,
            Action<BitmapSource> poFirstImageLoaded)
        {
            using (var toMS = new MemoryStream(poImageBytes))
            {
                var toBitmapDecoder = BitmapDecoder.Create(toMS,
                    BitmapCreateOptions.None, BitmapCacheOption.OnLoad
                );

                if (toBitmapDecoder.Frames.Count > 1)
                {
                    var toMetadata = _getMetadata(toBitmapDecoder);
                    if (toMetadata != null)
                    {
                        var toAnimation = _createAnimation(toBitmapDecoder, toMetadata, poFirstImageLoaded);
                        var toStoryboard = new Storyboard();
                        toStoryboard.Children.Add(toAnimation);
                        Storyboard.SetTarget(toAnimation, poImage);
                        Storyboard.SetTargetProperty(toAnimation, new PropertyPath("Source"));
                        toStoryboard.RepeatBehavior = RepeatBehavior.Forever;
                        return toStoryboard;
                    }
                }
                return null;
            }
        }

        #endregion

        //----------------------------------------------------------------------

        #region private animation

        private static ObjectAnimationUsingKeyFrames _createAnimation(
            BitmapDecoder poDecoder,
            Metadata poMetadata,
            Action<BitmapSource> poFirstImageLoaded)
        {
            var toAnimation = new ObjectAnimationUsingKeyFrames();
            var toAnimationTime = TimeSpan.FromMilliseconds(0);
            BitmapSource toBaseFrame = null;

            foreach (var toFrame in poDecoder.Frames)
            {
                var toFrameMetadata = _getFrameMetadata(toFrame);
                if (toFrameMetadata != null)
                {
                    var toFullSize = new Size(poMetadata.miWidth, poMetadata.miHeight);
                    var toRenderedFrame = _renderFrame(toFullSize, toFrame, toFrameMetadata, toBaseFrame);

                    toAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame()
                    {
                        KeyTime = toAnimationTime,
                        Value = toRenderedFrame
                    });

                    switch (toFrameMetadata.meDisposalMethod)
                    {
                        case eDisposalMethod.None:
                            break;
                        case eDisposalMethod.NoDispose:
                            toBaseFrame = toRenderedFrame;
                            break;
                        case eDisposalMethod.RestoreBackground:
                            toBaseFrame = _restoreBackgroundFrame(toFullSize, toFrame, toFrameMetadata);
                            break;
                        case eDisposalMethod.RestorePrevious:
                            break;
                    }
                    // Add the current frame delta to the total animation time
                    toAnimationTime += toFrameMetadata.moDelay;
                    // Callback to perform window resize
                    poFirstImageLoaded?.Invoke(toRenderedFrame);
                }
            }
            // Set the total duration of the animation
            toAnimation.Duration = toAnimationTime;
            return toAnimation;
        }

        #endregion

        //----------------------------------------------------------------------

        #region private metadata

        private static Metadata _getMetadata(BitmapDecoder poDecoder)
        {
            if (poDecoder.Metadata is BitmapMetadata toMetadata)
            {
                var toBitmapMetadata = toMetadata.GetQuery("/logscrdesc") as BitmapMetadata;
                if (toBitmapMetadata != null)
                {
                    return new Metadata
                    {
                        miWidth = toBitmapMetadata.GetQueryValue("/Width", 0),
                        miHeight = toBitmapMetadata.GetQueryValue("/Height", 0)
                    };
                }
            }
            return null;
        }

        private static FrameMetadata _getFrameMetadata(BitmapFrame poFrame)
        {
            if (poFrame.Metadata is BitmapMetadata toMetadata)
            {
                var toGrctlext = toMetadata.GetQuery("/grctlext") as BitmapMetadata;
                var toImgdesc = toMetadata.GetQuery("/imgdesc") as BitmapMetadata;

                if (toGrctlext != null && toImgdesc != null)
                {
                    var toFrameMetadata =  new FrameMetadata
                    {
                        miPositionX = toImgdesc.GetQueryValue("/Left", 0),
                        miPositionY = toImgdesc.GetQueryValue("/Top", 0),
                        moMetadata = new Metadata
                        {
                            miWidth = toImgdesc.GetQueryValue("/Width", 0),
                            miHeight = toImgdesc.GetQueryValue("/Height", 0)
                        },
                        moDelay = TimeSpan.FromMilliseconds(toGrctlext.GetQueryValue("/Delay", 10) * 10),
                        meDisposalMethod = (eDisposalMethod)toGrctlext.GetQueryValue("/Disposal", 0)
                    };

                    // Handle malformed gif delays by setting a minimum delay
                    toFrameMetadata.moDelay = toFrameMetadata.moDelay.Ticks == 0
                        ? TimeSpan.FromMilliseconds(50) : toFrameMetadata.moDelay;

                    return toFrameMetadata;
                }
            }
            return null;
        }

        #endregion

        //----------------------------------------------------------------------

        #region private rendering

        private static BitmapSource _renderFrame(
            Size poFullSize,
            BitmapSource poFrame,
            FrameMetadata poMetadata,
            BitmapSource poBaseFrame)
        {
            if (_isNotFullSize(poMetadata, poFullSize))
            {
                var toVisual = new DrawingVisual();
                using (var toContext = toVisual.RenderOpen())
                {
                    if (poBaseFrame != null)
                    {
                        var toFullRect = new Rect(0, 0, poFullSize.Width, poFullSize.Height);
                        toContext.DrawImage(poBaseFrame, toFullRect);
                    }

                    var toRect = new Rect(
                        poMetadata.miPositionX, poMetadata.miPositionY,
                        poMetadata.moMetadata.miWidth, poMetadata.moMetadata.miHeight
                    );
                    toContext.DrawImage(poFrame, toRect);
                }

                var toRenderer = new RenderTargetBitmap(
                    Convert.ToInt32(poFullSize.Width),
                    Convert.ToInt32(poFullSize.Height),
                    96, 96, PixelFormats.Pbgra32
                );
                toRenderer.Render(toVisual);

                var toFrame = new WriteableBitmap(toRenderer);
                toFrame.Freeze();
                return toFrame;
            }
            return poFrame;
        }

        private static BitmapSource _restoreBackgroundFrame(
            Size poFullSize,
            BitmapSource poFrame,
            FrameMetadata poMetadata)
        {
            if (_isNotFullSize(poMetadata, poFullSize))
            {
                var toVisual = new DrawingVisual();
                using (var toContext = toVisual.RenderOpen())
                {
                    var toFullRect = new Rect(0, 0, poFrame.PixelWidth, poFrame.PixelHeight);
                    var toRect = new Rect(poMetadata.miPositionX, poMetadata.miPositionY,
                        poMetadata.moMetadata.miWidth, poMetadata.moMetadata.miHeight
                    );
                    var toClip = Geometry.Combine(new RectangleGeometry(toFullRect),
                        new RectangleGeometry(toRect), GeometryCombineMode.Exclude, null
                    );
                    toContext.PushClip(toClip);
                    toContext.DrawImage(poFrame, toFullRect);
                }

                var toRenderer = new RenderTargetBitmap(
                    poFrame.PixelWidth, poFrame.PixelHeight,
                    poFrame.DpiX, poFrame.DpiY,
                    PixelFormats.Pbgra32
                );
                toRenderer.Render(toVisual);

                var toFrame = new WriteableBitmap(toRenderer);
                toFrame.Freeze();
                return toFrame;
            }
            return null;
        }

        #endregion

        //----------------------------------------------------------------------

        #region private helpers

        private static bool _isNotFullSize(FrameMetadata poMetadata, Size poFullSize)
        {
            return poMetadata.miPositionX != 0
                || poMetadata.miPositionY != 0
                || poMetadata.moMetadata.miWidth != poFullSize.Width
                || poMetadata.moMetadata.miHeight != poFullSize.Height;
        }

        #endregion
    }

    //--------------------------------------------------------------------------

    internal static class BitmapMetadataExtensions
    {
        public static T GetQueryValue<T>(
            this BitmapMetadata poMetadata,
            string psQuery,
            T poDefault)
        {
            if (poMetadata.ContainsQuery(psQuery))
            {
                return (T)Convert.ChangeType(poMetadata.GetQuery(psQuery), typeof(T));
            }
            return poDefault;
        }
    }
}
