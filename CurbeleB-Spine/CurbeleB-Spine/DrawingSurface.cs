using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections.Generic;

namespace CurbeleB_Spine;

public class DrawingSurface : Control
{
    private readonly List<Point> _controlPoints = new();
    private int _draggedIndex = -1;

    private const double PointRadius = 6.0;
    private const double HitRadius   = 10.0;

    private static readonly IBrush[] CurveBrushes =
    {
        Brushes.DarkOrange,
        Brushes.SeaGreen,
        Brushes.DarkRed,
        new SolidColorBrush(Color.Parse("#663399")),
        Brushes.Teal,
        Brushes.Magenta,
        Brushes.SaddleBrown,
        Brushes.MidnightBlue
    };

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var cp = e.GetCurrentPoint(this);
        var pos = cp.Position;

        if (cp.Properties.IsRightButtonPressed)
        {
            _controlPoints.Clear();
            _draggedIndex = -1;
            InvalidateVisual();
            return;
        }

        if (cp.Properties.IsLeftButtonPressed)
        {
            int idx = FindPointAt(pos);
            if (idx >= 0)
                _draggedIndex = idx;
            else
            {
                _controlPoints.Add(pos);
                InvalidateVisual();
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_draggedIndex >= 0)
        {
            _controlPoints[_draggedIndex] = e.GetPosition(this);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggedIndex = -1;
    }

    private int FindPointAt(Point pos)
    {
        for (int i = _controlPoints.Count - 1; i >= 0; i--)
        {
            double dx = _controlPoints[i].X - pos.X;
            double dy = _controlPoints[i].Y - pos.Y;
            if (dx * dx + dy * dy <= HitRadius * HitRadius)
                return i;
        }
        return -1;
    }

    public override void Render(DrawingContext ctx)
    {
        // Fundal + zona de hit-test pentru intreaga suprafata
        ctx.FillRectangle(Brushes.WhiteSmoke, new Rect(Bounds.Size));

        int nCp = _controlPoints.Count;

        if (nCp >= 2)
        {
            var polyPen = new Pen(Brushes.LightSkyBlue, 1.5);
            for (int i = 0; i < nCp - 1; i++)
                ctx.DrawLine(polyPen, _controlPoints[i], _controlPoints[i + 1]);
        }

        int n = nCp - 1;
        for (int k = 1; k <= n - 1; k++)
        {
            int degree = k + 1;
            var brush = CurveBrushes[(k - 1) % CurveBrushes.Length];
            var pen = new Pen(brush, 2);

            var samples = BSpline.ComputeCurve(_controlPoints, degree, 400);
            for (int i = 0; i < samples.Count - 1; i++)
                ctx.DrawLine(pen, samples[i], samples[i + 1]);
        }

        var pointPen = new Pen(Brushes.DarkRed, 1.5);
        foreach (var p in _controlPoints)
            ctx.DrawEllipse(Brushes.Red, pointPen, p, PointRadius, PointRadius);
    }
}
