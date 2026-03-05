using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace TransformareApp;

public partial class MainWindow : Window
{
    internal List<Point> puncteOrig = new();
    internal Point[] puncteTransf = Array.Empty<Point>();
    internal Transformare tr = new();
    internal bool poligonInchis;

    private enum Mod { Rotatie, Translatie, Scalare, Simetrie }
    private Mod mod = Mod.Rotatie;

    internal bool dragging;
    private Point dragStart;
    private Transformare trBackup = new();
    internal Point centru;

    private Control drawSurface = null!;

    public MainWindow()
    {
        InitializeComponent();

        BtnRotatie.Click += (_, _) => SetMod(Mod.Rotatie);
        BtnTranslatie.Click += (_, _) => SetMod(Mod.Translatie);
        BtnScalare.Click += (_, _) => SetMod(Mod.Scalare);
        BtnSimetrie.Click += (_, _) => SetMod(Mod.Simetrie);
        BtnReset.Click += (_, _) => ResetTr();
        BtnSterge.Click += (_, _) => ClearAll();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var customCanvas = new DrawingSurface(this);
        var parent = (DockPanel)DrawCanvas.Parent!;
        parent.Children.Remove(DrawCanvas);
        parent.Children.Add(customCanvas);
        drawSurface = customCanvas;

        customCanvas.PointerPressed += OnPointerPressed;
        customCanvas.PointerMoved += OnPointerMoved;
        customCanvas.PointerReleased += (_, _) => dragging = false;

        KeyDown += OnKeyDown;
    }

    void SetMod(Mod m)
    {
        mod = m;
        string[] n = { "Rotatie (drag mouse)", "Translatie (drag mouse)",
                       "Scalare (drag sus/jos)", "Simetrie (click directie)" };
        LblMod.Text = "Mod: " + n[(int)m];
        drawSurface.InvalidateVisual();
    }

    void OnKeyDown(object? s, KeyEventArgs e)
    {
        if (!poligonInchis) return;
        switch (e.Key)
        {
            case Key.R: SetMod(Mod.Rotatie); break;
            case Key.T: SetMod(Mod.Translatie); break;
            case Key.S: SetMod(Mod.Scalare); break;
            case Key.D: SetMod(Mod.Simetrie); break;
            case Key.Escape: ResetTr(); break;
        }
    }

    void OnPointerPressed(object? s, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(drawSurface);
        var props = e.GetCurrentPoint(drawSurface).Properties;

        if (!poligonInchis)
        {
            if (props.IsLeftButtonPressed)
            {
                puncteOrig.Add(pos);
                drawSurface.InvalidateVisual();
            }
            else if (props.IsRightButtonPressed && puncteOrig.Count >= 3)
            {
                poligonInchis = true;
                RecalcTransf(); CalcCentru();
                LblInfo.Text = "Drag = transformare | Taste: R/T/S/D | Esc = reset";
                LblMod.Text = "Mod: Rotatie (drag mouse)";
                drawSurface.InvalidateVisual();
            }
        }
        else if (props.IsLeftButtonPressed)
        {
            if (mod == Mod.Simetrie)
            {
                double vx = pos.X - centru.X, vy = pos.Y - centru.Y;
                if (Math.Sqrt(vx * vx + vy * vy) >= 5)
                {
                    tr.SimetrieFataDeDreapta(centru.X, centru.Y, vx, vy);
                    RecalcTransf(); CalcCentru();
                    drawSurface.InvalidateVisual();
                }
            }
            else
            {
                dragging = true;
                dragStart = pos;
                trBackup = tr.Clone();
            }
        }
    }

    void OnPointerMoved(object? s, PointerEventArgs e)
    {
        if (!dragging || !poligonInchis) return;
        var pos = e.GetPosition(drawSurface);
        tr = trBackup.Clone();
        double dx = pos.X - dragStart.X, dy = pos.Y - dragStart.Y;

        switch (mod)
        {
            case Mod.Rotatie:
                tr.RotatieInJurulPunctului(centru.X, centru.Y,
                    dx * Math.PI / 180.0);
                break;
            case Mod.Translatie:
                tr.Translatie(dx, dy);
                break;
            case Mod.Scalare:
                double f = Math.Clamp(1.0 + (-dy) / 200.0, 0.1, 5.0);
                tr.ScalareInJurulPunctului(centru.X, centru.Y, f, f);
                break;
        }
        RecalcTransf(); CalcCentru();
        drawSurface.InvalidateVisual();
    }

    void RecalcTransf()
    {
        puncteTransf = tr.AplicaPePuncte(puncteOrig.ToArray());
    }

    void CalcCentru()
    {
        if (puncteTransf.Length == 0) return;
        double sx = 0, sy = 0;
        foreach (var p in puncteTransf) { sx += p.X; sy += p.Y; }
        centru = new Point(sx / puncteTransf.Length, sy / puncteTransf.Length);
    }

    void ResetTr()
    {
        tr = new Transformare();
        if (poligonInchis) { RecalcTransf(); CalcCentru(); }
        drawSurface?.InvalidateVisual();
    }

    void ClearAll()
    {
        puncteOrig.Clear();
        puncteTransf = Array.Empty<Point>();
        tr = new Transformare();
        poligonInchis = false;
        LblInfo.Text = "Click stanga = adauga punct | Click dreapta = inchide poligon";
        LblMod.Text = "Mod: Construire poligon";
        drawSurface?.InvalidateVisual();
    }
}

// ============================================================
// Control custom separat - NU mosteneste Canvas/Panel
// ============================================================
public class DrawingSurface : Control
{
    private readonly MainWindow _w;

    public DrawingSurface(MainWindow win)
    {
        _w = win;
        ClipToBounds = true;
    }

    public override void Render(DrawingContext ctx)
    {
        // Background alb
        ctx.DrawRectangle(Brushes.White, null, new Rect(Bounds.Size));

        if (!_w.poligonInchis)
        {
            var bluePen = new Pen(Brushes.Blue, 2);
            var pts = GetOrigPoints();
            for (int i = 0; i < pts.Count; i++)
            {
                ctx.DrawEllipse(Brushes.Blue, null, pts[i], 4, 4);
                if (i > 0)
                    ctx.DrawLine(bluePen, pts[i - 1], pts[i]);
            }
        }
        else
        {
            var origPts = GetOrigPoints().ToArray();
            var transPts = GetTransfPoints();

            if (transPts.Length >= 3)
            {
                // Ghost original
                var ghostPen = new Pen(Brushes.LightGray, 1,
                    new DashStyle(new double[] { 4, 4 }, 0));
                DrawPolygon(ctx, origPts, null, ghostPen);

                // Poligon transformat
                var fill = new SolidColorBrush(Color.FromArgb(50, 0, 100, 200));
                var stroke = new Pen(new SolidColorBrush(
                    Color.FromRgb(0, 80, 180)), 2.5);
                DrawPolygon(ctx, transPts, fill, stroke);

                // Varfuri
                foreach (var p in transPts)
                    ctx.DrawEllipse(Brushes.DarkBlue, null, p, 4, 4);

                // Centru
                ctx.DrawEllipse(Brushes.Red, null, _w.centru, 5, 5);

                // Matrice
                var fmt = new FormattedText(
                    "Matricea:\n" + _w.tr.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"), 12,
                    Brushes.DarkSlateGray);
                ctx.DrawText(fmt, new Point(Bounds.Width - 280, 10));
            }
        }
    }

    // Accesam datele prin reflection-free approach: facem campurile internal
    List<Point> GetOrigPoints() => _w.puncteOrig;
    Point[] GetTransfPoints() => _w.puncteTransf;

    void DrawPolygon(DrawingContext ctx, Point[] pts,
        IBrush? fill, IPen? stroke)
    {
        if (pts.Length < 3) return;
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(pts[0], fill != null);
            for (int i = 1; i < pts.Length; i++)
                gc.LineTo(pts[i]);
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(fill, stroke, geo);
    }
}