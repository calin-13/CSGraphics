using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace CurbeParametrice;

public partial class MainWindow : Window
{
    private readonly List<CurbaParametrica> _curbe;
    private readonly List<(string desc, Transformare t)> _transformari = new();

    public MainWindow()
    {
        InitializeComponent();

        _curbe = CurbaParametrica.Predefinite();
        foreach (var c in _curbe)
            ComboCurbe.Items.Add(c.Nume);
        ComboCurbe.SelectedIndex = 0;

        ComboTransformare.Items.Add("Translatie");
        ComboTransformare.Items.Add("Scalare");
        ComboTransformare.Items.Add("Rotatie");
        ComboTransformare.Items.Add("Simetrie Ox");
        ComboTransformare.Items.Add("Simetrie Oy");
        ComboTransformare.Items.Add("Simetrie Origine");
        ComboTransformare.SelectedIndex = 0;
        ComboTransformare.SelectionChanged += (_, _) => ActualizeazaParametri();

        BtnAdauga.Click += (_, _) => AdaugaTransformare();
        BtnSterge.Click += (_, _) => StergeTransformare();
        BtnDeseneaza.Click += (_, _) => Deseneaza();
        BtnReset.Click += (_, _) => ResetTransformari();

        CanvasDesenar.SizeChanged += (_, _) => Deseneaza();

        ActualizeazaParametri();
    }

    private void ActualizeazaParametri()
    {
        int sel = ComboTransformare.SelectedIndex;
        switch (sel)
        {
            case 0:
                LblParam1.Text = "tx:"; LblParam2.Text = "ty:";
                TxtParam1.Text = "50"; TxtParam2.Text = "50";
                LblParam1.IsVisible = TxtParam1.IsVisible = true;
                LblParam2.IsVisible = TxtParam2.IsVisible = true;
                break;
            case 1:
                LblParam1.Text = "sx:"; LblParam2.Text = "sy:";
                TxtParam1.Text = "2"; TxtParam2.Text = "2";
                LblParam1.IsVisible = TxtParam1.IsVisible = true;
                LblParam2.IsVisible = TxtParam2.IsVisible = true;
                break;
            case 2:
                LblParam1.Text = "unghi(°):"; TxtParam1.Text = "45";
                LblParam1.IsVisible = TxtParam1.IsVisible = true;
                LblParam2.IsVisible = TxtParam2.IsVisible = false;
                break;
            default:
                LblParam1.IsVisible = TxtParam1.IsVisible = false;
                LblParam2.IsVisible = TxtParam2.IsVisible = false;
                break;
        }
    }

    private void AdaugaTransformare()
    {
        int sel = ComboTransformare.SelectedIndex;
        Transformare t;
        string desc;

        try
        {
            switch (sel)
            {
                case 0:
                    double tx = double.Parse(TxtParam1.Text!, CultureInfo.InvariantCulture);
                    double ty = double.Parse(TxtParam2.Text!, CultureInfo.InvariantCulture);
                    t = Transformare.Translatie(tx, ty);
                    desc = $"Translatie({tx}, {ty})";
                    break;
                case 1:
                    double sx = double.Parse(TxtParam1.Text!, CultureInfo.InvariantCulture);
                    double sy = double.Parse(TxtParam2.Text!, CultureInfo.InvariantCulture);
                    t = Transformare.Scalare(sx, sy);
                    desc = $"Scalare({sx}, {sy})";
                    break;
                case 2:
                    double unghi = double.Parse(TxtParam1.Text!, CultureInfo.InvariantCulture);
                    t = Transformare.Rotatie(unghi);
                    desc = $"Rotatie({unghi}°)";
                    break;
                case 3:
                    t = Transformare.SimetrieOx();
                    desc = "Simetrie Ox";
                    break;
                case 4:
                    t = Transformare.SimetrieOy();
                    desc = "Simetrie Oy";
                    break;
                case 5:
                    t = Transformare.SimetrieOrigine();
                    desc = "Simetrie Origine";
                    break;
                default: return;
            }
        }
        catch
        {
            return;
        }

        _transformari.Add((desc, t));
        RefreshLista();
    }

    private void StergeTransformare()
    {
        int idx = ListTransformari.SelectedIndex;
        if (idx < 0) return;
        _transformari.RemoveAt(idx);
        RefreshLista();
    }

    private void ResetTransformari()
    {
        _transformari.Clear();
        RefreshLista();
        Deseneaza();
    }

    private void RefreshLista()
    {
        ListTransformari.Items.Clear();
        for (int i = 0; i < _transformari.Count; i++)
            ListTransformari.Items.Add($"{i + 1}. {_transformari[i].desc}");
    }

    private void Deseneaza()
    {
        CanvasDesenar.Children.Clear();

        double w = CanvasDesenar.Bounds.Width;
        double h = CanvasDesenar.Bounds.Height;
        if (w < 10 || h < 10) return;

        int idxCurba = ComboCurbe.SelectedIndex;
        if (idxCurba < 0) return;
        var curba = _curbe[idxCurba];

        int n = (int)(NumRezolutie.Value ?? 500);
        var (xs, ys) = curba.Genereaza(n);

        var compusa = new Transformare();
        foreach (var (_, t) in _transformari)
            compusa = t * compusa;

        double[] txs = new double[n], tys = new double[n];
        for (int i = 0; i < n; i++)
            (txs[i], tys[i]) = compusa.Aplica(xs[i], ys[i]);

        bool afisOriginal = ChkOriginal.IsChecked == true && _transformari.Count > 0;

        double minX = txs.Min(), maxX = txs.Max();
        double minY = tys.Min(), maxY = tys.Max();
        if (afisOriginal)
        {
            minX = Math.Min(minX, xs.Min()); maxX = Math.Max(maxX, xs.Max());
            minY = Math.Min(minY, ys.Min()); maxY = Math.Max(maxY, ys.Max());
        }
        minX = Math.Min(minX, 0); maxX = Math.Max(maxX, 0);
        minY = Math.Min(minY, 0); maxY = Math.Max(maxY, 0);

        double rangeX = maxX - minX; if (rangeX < 1e-9) rangeX = 1;
        double rangeY = maxY - minY; if (rangeY < 1e-9) rangeY = 1;

        double margin = 0.12;
        double scale = Math.Min(w * (1 - 2 * margin) / rangeX, h * (1 - 2 * margin) / rangeY);
        double cx = w / 2.0 - (minX + maxX) / 2.0 * scale;
        double cy = h / 2.0 + (minY + maxY) / 2.0 * scale;

        Point ToScreen(double x, double y) => new(cx + x * scale, cy - y * scale);

        var axeColor = new SolidColorBrush(Color.FromRgb(140, 140, 140));
        var gridColor = new SolidColorBrush(Color.FromArgb(60, 180, 180, 180));
        var orig = ToScreen(0, 0);

        CanvasDesenar.Children.Add(new Line { StartPoint = new Point(0, orig.Y), EndPoint = new Point(w, orig.Y), Stroke = axeColor, StrokeThickness = 1.2 });
        CanvasDesenar.Children.Add(new Line { StartPoint = new Point(orig.X, 0), EndPoint = new Point(orig.X, h), Stroke = axeColor, StrokeThickness = 1.2 });

        CanvasDesenar.Children.Add(new Line { StartPoint = new Point(w - 12, orig.Y - 4), EndPoint = new Point(w, orig.Y), Stroke = axeColor, StrokeThickness = 1.2 });
        CanvasDesenar.Children.Add(new Line { StartPoint = new Point(w - 12, orig.Y + 4), EndPoint = new Point(w, orig.Y), Stroke = axeColor, StrokeThickness = 1.2 });
        CanvasDesenar.Children.Add(new Line { StartPoint = new Point(orig.X - 4, 12), EndPoint = new Point(orig.X, 0), Stroke = axeColor, StrokeThickness = 1.2 });
        CanvasDesenar.Children.Add(new Line { StartPoint = new Point(orig.X + 4, 12), EndPoint = new Point(orig.X, 0), Stroke = axeColor, StrokeThickness = 1.2 });

        double stepX = CalculeazaPas(rangeX);
        for (double v = Math.Ceiling(minX / stepX) * stepX; v <= maxX; v += stepX)
        {
            if (Math.Abs(v) < stepX * 0.01) continue;
            var p = ToScreen(v, 0);
            CanvasDesenar.Children.Add(new Line { StartPoint = new Point(p.X, 0), EndPoint = new Point(p.X, h), Stroke = gridColor, StrokeThickness = 0.5 });
            CanvasDesenar.Children.Add(new Line { StartPoint = new Point(p.X, orig.Y - 3), EndPoint = new Point(p.X, orig.Y + 3), Stroke = axeColor, StrokeThickness = 1 });
            var lbl = new TextBlock { Text = v.ToString("G3"), FontSize = 10, Foreground = axeColor };
            Canvas.SetLeft(lbl, p.X + 2); Canvas.SetTop(lbl, orig.Y + 5);
            CanvasDesenar.Children.Add(lbl);
        }

        double stepY = CalculeazaPas(rangeY);
        for (double v = Math.Ceiling(minY / stepY) * stepY; v <= maxY; v += stepY)
        {
            if (Math.Abs(v) < stepY * 0.01) continue;
            var p = ToScreen(0, v);
            CanvasDesenar.Children.Add(new Line { StartPoint = new Point(0, p.Y), EndPoint = new Point(w, p.Y), Stroke = gridColor, StrokeThickness = 0.5 });
            CanvasDesenar.Children.Add(new Line { StartPoint = new Point(orig.X - 3, p.Y), EndPoint = new Point(orig.X + 3, p.Y), Stroke = axeColor, StrokeThickness = 1 });
            var lbl = new TextBlock { Text = v.ToString("G3"), FontSize = 10, Foreground = axeColor };
            Canvas.SetLeft(lbl, orig.X + 5); Canvas.SetTop(lbl, p.Y - 8);
            CanvasDesenar.Children.Add(lbl);
        }

        var lblO = new TextBlock { Text = "O", FontSize = 10, Foreground = axeColor };
        Canvas.SetLeft(lblO, orig.X + 4); Canvas.SetTop(lblO, orig.Y + 4);
        CanvasDesenar.Children.Add(lblO);

        if (afisOriginal)
        {
            var geomOrig = new StreamGeometry();
            using (var ctx = geomOrig.Open())
            {
                ctx.BeginFigure(ToScreen(xs[0], ys[0]), false);
                for (int i = 1; i < n; i++)
                    ctx.LineTo(ToScreen(xs[i], ys[i]));
            }
            CanvasDesenar.Children.Add(new Avalonia.Controls.Shapes.Path
            {
                Data = geomOrig,
                Stroke = new SolidColorBrush(Color.FromArgb(140, 180, 180, 220)),
                StrokeThickness = 2,
                StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 3 }
            });

            var lblOrig = new TextBlock { Text = "--- Curba originala", FontSize = 11, FontStyle = FontStyle.Italic, Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 200)) };
            Canvas.SetLeft(lblOrig, 10); Canvas.SetTop(lblOrig, h - 50);
            CanvasDesenar.Children.Add(lblOrig);
        }

        var geomT = new StreamGeometry();
        using (var ctx = geomT.Open())
        {
            ctx.BeginFigure(ToScreen(txs[0], tys[0]), false);
            for (int i = 1; i < n; i++)
                ctx.LineTo(ToScreen(txs[i], tys[i]));
        }
        CanvasDesenar.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Data = geomT,
            Stroke = new SolidColorBrush(Color.FromRgb(30, 80, 200)),
            StrokeThickness = 2.5
        });

        string legendT = _transformari.Count > 0 ? "— Curba transformata" : "— Curba (fara transformari)";
        var lblT = new TextBlock { Text = legendT, FontSize = 11, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromRgb(30, 80, 200)) };
        Canvas.SetLeft(lblT, 10); Canvas.SetTop(lblT, h - 28);
        CanvasDesenar.Children.Add(lblT);

        var lblTitlu = new TextBlock { Text = curba.Nume, FontSize = 13, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 80)) };
        Canvas.SetLeft(lblTitlu, 10); Canvas.SetTop(lblTitlu, 8);
        CanvasDesenar.Children.Add(lblTitlu);

        if (_transformari.Count > 0)
        {
            double ty2 = 30;
            var infoColor = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            var hdr = new TextBlock { Text = "Transformari aplicate:", FontSize = 10, Foreground = infoColor };
            Canvas.SetLeft(hdr, 10); Canvas.SetTop(hdr, ty2);
            CanvasDesenar.Children.Add(hdr);
            ty2 += 16;
            foreach (var (desc, _) in _transformari)
            {
                var item = new TextBlock { Text = "• " + desc, FontSize = 10, Foreground = infoColor };
                Canvas.SetLeft(item, 14); Canvas.SetTop(item, ty2);
                CanvasDesenar.Children.Add(item);
                ty2 += 15;
            }
        }
    }

    private static double CalculeazaPas(double range)
    {
        double raw = range / 8.0;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag;
        if (norm < 1.5) return mag;
        if (norm < 3.5) return 2 * mag;
        if (norm < 7.5) return 5 * mag;
        return 10 * mag;
    }
}
