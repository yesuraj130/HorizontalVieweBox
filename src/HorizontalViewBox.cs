using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

//Test 1
//Test 2
//Test 3

namespace HorizontalVieweBox
{
    public enum FitOrientation
    {
        Horizontal,
        Vertical,
        Both,
    }

    /// <summary>
    /// A simple WPF decorator that scales its child according to fit orientation
    /// and exposes MinScale, MaxScale and a read-only Scale property.
    /// </summary>
    public class HorizontalViewBox : Decorator
    {
        public static readonly DependencyProperty MinScaleProperty =
            DependencyProperty.Register(
                nameof(MinScale), typeof(double), typeof(HorizontalViewBox),
                new FrameworkPropertyMetadata(0.1, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnMinMaxChanged));

        public static readonly DependencyProperty MaxScaleProperty =
            DependencyProperty.Register(
                nameof(MaxScale), typeof(double), typeof(HorizontalViewBox),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnMinMaxChanged));

        public static readonly DependencyProperty FitOrientationProperty =
            DependencyProperty.Register(
                nameof(FitOrientation), typeof(FitOrientation), typeof(HorizontalViewBox),
                new FrameworkPropertyMetadata(FitOrientation.Both, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        private static readonly DependencyPropertyKey ScalePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Scale), typeof(double), typeof(HorizontalViewBox),
                new FrameworkPropertyMetadata(1.0));

        public static readonly DependencyProperty ScaleProperty = ScalePropertyKey.DependencyProperty;

        public double MinScale
        {
            get => (double)GetValue(MinScaleProperty);
            set => SetValue(MinScaleProperty, value);
        }

        public double MaxScale
        {
            get => (double)GetValue(MaxScaleProperty);
            set => SetValue(MaxScaleProperty, value);
        }

        public FitOrientation FitOrientation
        {
            get => (FitOrientation)GetValue(FitOrientationProperty);
            set => SetValue(FitOrientationProperty, value);
        }

        /// <summary>
        /// Read-only scale currently applied to the child.
        /// </summary>
        public double Scale => (double)GetValue(ScaleProperty);

        private static void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var hvb = (HorizontalViewBox)d;
            // keep min/max consistent
            if (hvb.MinScale <= 0) hvb.MinScale = 0.0001;
            if (hvb.MaxScale <= 0) hvb.MaxScale = hvb.MinScale;
            if (hvb.MinScale > hvb.MaxScale)
            {
                var tmp = hvb.MinScale;
                hvb.MinScale = hvb.MaxScale;
                hvb.MaxScale = tmp;
            }
            hvb.CoerceScale();
        }

        private void CoerceScale()
        {
            var s = Scale;
            var clamped = Clamp(s);
            if (!DoubleUtil.AreClose(s, clamped))
            {
                SetValue(ScalePropertyKey, clamped);
                UpdateChildTransform(clamped);
            }
        }

        private double Clamp(double value)
        {
            return Math.Max(MinScale, Math.Min(MaxScale, value));
        }

        /// <summary>
        /// Zoom to an absolute scale (will be clamped to Min/Max).
        /// </summary>
        public void ZoomTo(double value)
        {
            var clamped = Clamp(value);
            SetValue(ScalePropertyKey, clamped);
            UpdateChildTransform(clamped);
            InvalidateMeasure();
            InvalidateArrange();
        }

        /// <summary>
        /// Zoom by a multiplicative factor.
        /// </summary>
        public void ZoomBy(double factor)
        {
            ZoomTo(Scale * factor);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Child == null) return new Size(0, 0);

            // Measure child at its desired size
            Child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desired = Child.DesiredSize;

            if (double.IsInfinity(constraint.Width) && double.IsInfinity(constraint.Height))
            {
                // No constraints -> don't scale
                SetValue(ScalePropertyKey, Clamp(1.0));
                UpdateChildTransform(Scale);
                return desired;
            }

            var scaleX = constraint.Width / Math.Max(1e-6, desired.Width);
            var scaleY = constraint.Height / Math.Max(1e-6, desired.Height);

            double newScale;
            switch (FitOrientation)
            {
                case FitOrientation.Horizontal:
                    newScale = scaleX;
                    break;
                case FitOrientation.Vertical:
                    newScale = scaleY;
                    break;
                default:
                    newScale = Math.Min(scaleX, scaleY);
                    break;
            }

            newScale = Clamp(newScale);
            SetValue(ScalePropertyKey, newScale);
            UpdateChildTransform(newScale);

            // return the space the child will occupy after scale
            return new Size(desired.Width * newScale, desired.Height * newScale);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (Child == null) return arrangeSize;

            var desired = Child.DesiredSize;
            var scale = Scale;

            var fe = Child as FrameworkElement;
            var hAlign = fe?.HorizontalAlignment ?? HorizontalAlignment.Center;
            var vAlign = fe?.VerticalAlignment ?? VerticalAlignment.Center;

            // When alignment is Stretch, size the child so that after applying the clamped scale
            // it fills the available space. This sizes the child during Arrange (finalSize)
            // while still capping the scale at MaxScale.
            double arrangedChildWidth = desired.Width;
            double arrangedChildHeight = desired.Height;

            if (hAlign == HorizontalAlignment.Stretch)
            {
                arrangedChildWidth = Math.Max(0.0, arrangeSize.Width / Math.Max(1e-6, scale));
            }

            if (vAlign == VerticalAlignment.Stretch)
            {
                arrangedChildHeight = Math.Max(0.0, arrangeSize.Height / Math.Max(1e-6, scale));
            }

            var childSize = new Size(arrangedChildWidth * scale, arrangedChildHeight * scale);

            double offsetX;
            double offsetY;

            switch (hAlign)
            {
                case HorizontalAlignment.Left:
                    offsetX = 0;
                    break;
                case HorizontalAlignment.Center:
                    offsetX = (arrangeSize.Width - childSize.Width) / 2.0;
                    break;
                case HorizontalAlignment.Right:
                    offsetX = arrangeSize.Width - childSize.Width;
                    break;
                case HorizontalAlignment.Stretch:
                    offsetX = 0;
                    break;
                default:
                    offsetX = (arrangeSize.Width - childSize.Width) / 2.0;
                    break;
            }

            switch (vAlign)
            {
                case VerticalAlignment.Top:
                    offsetY = 0;
                    break;
                case VerticalAlignment.Center:
                    offsetY = (arrangeSize.Height - childSize.Height) / 2.0;
                    break;
                case VerticalAlignment.Bottom:
                    offsetY = arrangeSize.Height - childSize.Height;
                    break;
                case VerticalAlignment.Stretch:
                    offsetY = 0;
                    break;
                default:
                    offsetY = (arrangeSize.Height - childSize.Height) / 2.0;
                    break;
            }

            var childRect = new Rect(offsetX / Math.Max(1e-6, scale), offsetY / Math.Max(1e-6, scale), arrangedChildWidth, arrangedChildHeight);
            Child.Arrange(childRect);

            return arrangeSize;
        }

        private void UpdateChildTransform(double scale)
        {
            if (Child == null) return;
            var fe = Child as FrameworkElement;
            if (fe == null) return;
            var st = fe.LayoutTransform as ScaleTransform;
            if (st == null || !DoubleUtil.AreClose(st.ScaleX, scale) || !DoubleUtil.AreClose(st.ScaleY, scale))
            {
                fe.LayoutTransform = new ScaleTransform(scale, scale);
            }
        }
    }

    internal static class DoubleUtil
    {
        private const double Epsilon = 1e-6;
        public static bool AreClose(double a, double b) => Math.Abs(a - b) < Epsilon;
    }

    // Helper structures for testing/layout computation without requiring WPF layout pass.
    public struct LayoutResult
    {
        public double ActualScale { get; set; }
        public double ChildActualWidth { get; set; }
        public double ChildActualHeight { get; set; }
        public double LeftSpace { get; set; }
        public double RightSpace { get; set; }
        public double TopSpace { get; set; }
        public double BottomSpace { get; set; }
    }

    public static class LayoutTester
    {
        public static LayoutResult ComputeLayout(
            double parentWidth, double parentHeight,
            double childDesiredWidth, double childDesiredHeight,
            double minScale, double maxScale,
            FitOrientation fitOrientation,
            HorizontalAlignment hAlign,
            VerticalAlignment vAlign)
        {
            // compute raw scales
            var scaleX = parentWidth / Math.Max(1e-6, childDesiredWidth);
            var scaleY = parentHeight / Math.Max(1e-6, childDesiredHeight);

            double scale;
            switch (fitOrientation)
            {
                case FitOrientation.Horizontal:
                    scale = scaleX;
                    break;
                case FitOrientation.Vertical:
                    scale = scaleY;
                    break;
                default:
                    scale = Math.Min(scaleX, scaleY);
                    break;
            }

            // clamp
            var clamped = Math.Max(minScale, Math.Min(maxScale, scale));

            // When Stretch on an axis, arrange the child such that after applying clamped scale
            // it fills that axis. That means arrangedChildWidth = parentWidth / clamped.
            double arrangedChildWidth = childDesiredWidth;
            double arrangedChildHeight = childDesiredHeight;

            if (hAlign == HorizontalAlignment.Stretch)
            {
                arrangedChildWidth = parentWidth / Math.Max(1e-6, clamped);
            }
            if (vAlign == VerticalAlignment.Stretch)
            {
                arrangedChildHeight = parentHeight / Math.Max(1e-6, clamped);
            }

            var childActualWidth = arrangedChildWidth * clamped;
            var childActualHeight = arrangedChildHeight * clamped;

            double leftSpace = 0, rightSpace = 0, topSpace = 0, bottomSpace = 0;

            switch (hAlign)
            {
                case HorizontalAlignment.Left:
                    leftSpace = 0;
                    rightSpace = Math.Max(0, parentWidth - childActualWidth);
                    break;
                case HorizontalAlignment.Center:
                    leftSpace = Math.Max(0, (parentWidth - childActualWidth) / 2.0);
                    rightSpace = leftSpace;
                    break;
                case HorizontalAlignment.Right:
                    leftSpace = Math.Max(0, parentWidth - childActualWidth);
                    rightSpace = 0;
                    break;
                case HorizontalAlignment.Stretch:
                    leftSpace = 0;
                    rightSpace = 0;
                    break;
            }

            switch (vAlign)
            {
                case VerticalAlignment.Top:
                    topSpace = 0;
                    bottomSpace = Math.Max(0, parentHeight - childActualHeight);
                    break;
                case VerticalAlignment.Center:
                    topSpace = Math.Max(0, (parentHeight - childActualHeight) / 2.0);
                    bottomSpace = topSpace;
                    break;
                case VerticalAlignment.Bottom:
                    topSpace = Math.Max(0, parentHeight - childActualHeight);
                    bottomSpace = 0;
                    break;
                case VerticalAlignment.Stretch:
                    topSpace = 0;
                    bottomSpace = 0;
                    break;
            }

            return new LayoutResult
            {
                ActualScale = clamped,
                ChildActualWidth = childActualWidth,
                ChildActualHeight = childActualHeight,
                LeftSpace = leftSpace,
                RightSpace = rightSpace,
                TopSpace = topSpace,
                BottomSpace = bottomSpace
            };
        }
    }
}
