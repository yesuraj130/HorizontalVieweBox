HorizontalVieweBox
===================

This project provides a small WPF `HorizontalViewBox` control that scales its child according to `FitOrientation` and exposes `MinScale`, `MaxScale`, and a read-only `Scale` property.

Example usage in XAML:

```xml
<Window x:Class="Example.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:HorizontalVieweBox;assembly=HorizontalVieweBox"
        Title="MainWindow" Height="450" Width="800">
    <local:HorizontalViewBox MinScale="0.5" MaxScale="4" FitOrientation="Horizontal">
        <Image Source="/Images/large.png" />
    </local:HorizontalViewBox>
</Window>
```

Or from code-behind:

```csharp
var hvb = new HorizontalViewBox.HorizontalViewBox
{
    MinScale = 0.5,
    MaxScale = 4,
    FitOrientation = HorizontalVieweBox.FitOrientation.Horizontal
};
hvb.Child = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Images/large.png")) };
// Zoom programmatically
hvb.ZoomTo(2.0);
```
# HorizontalVieweBox

This repo provides a small `HorizontalViewBox` TypeScript class that manages scaling and fit orientation for content within a viewport.

Usage example:

```ts
import HorizontalViewBox, { FitOrientation } from './src/HorizontalViewBox';

const hvb = new HorizontalViewBox({ minScale: 0.5, maxScale: 4, fitOrientation: FitOrientation.Horizontal });
console.log(hvb.scale); // read-only getter
hvb.zoomTo(2);
console.log(hvb.scale);
hvb.fit(800, 600, 1600, 600); // fits horizontally
console.log(hvb.scale);
```

Run the layout tester to get a Markdown table of many combinations:

```bash
dotnet run --project tools/LayoutTester
```

This prints a Markdown table showing `ActualScale`, child actual size, and space on each side for each combination of `FitOrientation`, `HorizontalAlignment`, and `VerticalAlignment`.
