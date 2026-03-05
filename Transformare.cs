using System;

namespace CurbeParametrice;

public class Transformare
{
    private readonly double[,] _m = new double[3, 3];

    public Transformare()
    {
        _m[0, 0] = 1; _m[1, 1] = 1; _m[2, 2] = 1;
    }

    private Transformare(double[,] m)
    {
        _m = (double[,])m.Clone();
    }

    public static Transformare Translatie(double tx, double ty) =>
        new(new double[,] { { 1, 0, tx }, { 0, 1, ty }, { 0, 0, 1 } });

    public static Transformare Scalare(double sx, double sy) =>
        new(new double[,] { { sx, 0, 0 }, { 0, sy, 0 }, { 0, 0, 1 } });

    public static Transformare Rotatie(double grade)
    {
        double r = grade * Math.PI / 180.0;
        double c = Math.Cos(r), s = Math.Sin(r);
        return new(new double[,] { { c, -s, 0 }, { s, c, 0 }, { 0, 0, 1 } });
    }

    public static Transformare SimetrieOx() =>
        new(new double[,] { { 1, 0, 0 }, { 0, -1, 0 }, { 0, 0, 1 } });

    public static Transformare SimetrieOy() =>
        new(new double[,] { { -1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } });

    public static Transformare SimetrieOrigine() =>
        new(new double[,] { { -1, 0, 0 }, { 0, -1, 0 }, { 0, 0, 1 } });

    public static Transformare operator *(Transformare a, Transformare b)
    {
        var r = new double[3, 3];
        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
        for (int k = 0; k < 3; k++)
            r[i, j] += a._m[i, k] * b._m[k, j];
        return new Transformare(r);
    }

    public (double x, double y) Aplica(double x, double y)
    {
        double rx = _m[0, 0] * x + _m[0, 1] * y + _m[0, 2];
        double ry = _m[1, 0] * x + _m[1, 1] * y + _m[1, 2];
        return (rx, ry);
    }
}
