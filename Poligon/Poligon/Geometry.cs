using Avalonia;
using System.Collections.Generic;

namespace Poligon;

/// <summary>
/// Biblioteca de functii pentru geometrie computationala.
/// Conventie axe: Y creste in jos (coordonate ecran), deci
///   F > 0 => cotire la dreapta vizual (stanga in sistem matematic)
///   F < 0 => cotire la stanga vizual
/// Semnele sunt folosite doar pentru comparatie (acelasi semn / semne diferite),
/// deci convenția nu afecteaza corectitudinea algoritmilor.
/// </summary>
public static class Geometry
{
    // 1. F(A, B, C)
    public static double F(Point A, Point B, Point C)
        => (B.X - A.X) * (C.Y - A.Y) - (B.Y - A.Y) * (C.X - A.X);

    // 2. sgn(A, B, C)
    public static int Sgn(Point A, Point B, Point C)
    {
        const double eps = 1e-9;
        double v = F(A, B, C);
        if (v >  eps) return  1;
        if (v < -eps) return -1;
        return 0;
    }

    // 3. punct_interior_triunghi(A, B, C, D) -> A in interiorul triunghiului BCD
    public static bool PunctInteriorTriunghi(Point A, Point B, Point C, Point D)
    {
        int s1 = Sgn(B, C, A);
        int s2 = Sgn(C, D, A);
        int s3 = Sgn(D, B, A);
        return s1 != 0 && s1 == s2 && s2 == s3;
    }

    // 4. semiplane_diferite(A, B, C, D) -> A si B de o parte si alta a dreptei (CD)
    public static bool SemiplaneDiferite(Point A, Point B, Point C, Point D)
    {
        int sA = Sgn(C, D, A);
        int sB = Sgn(C, D, B);
        return sA * sB < 0;
    }

    // 5. patrulater_convex(A, B, C, D)
    public static bool PatrulaterConvex(Point A, Point B, Point C, Point D)
    {
        int s1 = Sgn(A, B, C);
        int s2 = Sgn(B, C, D);
        int s3 = Sgn(C, D, A);
        int s4 = Sgn(D, A, B);
        return s1 != 0 && s1 == s2 && s2 == s3 && s3 == s4;
    }

    // 6. intersectie_segmente(A, B, C, D) -> AB intersecteaza CD (proper)
    public static bool IntersectieSegmente(Point A, Point B, Point C, Point D)
        => SemiplaneDiferite(A, B, C, D) && SemiplaneDiferite(C, D, A, B);

    // 7. poligon_convex(varfuri)
    public static bool PoligonConvex(IReadOnlyList<Point> v)
    {
        int n = v.Count;
        if (n < 3) return false;
        int refSign = 0;
        for (int i = 0; i < n; i++)
        {
            int s = Sgn(v[i], v[(i + 1) % n], v[(i + 2) % n]);
            if (s == 0) continue;
            if (refSign == 0) refSign = s;
            else if (s != refSign) return false;
        }
        return refSign != 0;
    }

    // 8. punct_interior_poligon(A, varfuri) -> O(n) ray casting
    // Trimitem o raza orizontala din A spre +infinit si numaram intersectiile
    // cu laturile. Impar => interior, par => exterior.
    public static bool PunctInteriorPoligon(Point A, IReadOnlyList<Point> v)
    {
        int n = v.Count;
        if (n < 3) return false;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Point pi = v[i], pj = v[j];
            // Latura traverseaza linia orizontala y = A.Y ?
            if ((pi.Y > A.Y) != (pj.Y > A.Y))
            {
                double xIntersect = (pj.X - pi.X) * (A.Y - pi.Y) / (pj.Y - pi.Y) + pi.X;
                if (A.X < xIntersect) inside = !inside;
            }
        }
        return inside;
    }
}
