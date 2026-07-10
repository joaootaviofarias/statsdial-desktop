using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PcSystemMonitorLcd.Gui.Controls;

public sealed class Sparkline : Control
{
    public static readonly StyledProperty<IEnumerable<double>?> ValuesProperty =
        AvaloniaProperty.Register<Sparkline, IEnumerable<double>?>(nameof(Values));

    public static readonly StyledProperty<IBrush> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush>(nameof(Stroke), Brushes.Cyan);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 2.0);

    public static readonly StyledProperty<double> MaxValueProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(MaxValue), 100.0);

    static Sparkline()
    {
        AffectsRender<Sparkline>(ValuesProperty, StrokeProperty, StrokeThicknessProperty, MaxValueProperty);
    }

    public IEnumerable<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IBrush Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double MaxValue
    {
        get => GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != ValuesProperty) return;

        if (change.OldValue is INotifyCollectionChanged oldIncc)
            oldIncc.CollectionChanged -= OnValuesCollectionChanged;

        if (change.NewValue is INotifyCollectionChanged newIncc)
            newIncc.CollectionChanged += OnValuesCollectionChanged;

        InvalidateVisual();
    }

    private void OnValuesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var values = Values?.ToArray();
        var width = Bounds.Width;
        var height = Bounds.Height;

        if (values is null || values.Length < 2 || width <= 0 || height <= 0)
            return;

        double max = Math.Max(MaxValue, 1);
        double stepX = width / (values.Length - 1);

        Point PointAt(int i)
        {
            double x = i * stepX;
            double v = Math.Clamp(values[i], 0, max);
            double y = height - v / max * height;
            return new Point(x, y);
        }

        // Filled area under the line, faded toward the bottom for a subtle glow-on-glass look
        var fillGeometry = new StreamGeometry();
        using (var ctx = fillGeometry.Open())
        {
            ctx.BeginFigure(new Point(0, height), true);
            for (int i = 0; i < values.Length; i++)
                ctx.LineTo(PointAt(i));
            ctx.LineTo(new Point(width, height));
            ctx.EndFigure(true);
        }

        var fillBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(((SolidColorBrush)Stroke).Color, 0),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
            }
        };
        fillBrush.Opacity = 0.18;
        context.DrawGeometry(fillBrush, null, fillGeometry);

        // The line itself
        var lineGeometry = new StreamGeometry();
        using (var ctx = lineGeometry.Open())
        {
            ctx.BeginFigure(PointAt(0), false);
            for (int i = 1; i < values.Length; i++)
                ctx.LineTo(PointAt(i));
            ctx.EndFigure(false);
        }

        var pen = new Pen(Stroke, StrokeThickness)
        {
            LineJoin = PenLineJoin.Round,
            LineCap = PenLineCap.Round
        };
        context.DrawGeometry(null, pen, lineGeometry);
    }
}
