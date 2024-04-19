﻿namespace ScottPlot.Plottables;

public class FunctionPlot(IFunctionSource source) : IPlottable, IHasLine
{
    public bool IsVisible { get; set; } = true;
    public IAxes Axes { get; set; } = new Axes();
    public string Label { get; set; } = string.Empty;
    private IFunctionSource Source { get; set; } = source;
    private CoordinateRange MaxObservedRangeY { get; set; } = CoordinateRange.NotSet;
    private CoordinateRange LastRenderHorizontalSpan { get; set; } = new(-10, 10);
    public double GetY(double x) => Source.Get(x);

    public LineStyle LineStyle { get; } = new() { Width = 1 };
    public float LineWidth { get => LineStyle.Width; set => LineStyle.Width = value; }
    public LinePattern LinePattern { get => LineStyle.Pattern; set => LineStyle.Pattern = value; }
    public Color LineColor { get => LineStyle.Color; set => LineStyle.Color = value; }

    public double MinX
    {
        get
        {
            return double.IsInfinity(Source.RangeX.Min)
                ? Axes.XAxis.Min
                : Source.RangeX.Min;
        }
        set
        {
            Source.RangeX = new CoordinateRange(value, Source.RangeX.Max);
        }
    }

    public double MaxX
    {
        get
        {
            return double.IsInfinity(Source.RangeX.Max)
                ? Axes.XAxis.Max
                : Source.RangeX.Max;
        }
        set
        {
            Source.RangeX = new CoordinateRange(Source.RangeX.Min, value);
        }
    }

    public IEnumerable<LegendItem> LegendItems => LegendItem.Single(Label, LineStyle);

    public AxisLimits GetAxisLimits()
    {
        if (MaxObservedRangeY == CoordinateRange.NotSet)
            return new AxisLimits(-10, 10, -10, 10);

        bool sourceRangeIsFinite = !(double.IsInfinity(Source.RangeX.Min) || double.IsInfinity(Source.RangeX.Max));
        CoordinateRange xRange = sourceRangeIsFinite ? Source.RangeX : LastRenderHorizontalSpan;
        return new AxisLimits(xRange, MaxObservedRangeY);
    }

    public void Render(RenderPack rp)
    {
        var unitsPerPixel = Axes.XAxis.GetCoordinateDistance(1, rp.DataRect);

        double max = double.MinValue;
        double min = double.MaxValue;

        using SKPath path = new();
        bool penIsDown = false;
        for (double x = MinX; x <= MaxX; x += unitsPerPixel)
        {
            double y = Source.Get(x);
            if (y.IsInfiniteOrNaN())
            {
                penIsDown = false; // Picking up pen allows us to skip over regions where the function is undefined
                continue;
            }

            max = Math.Max(max, y);
            min = Math.Min(min, y);

            Pixel px = Axes.GetPixel(new(x, y));

            if (penIsDown)
            {
                path.LineTo(px.ToSKPoint());
            }
            else
            {
                path.MoveTo(px.ToSKPoint());
                penIsDown = true;
            }
        }

        using SKPaint paint = new();
        LineStyle.ApplyToPaint(paint);

        rp.Canvas.DrawPath(path, paint);

        bool somethingWasRendered = min <= max;
        if (somethingWasRendered)
        {
            if (min < MaxObservedRangeY.Min)
                MaxObservedRangeY = MaxObservedRangeY.Expanded(min);

            if (max > MaxObservedRangeY.Max)
                MaxObservedRangeY = MaxObservedRangeY.Expanded(max);
        }

        LastRenderHorizontalSpan = Axes.XAxis.Range.ToCoordinateRange;
    }
}
