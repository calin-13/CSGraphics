using Avalonia;
using System.Collections.Generic;

namespace CurbeleB_Spine;

/// <summary>
/// Calcul B-spline cu recursie Cox-de Boor pe knot vector clamped (uniform deschis).
/// Pentru n+1 puncte de control si grad d:
///   - vectorul de noduri are (n+1) + d + 1 elemente
///   - primele d+1 noduri = 0, ultimele d+1 noduri = 1
///   - (n - d) noduri interioare uniform distribuite in (0, 1)
/// Cand d = n (maxim), knot vector devine [0..0, 1..1] => curba Bezier.
/// </summary>
public static class BSpline
{
    public static List<Point> ComputeCurve(IReadOnlyList<Point> controlPoints, int degree, int samples)
    {
        var result = new List<Point>();
        int nCp = controlPoints.Count;
        if (nCp < degree + 1) return result;

        int order = degree + 1;
        double[] knots = BuildClampedKnots(nCp, degree);

        double tStart = knots[degree];
        double tEnd   = knots[nCp];

        for (int s = 0; s < samples; s++)
        {
            double t = tStart + (tEnd - tStart) * s / samples;
            double x = 0, y = 0;
            for (int i = 0; i < nCp; i++)
            {
                double b = BasisFunction(i, order, t, knots);
                x += controlPoints[i].X * b;
                y += controlPoints[i].Y * b;
            }
            result.Add(new Point(x, y));
        }

        // Capatul t = tEnd: curba clamped trece exact prin ultimul punct de control
        result.Add(controlPoints[nCp - 1]);
        return result;
    }

    private static double[] BuildClampedKnots(int nCp, int degree)
    {
        int total = nCp + degree + 1;
        var knots = new double[total];
        int interior = nCp - degree - 1;

        for (int i = 0; i <= degree; i++) knots[i] = 0.0;
        for (int i = total - degree - 1; i < total; i++) knots[i] = 1.0;
        for (int i = 1; i <= interior; i++)
            knots[degree + i] = (double)i / (interior + 1);

        return knots;
    }

    // Cox-de Boor: N_{i, order}(t)
    private static double BasisFunction(int i, int order, double t, double[] knots)
    {
        if (order == 1)
            return (t >= knots[i] && t < knots[i + 1]) ? 1.0 : 0.0;

        double left = 0, right = 0;
        double denomL = knots[i + order - 1] - knots[i];
        double denomR = knots[i + order]     - knots[i + 1];

        if (denomL > 1e-12)
            left  = (t - knots[i]) / denomL * BasisFunction(i, order - 1, t, knots);
        if (denomR > 1e-12)
            right = (knots[i + order] - t) / denomR * BasisFunction(i + 1, order - 1, t, knots);

        return left + right;
    }
}
