using System;
using System.Collections.Generic;

namespace CurbeParametrice;

public class CurbaParametrica
{
    public string Nume { get; }
    public Func<double, double> X { get; }
    public Func<double, double> Y { get; }
    public double A { get; }
    public double B { get; }

    public CurbaParametrica(string nume, Func<double, double> x, Func<double, double> y, double a, double b)
    {
        Nume = nume;
        X = x;
        Y = y;
        A = a;
        B = b;
    }

    public (double[] xs, double[] ys) Genereaza(int n)
    {
        var xs = new double[n];
        var ys = new double[n];
        double pas = (B - A) / (n - 1);
        for (int i = 0; i < n; i++)
        {
            double u = A + i * pas;
            xs[i] = X(u);
            ys[i] = Y(u);
        }
        return (xs, ys);
    }

    public override string ToString() => Nume;

    public static List<CurbaParametrica> Predefinite() => new()
    {
        new("Elipsa: x=cos(u), y=2sin(u), u in [0,2pi]",
            u => Math.Cos(u), u => 2 * Math.Sin(u), 0, 2 * Math.PI),

        new("Spirala 2D: x=u*cos(u), y=u*sin(u), u in [0,20]",
            u => u * Math.Cos(u), u => u * Math.Sin(u), 0, 20),

        new("Parabola: f(x)=x^2+1, x in [-2,2]",
            u => u, u => u * u + 1, -2, 2),

        new("Cerc: x=cos(u), y=sin(u), u in [0,2pi]",
            u => Math.Cos(u), u => Math.Sin(u), 0, 2 * Math.PI),

        new("Lemniscata Bernoulli",
            u => Math.Cos(u) / (1 + Math.Sin(u) * Math.Sin(u)),
            u => Math.Sin(u) * Math.Cos(u) / (1 + Math.Sin(u) * Math.Sin(u)),
            0, 2 * Math.PI),

        new("Astroida: x=cos^3(u), y=sin^3(u), u in [0,2pi]",
            u => Math.Pow(Math.Cos(u), 3), u => Math.Pow(Math.Sin(u), 3), 0, 2 * Math.PI),
    };
}
