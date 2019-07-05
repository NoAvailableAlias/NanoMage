using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NanoMage.Controls
{
    /// <summary>
    /// Crucial control found on SO that allows image panning and zooming.
    /// </summary>
    /// <remarks>
    /// https://stackoverflow.com/questions/741956/pan-zoom-image/6782715#6782715
    /// </remarks>
    public class ZoomBorder : Border
    {
        private static readonly double MAX_ZOOM_SCALE = 0.4;
        private static readonly double ZOOM_SCALE = 0.2;

        //----------------------------------------------------------------------

        private UIElement moChild = null;
        private Point moOrigin;
        private Point moStart;

        //----------------------------------------------------------------------

        public override UIElement Child
        {
            get
            {
                return base.Child;
            }
            set
            {
                if (value != null && value != this.Child)
                {
                    this.Initialize(value);
                }
                base.Child = value;
            }
        }

        public void Initialize(UIElement poElement)
        {
            moChild = poElement;

            if (moChild != null)
            {
                TransformGroup tg = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                tg.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                tg.Children.Add(tt);
                moChild.RenderTransform = tg;
                moChild.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += child_PreviewMouseRightButtonDown;
            }
        }

        public void Reset()
        {
            if (moChild != null)
            {
                // reset zoom
                var st = _GetScaleTransform(moChild);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = _GetTranslateTransform(moChild);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        public void ZoomOut()
        {
            if (moChild != null)
            {
                var toRelative = Mouse.GetPosition(Application.Current.MainWindow);
                _ZoomAndTranslate(toRelative, -ZOOM_SCALE);
            }
        }

        public void ZoomIn()
        {
            if (moChild != null)
            {
                var toRelative = Mouse.GetPosition(Application.Current.MainWindow);
                _ZoomAndTranslate(toRelative, ZOOM_SCALE);
            }
        }

        //----------------------------------------------------------------------

        private TranslateTransform _GetTranslateTransform(UIElement poElement)
        {
            return (TranslateTransform)((TransformGroup)poElement.RenderTransform)
              .Children.First(rt => rt is TranslateTransform);
        }

        private ScaleTransform _GetScaleTransform(UIElement poElement)
        {
            return (ScaleTransform)((TransformGroup)poElement.RenderTransform)
              .Children.First(rt => rt is ScaleTransform);
        }

        private void _ZoomAndTranslate(Point poRelative, double pfZoom)
        {
            var st = _GetScaleTransform(moChild);
            var tt = _GetTranslateTransform(moChild);

            double tfAbsoluteX;
            double tfAbsoluteY;

            tfAbsoluteX = poRelative.X * st.ScaleX + tt.X;
            tfAbsoluteY = poRelative.Y * st.ScaleY + tt.Y;

            st.ScaleX += pfZoom;
            st.ScaleY += pfZoom;

            tt.X = tfAbsoluteX - poRelative.X * st.ScaleX;
            tt.Y = tfAbsoluteY - poRelative.Y * st.ScaleY;
        }

        //----------------------------------------------------------------------

        #region child event handlers

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (moChild != null)
            {
                var st = _GetScaleTransform(moChild);
                var tfZoom = e.Delta > 0 ? ZOOM_SCALE : -ZOOM_SCALE;

                if (!(e.Delta > 0) && (st.ScaleX < MAX_ZOOM_SCALE || st.ScaleY < MAX_ZOOM_SCALE))
                {
                    return;
                }
                Point toRelative = e.GetPosition(moChild);
                _ZoomAndTranslate(toRelative, tfZoom);
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (moChild != null)
            {
                var tt = _GetTranslateTransform(moChild);
                moStart = e.GetPosition(this);
                moOrigin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                moChild.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (moChild != null)
            {
                moChild.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Reset();
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (moChild != null)
            {
                if (moChild.IsMouseCaptured)
                {
                    var tt = _GetTranslateTransform(moChild);
                    Vector v = moStart - e.GetPosition(this);
                    tt.X = moOrigin.X - v.X;
                    tt.Y = moOrigin.Y - v.Y;
                }
            }
        }

        #endregion
    }
}
