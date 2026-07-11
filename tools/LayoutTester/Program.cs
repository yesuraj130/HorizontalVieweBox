using System;
using System;
using System.Text;
using System.Collections.Generic;

enum FitOrientation { Horizontal, Vertical, Both }
enum HAlign { Left, Center, Right, Stretch }
enum VAlign { Top, Center, Bottom, Stretch }

record LayoutResult(double ActualScale, double ChildActualWidth, double ChildActualHeight, double LeftSpace, double RightSpace, double TopSpace, double BottomSpace);

class Program
{
    static LayoutResult ComputeLayout(
        double parentWidth, double parentHeight,
        double childDesiredWidth, double childDesiredHeight,
        double minScale, double maxScale,
        FitOrientation fitOrientation,
        HAlign hAlign,
        VAlign vAlign)
    {
        var scaleX = parentWidth / Math.Max(1e-6, childDesiredWidth);
        var scaleY = parentHeight / Math.Max(1e-6, childDesiredHeight);

        double scale = fitOrientation == FitOrientation.Horizontal ? scaleX : (fitOrientation == FitOrientation.Vertical ? scaleY : Math.Min(scaleX, scaleY));
        var clamped = Math.Max(minScale, Math.Min(maxScale, scale));

        double arrangedChildWidth = childDesiredWidth;
        double arrangedChildHeight = childDesiredHeight;
        if (hAlign == HAlign.Stretch) arrangedChildWidth = parentWidth / Math.Max(1e-6, clamped);
        if (vAlign == VAlign.Stretch) arrangedChildHeight = parentHeight / Math.Max(1e-6, clamped);

        var childActualWidth = arrangedChildWidth * clamped;
        var childActualHeight = arrangedChildHeight * clamped;

        double leftSpace = 0, rightSpace = 0, topSpace = 0, bottomSpace = 0;
        switch (hAlign)
        {
            case HAlign.Left:
                leftSpace = 0; rightSpace = Math.Max(0, parentWidth - childActualWidth); break;
            case HAlign.Center:
                leftSpace = Math.Max(0, (parentWidth - childActualWidth) / 2.0); rightSpace = leftSpace; break;
            case HAlign.Right:
                leftSpace = Math.Max(0, parentWidth - childActualWidth); rightSpace = 0; break;
            case HAlign.Stretch:
                leftSpace = 0; rightSpace = 0; break;
        }
        switch (vAlign)
        {
            case VAlign.Top:
                topSpace = 0; bottomSpace = Math.Max(0, parentHeight - childActualHeight); break;
            case VAlign.Center:
                topSpace = Math.Max(0, (parentHeight - childActualHeight) / 2.0); bottomSpace = topSpace; break;
            case VAlign.Bottom:
                topSpace = Math.Max(0, parentHeight - childActualHeight); bottomSpace = 0; break;
            case VAlign.Stretch:
                topSpace = 0; bottomSpace = 0; break;
        }

        return new LayoutResult(clamped, childActualWidth, childActualHeight, leftSpace, rightSpace, topSpace, bottomSpace);
    }

    static void Main()
    {
        double parentW = 1200, parentH = 800;
        double childW = 400, childH = 300;
        double minScale = 0.5, maxScale = 2.0;

        var fits = new[] { FitOrientation.Horizontal, FitOrientation.Vertical, FitOrientation.Both };
        var hAligns = new[] { HAlign.Left, HAlign.Center, HAlign.Right, HAlign.Stretch };
        var vAligns = new[] { VAlign.Top, VAlign.Center, VAlign.Bottom, VAlign.Stretch };

        var rows = new List<string>();
        rows.Add("|Fit|HAlign|VAlign|Scale(min,max)|ActualScale|ChildActualWxH|LeftSpace|RightSpace|TopSpace|BottomSpace|");
        rows.Add("|---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|");

        foreach (var fit in fits)
        {
            foreach (var ha in hAligns)
            {
                foreach (var va in vAligns)
                {
                    var res = ComputeLayout(parentW, parentH, childW, childH, minScale, maxScale, fit, ha, va);
                    rows.Add($"|{fit}|{ha}|{va}|{minScale},{maxScale}|{res.ActualScale:0.###}|{res.ChildActualWidth:0.###}x{res.ChildActualHeight:0.###}|{res.LeftSpace:0.###}|{res.RightSpace:0.###}|{res.TopSpace:0.###}|{res.BottomSpace:0.###}|");
                }
            }
        }

        var sb = new StringBuilder();
        foreach (var r in rows) sb.AppendLine(r);

        Console.WriteLine(sb.ToString());
    }
}
