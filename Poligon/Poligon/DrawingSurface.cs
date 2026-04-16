using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Poligon;

public class DrawingSurface : Control
{
    private readonly List<Point> _vertices = new();
    private bool _finalized;
    private bool _translating;
    private Point _lastDragPos;

    private WriteableBitmap? _fillBitmap;
    private int _fillOriginX;
    private int _fillOriginY;

    private const double VertexRadius = 6.0;
    private const double HitRadius    = 12.0;

    // ---------------- Mouse ----------------

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var cp = e.GetCurrentPoint(this);
        var pos = cp.Position;

        if (cp.Properties.IsRightButtonPressed)
        {
            _vertices.Clear();
            _finalized = false;
            _translating = false;
            _fillBitmap = null;
            InvalidateVisual();
            return;
        }

        if (!cp.Properties.IsLeftButtonPressed) return;

        if (!_finalized)
        {
            // Inchide poligonul daca s-a apasat pe primul varf
            if (_vertices.Count >= 3 && Distance(pos, _vertices[0]) <= HitRadius)
            {
                if (CanCloseWithoutSelfIntersection())
                {
                    _finalized = true;
                    RebuildFill();
                }
            }
            else if (CanAddVertex(pos))
            {
                _vertices.Add(pos);
            }
            InvalidateVisual();
        }
        else
        {
            // Incepe translatia daca s-a apasat in interior
            if (Geometry.PunctInteriorPoligon(pos, _vertices))
            {
                _translating = true;
                _lastDragPos = pos;
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_translating) return;

        var pos = e.GetPosition(this);
        double dx = pos.X - _lastDragPos.X;
        double dy = pos.Y - _lastDragPos.Y;
        for (int i = 0; i < _vertices.Count; i++)
            _vertices[i] = new Point(_vertices[i].X + dx, _vertices[i].Y + dy);
        _lastDragPos = pos;

        RebuildFill();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _translating = false;
    }

    // ---------------- Validari simplitate ----------------

    // Verific ca noua latura (ultimul varf -> pos) nu taie nicio latura existenta.
    private bool CanAddVertex(Point pos)
    {
        int n = _vertices.Count;
        if (n < 2) return true;

        Point A = _vertices[n - 1];
        Point B = pos;
        // Laturile existente sunt v[i]->v[i+1] pentru i = 0..n-2
        // Exclud ultima latura (v[n-2]->v[n-1]) care partajeaza varful A
        for (int i = 0; i < n - 2; i++)
            if (Geometry.IntersectieSegmente(A, B, _vertices[i], _vertices[i + 1]))
                return false;
        return true;
    }

    // La inchidere, latura finala v[n-1] -> v[0] nu trebuie sa taie nicio latura
    // non-adiacenta (exclud v[0]->v[1] si v[n-2]->v[n-1]).
    private bool CanCloseWithoutSelfIntersection()
    {
        int n = _vertices.Count;
        Point A = _vertices[n - 1];
        Point B = _vertices[0];
        for (int i = 1; i < n - 2; i++)
            if (Geometry.IntersectieSegmente(A, B, _vertices[i], _vertices[i + 1]))
                return false;
        return true;
    }

    // ---------------- Umplere interior ----------------

    private void RebuildFill()
    {
        if (!_finalized || _vertices.Count < 3)
        {
            _fillBitmap = null;
            return;
        }

        // Bounding box
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var v in _vertices)
        {
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
        }

        int x0 = (int)Math.Floor(minX);
        int y0 = (int)Math.Floor(minY);
        int x1 = (int)Math.Ceiling(maxX);
        int y1 = (int)Math.Ceiling(maxY);
        int w = Math.Max(1, x1 - x0 + 1);
        int h = Math.Max(1, y1 - y0 + 1);

        var bmp = new WriteableBitmap(
            new PixelSize(w, h),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        // Bgra8888 memory layout: B, G, R, A (little endian byte order)
        // Verde (LimeGreen-ish) cu alpha plin: R=50, G=205, B=50, A=255
        byte[] buffer = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Testam centrul pixelului (x0+x+0.5, y0+y+0.5)
                var testPoint = new Point(x0 + x + 0.5, y0 + y + 0.5);
                if (Geometry.PunctInteriorPoligon(testPoint, _vertices))
                {
                    int o = (y * w + x) * 4;
                    buffer[o + 0] =  50; // B
                    buffer[o + 1] = 205; // G
                    buffer[o + 2] =  50; // R
                    buffer[o + 3] = 255; // A
                }
                // altfel lasam 0 (transparent)
            }
        }

        using (var fb = bmp.Lock())
        {
            int stride = fb.RowBytes;
            for (int y = 0; y < h; y++)
                Marshal.Copy(buffer, y * w * 4, fb.Address + y * stride, w * 4);
        }

        _fillBitmap = bmp;
        _fillOriginX = x0;
        _fillOriginY = y0;
    }

    // ---------------- Render ----------------

    public override void Render(DrawingContext ctx)
    {
        ctx.FillRectangle(Brushes.WhiteSmoke, new Rect(Bounds.Size));

        // Interior colorat
        if (_fillBitmap != null)
        {
            ctx.DrawImage(_fillBitmap,
                new Rect(_fillOriginX, _fillOriginY,
                         _fillBitmap.PixelSize.Width, _fillBitmap.PixelSize.Height));
        }

        // Laturi
        int n = _vertices.Count;
        if (n >= 2)
        {
            var edgePen = new Pen(Brushes.SteelBlue, 2);
            for (int i = 0; i < n - 1; i++)
                ctx.DrawLine(edgePen, _vertices[i], _vertices[i + 1]);
            if (_finalized && n >= 3)
                ctx.DrawLine(edgePen, _vertices[n - 1], _vertices[0]);
        }

        // Varfuri (primul varf evidentiat portocaliu inainte de inchidere)
        var vertexPen = new Pen(Brushes.DarkRed, 1.5);
        for (int i = 0; i < n; i++)
        {
            IBrush brush = (!_finalized && i == 0 && n >= 3)
                ? Brushes.Orange
                : Brushes.Red;
            ctx.DrawEllipse(brush, vertexPen, _vertices[i], VertexRadius, VertexRadius);
        }
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
