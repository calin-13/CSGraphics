using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace CurbaCoons;

public partial class MainWindow : Window
{
    internal List<Point> puncte = new();
    internal bool finalizat;
    internal int dragIndex = -1;

    private Control drawSurface = null!;

    public MainWindow()
    {
        InitializeComponent();
        BtnReset.Click += (_, _) => ResetAll();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var surface = new DrawingSurface(this);
        var parent = (DockPanel)DrawCanvas.Parent!;
        parent.Children.Remove(DrawCanvas);
        parent.Children.Add(surface);
        drawSurface = surface;

        surface.PointerPressed += OnPointerPressed;
        surface.PointerMoved += OnPointerMoved;
        surface.PointerReleased += OnPointerReleased;
    }

    void ResetAll()
    {
        puncte.Clear();
        finalizat = false;
        dragIndex = -1;
        UpdateStatus();
        drawSurface.InvalidateVisual();
    }

    void UpdateStatus()
    {
        if (!finalizat)
            LblStatus.Text = $"Puncte: {puncte.Count} (trebuie nr. par, min. 4)";
        else
            LblStatus.Text = $"Finalizat - {puncte.Count} puncte, {puncte.Count / 2 - 1} segmente curba";
    }

    void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(drawSurface);
        var props = e.GetCurrentPoint(drawSurface).Properties;

        if (!finalizat)
        {
            if (props.IsRightButtonPressed)
            {
                if (puncte.Count >= 4 && puncte.Count % 2 == 0)
                {
                    finalizat = true;
                    UpdateStatus();
                    drawSurface.InvalidateVisual();
                }
                return;
            }

            puncte.Add(pos);
            UpdateStatus();
            drawSurface.InvalidateVisual();
        }
        else
        {
            if (props.IsLeftButtonPressed)
            {
                for (int i = 0; i < puncte.Count; i++)
                {
                    var d = puncte[i] - pos;
                    if (Math.Sqrt(d.X * d.X + d.Y * d.Y) < 12)
                    {
                        dragIndex = i;
                        return;
                    }
                }
            }
        }
    }

    void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragIndex < 0 || !finalizat) return;
        var pos = e.GetPosition(drawSurface);
        puncte[dragIndex] = pos;
        drawSurface.InvalidateVisual();
    }

    void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        dragIndex = -1;
    }
}

internal class DrawingSurface : Control
{
    private readonly MainWindow _w;

    public DrawingSurface(MainWindow win)
    {
        _w = win;
    }

    public override void Render(DrawingContext ctx)
    {
        ctx.DrawRectangle(Brushes.White, null, new Rect(Bounds.Size));

        var pts = _w.puncte;
        if (pts.Count == 0) return;

        if (!_w.finalizat)
            DrawInputMode(ctx, pts);
        else
            DrawCurves(ctx, pts);
    }

    void DrawInputMode(DrawingContext ctx, List<Point> pts)
    {
        for (int i = 0; i < pts.Count; i++)
        {
            bool isPunctCurba = (i % 2 == 0);

            if (isPunctCurba)
            {
                ctx.DrawEllipse(Brushes.DodgerBlue, null, pts[i], 6, 6);
                var label = $"P{i / 2}";
                var fmt = MakeText(label, 10, Brushes.DodgerBlue);
                ctx.DrawText(fmt, pts[i] + new Vector(8, -16));
            }
            else
            {
                ctx.DrawEllipse(Brushes.OrangeRed, null, pts[i], 5, 5);
                var baseIdx = i - 1;
                if (baseIdx >= 0)
                {
                    var tangPen = new Pen(Brushes.OrangeRed, 1.5,
                        new DashStyle(new double[] { 4, 3 }, 0));
                    ctx.DrawLine(tangPen, pts[baseIdx], pts[i]);
                    DrawArrow(ctx, pts[baseIdx], pts[i], Brushes.OrangeRed);
                }
                var label = $"t{i / 2}";
                var fmt = MakeText(label, 10, Brushes.OrangeRed);
                ctx.DrawText(fmt, pts[i] + new Vector(8, -16));
            }
        }

        var info = MakeText("Click stanga = punct nou | Click dreapta = finalizeaza",
            11, Brushes.Gray);
        ctx.DrawText(info, new Point(10, Bounds.Height - 25));
    }

    void DrawCurves(DrawingContext ctx, List<Point> pts)
    {
        int nPuncte = pts.Count / 2;
        var curvePen = new Pen(new SolidColorBrush(Color.FromRgb(30, 100, 220)), 2.5);

        for (int i = 0; i < nPuncte - 1; i++)
        {
            var A = pts[i * 2];
            var B = pts[(i + 1) * 2];
            var aVec = pts[i * 2 + 1] - pts[i * 2];
            var bVec = pts[(i + 1) * 2 + 1] - pts[(i + 1) * 2];

            double ax = aVec.X, ay = aVec.Y;
            double bx = bVec.X, by = bVec.Y;

            var prev = A;
            int steps = 100;
            for (int s = 1; s <= steps; s++)
            {
                double u = (double)s / steps;
                double u2 = u * u;
                double u3 = u2 * u;

                double F1 = 2 * u3 - 3 * u2 + 1;
                double F2 = -2 * u3 + 3 * u2;
                double F3 = u3 - 2 * u2 + u;
                double F4 = u3 - u2;

                double x = F1 * A.X + F2 * B.X + F3 * ax + F4 * bx;
                double y = F1 * A.Y + F2 * B.Y + F3 * ay + F4 * by;

                var cur = new Point(x, y);
                ctx.DrawLine(curvePen, prev, cur);
                prev = cur;
            }
        }

        for (int i = 0; i < nPuncte; i++)
        {
            var pCurba = pts[i * 2];
            var pTang = pts[i * 2 + 1];

            var tangPen = new Pen(Brushes.OrangeRed, 1.2,
                new DashStyle(new double[] { 4, 3 }, 0));
            ctx.DrawLine(tangPen, pCurba, pTang);
            DrawArrow(ctx, pCurba, pTang, Brushes.OrangeRed);

            bool isDragged = (_w.dragIndex == i * 2);
            var pColor = isDragged ? Brushes.Red : Brushes.DodgerBlue;
            ctx.DrawEllipse(pColor, null, pCurba, 6, 6);

            isDragged = (_w.dragIndex == i * 2 + 1);
            var tColor = isDragged ? Brushes.Red : Brushes.OrangeRed;
            ctx.DrawEllipse(tColor, null, pTang, 5, 5);

            var lbl1 = MakeText($"P{i}", 10, Brushes.DodgerBlue);
            ctx.DrawText(lbl1, pCurba + new Vector(8, -16));

            var lbl2 = MakeText($"t{i}", 9, Brushes.OrangeRed);
            ctx.DrawText(lbl2, pTang + new Vector(8, -14));
        }

        var info = MakeText("Drag pe un punct pentru a-l muta | Curba se redeseneaza local",
            11, Brushes.Gray);
        ctx.DrawText(info, new Point(10, Bounds.Height - 25));
    }

    void DrawArrow(DrawingContext ctx, Point from, Point to, IBrush brush)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 5) return;

        var ux = dx / len;
        var uy = dy / len;
        var arrowLen = 8.0;
        var arrowAngle = Math.PI / 6;

        var p1 = new Point(
            to.X - arrowLen * (ux * Math.Cos(arrowAngle) - uy * Math.Sin(arrowAngle)),
            to.Y - arrowLen * (uy * Math.Cos(arrowAngle) + ux * Math.Sin(arrowAngle)));
        var p2 = new Point(
            to.X - arrowLen * (ux * Math.Cos(arrowAngle) + uy * Math.Sin(arrowAngle)),
            to.Y - arrowLen * (uy * Math.Cos(arrowAngle) - ux * Math.Sin(arrowAngle)));

        var pen = new Pen(brush, 1.5);
        ctx.DrawLine(pen, to, p1);
        ctx.DrawLine(pen, to, p2);
    }

    static FormattedText MakeText(string text, double size, IBrush brush)
    {
        return new FormattedText(text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Normal),
            size, brush);
    }
}
