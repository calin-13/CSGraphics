using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

namespace CurbeInterpolare;

public partial class MainWindow : Window
{
    private readonly List<Point> _points = new();
    private bool _finalized;
    private int  _dragIndex = -1;

    private const double PointRadius  = 7;
    private const double HitRadius    = 18;
    private const int    CurveSamples = 800;

    private static readonly IBrush PointBrush     = new SolidColorBrush(Color.Parse("#e94560"));
    private static readonly IBrush PointDragBrush = new SolidColorBrush(Color.Parse("#ffbd39"));
    private static readonly IBrush CurveBrush     = new SolidColorBrush(Color.Parse("#00fff5"));
    private static readonly IBrush GridBrush      = new SolidColorBrush(Color.Parse("#1a3a5c"));

    public MainWindow()
    {
        InitializeComponent();
        DrawCanvas.PointerPressed  += Canvas_PointerPressed;
        DrawCanvas.PointerMoved    += Canvas_PointerMoved;
        DrawCanvas.PointerReleased += Canvas_PointerReleased;
        ResetButton.Click          += (_, _) => ResetAll();
        KeyDown += (_, e) => { if (e.Key == Key.R) ResetAll(); };
    }

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(DrawCanvas);
        var props = e.GetCurrentPoint(DrawCanvas).Properties;

        if (props.IsRightButtonPressed)
        {
            if (_points.Count >= 2 && !_finalized)
            {
                _finalized = true;
                StatusText.Text = "Polinom desenat! Trage punctele cu mouse-ul. R = reset.";
                Redraw();
            }
            return;
        }

        if (!props.IsLeftButtonPressed) return;

        if (_finalized)
        {
            _dragIndex = FindNearestPoint(pos);
            if (_dragIndex >= 0) Redraw();
            return;
        }

        if (_points.Count > 0 && pos.X <= _points[^1].X)
        {
            StatusText.Text = "⚠ X trebuie sa fie mai mare decat precedentul!";
            return;
        }

        _points.Add(pos);
        PointCountText.Text = $"Puncte: {_points.Count}";
        StatusText.Text = $"Punct ({pos.X:F0}, {pos.Y:F0}) adaugat. Click dreapta = deseneaza.";
        Redraw();
    }

    private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(DrawCanvas);
        CoordText.Text = $"({pos.X:F0}, {pos.Y:F0})";

        if (_dragIndex < 0) return;

        double minX = _dragIndex > 0 ? _points[_dragIndex - 1].X + 1 : 0;
        double maxX = _dragIndex < _points.Count - 1 ? _points[_dragIndex + 1].X - 1 : DrawCanvas.Bounds.Width;

        double newX = Math.Clamp(pos.X, minX, maxX);
        double newY = Math.Clamp(pos.Y, 0, DrawCanvas.Bounds.Height);

        _points[_dragIndex] = new Point(newX, newY);
        Redraw();
    }

    private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragIndex = -1;
        Redraw();
    }

    private void Redraw()
    {
        DrawCanvas.Children.Clear();
        DrawGrid();

        if (_finalized && _points.Count >= 2)
            DrawCurve();

        for (int i = 0; i < _points.Count; i++)
        {
            bool drag = i == _dragIndex;
            double r = drag ? PointRadius * 1.5 : PointRadius;
            var ellipse = new Ellipse
            {
                Width = r * 2, Height = r * 2,
                Fill = drag ? PointDragBrush : PointBrush,
                Stroke = Brushes.White,
                StrokeThickness = drag ? 2 : 1,
            };
            Canvas.SetLeft(ellipse, _points[i].X - r);
            Canvas.SetTop(ellipse, _points[i].Y - r);
            DrawCanvas.Children.Add(ellipse);

            var label = new TextBlock
            {
                Text = $"P{i}",
                Foreground = Brushes.White,
                FontSize = 11,
            };
            Canvas.SetLeft(label, _points[i].X - r - 2);
            Canvas.SetTop(label, _points[i].Y - r - 16);
            DrawCanvas.Children.Add(label);
        }
    }

    private void DrawGrid()
    {
        double w = DrawCanvas.Bounds.Width, h = DrawCanvas.Bounds.Height;
        if (w <= 0 || h <= 0) return;
        for (double x = 50; x < w; x += 50)
            DrawCanvas.Children.Add(new Line
            {
                StartPoint = new Point(x, 0), EndPoint = new Point(x, h),
                Stroke = GridBrush, StrokeThickness = 0.5,
            });
        for (double y = 50; y < h; y += 50)
            DrawCanvas.Children.Add(new Line
            {
                StartPoint = new Point(0, y), EndPoint = new Point(w, y),
                Stroke = GridBrush, StrokeThickness = 0.5,
            });
    }

    private void DrawCurve()
    {
        double[] xs = _points.Select(p => p.X).ToArray();
        double[] ys = _points.Select(p => p.Y).ToArray();
        double xMin = xs[0], xMax = xs[^1], range = xMax - xMin;
        if (range <= 0) return;

        var polyline = new Polyline
        {
            Stroke = CurveBrush, StrokeThickness = 2.5,
            StrokeLineCap = PenLineCap.Round,

        };

        for (int i = 0; i <= CurveSamples; i++)
        {
            double x = xMin + (double)i / CurveSamples * range;
            double y = LagrangeEval(xs, ys, x);
            y = Math.Clamp(y, -500, DrawCanvas.Bounds.Height + 500);
            polyline.Points.Add(new Point(x, y));
        }
        DrawCanvas.Children.Add(polyline);
    }

    private static double LagrangeEval(double[] xs, double[] ys, double x)
    {
        int n = xs.Length;
        double result = 0;
        for (int i = 0; i < n; i++)
        {
            double li = 1.0;
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                li *= (x - xs[j]) / (xs[i] - xs[j]);
            }
            result += ys[i] * li;
        }
        return result;
    }

    private int FindNearestPoint(Point pos)
    {
        for (int i = 0; i < _points.Count; i++)
        {
            double dx = pos.X - _points[i].X, dy = pos.Y - _points[i].Y;
            if (Math.Sqrt(dx * dx + dy * dy) <= HitRadius) return i;
        }
        return -1;
    }

    private void ResetAll()
    {
        _points.Clear();
        _finalized = false;
        _dragIndex = -1;
        DrawCanvas.Children.Clear();
        PointCountText.Text = "Puncte: 0";
        MethodText.Text = "Metoda: Lagrange";
        StatusText.Text = "Click stanga = adauga punct | Click dreapta = deseneaza polinom";
        CoordText.Text = "";
    }
}
