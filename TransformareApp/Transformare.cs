using System;
using Avalonia;

namespace TransformareApp;

public class Transformare
{
    public double[,] Mat { get; private set; }

    public Transformare()
    {
        Mat = new double[3, 3];
        SetIdentitate();
    }

    public Transformare(double[,] mat) => Mat = (double[,])mat.Clone();

    public void SetIdentitate()
    {
        Mat = new double[3, 3];
        Mat[0, 0] = 1; Mat[1, 1] = 1; Mat[2, 2] = 1;
    }

    public void InmultesteLaStanga(double[,] m)
    {
        var rez = new double[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    rez[i, j] += m[i, k] * Mat[k, j];
        Mat = rez;
    }

    public void Rotatie(double theta) => Rotatie(Math.Cos(theta), Math.Sin(theta));

    public void Rotatie(double cosA, double sinA)
    {
        double[,] r = {
            { cosA, -sinA, 0 },
            { sinA,  cosA, 0 },
            {    0,     0, 1 }
        };
        InmultesteLaStanga(r);
    }

    public void Translatie(double tx, double ty)
    {
        double[,] t = {
            { 1, 0, tx },
            { 0, 1, ty },
            { 0, 0,  1 }
        };
        InmultesteLaStanga(t);
    }

    public void SimetrieFataDeOx()
    {
        double[,] s = { { 1, 0, 0 }, { 0, -1, 0 }, { 0, 0, 1 } };
        InmultesteLaStanga(s);
    }

    public void SimetrieFataDeOy()
    {
        double[,] s = { { -1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
        InmultesteLaStanga(s);
    }

    public void SimetrieFataDeOrigine()
    {
        double[,] s = { { -1, 0, 0 }, { 0, -1, 0 }, { 0, 0, 1 } };
        InmultesteLaStanga(s);
    }

    public void Scalare(double sx, double sy)
    {
        double[,] sc = { { sx, 0, 0 }, { 0, sy, 0 }, { 0, 0, 1 } };
        InmultesteLaStanga(sc);
    }

    public void Scalare(double f) => Scalare(f, f);

    // R^A - Rotatie in jurul punctului A
    public void RotatieInJurulPunctului(double ax, double ay, double theta)
    {
        Translatie(-ax, -ay);
        Rotatie(theta);
        Translatie(ax, ay);
    }

    // S^A - Scalare in jurul punctului A
    public void ScalareInJurulPunctului(double ax, double ay, double sx, double sy)
    {
        Translatie(-ax, -ay);
        Scalare(sx, sy);
        Translatie(ax, ay);
    }

    // Simetrie fata de dreapta d(A, v)
    public void SimetrieFataDeDreapta(double ax, double ay, double vx, double vy)
    {
        double len = Math.Sqrt(vx * vx + vy * vy);
        if (len < 1e-10) return;
        double cosA = vx / len, sinA = vy / len;

        Translatie(-ax, -ay);
        Rotatie(cosA, -sinA);
        SimetrieFataDeOx();
        Rotatie(cosA, sinA);
        Translatie(ax, ay);
    }

    public Point AplicaPePunct(double x, double y)
    {
        double xN = Mat[0, 0] * x + Mat[0, 1] * y + Mat[0, 2];
        double yN = Mat[1, 0] * x + Mat[1, 1] * y + Mat[1, 2];
        return new Point(xN, yN);
    }

    public Point[] AplicaPePuncte(Point[] p)
    {
        var r = new Point[p.Length];
        for (int i = 0; i < p.Length; i++)
            r[i] = AplicaPePunct(p[i].X, p[i].Y);
        return r;
    }

    public Transformare Clone() => new((double[,])Mat.Clone());

    public override string ToString() =>
        $"[{Mat[0,0],8:F3} {Mat[0,1],8:F3} {Mat[0,2],8:F3}]\n" +
        $"[{Mat[1,0],8:F3} {Mat[1,1],8:F3} {Mat[1,2],8:F3}]\n" +
        $"[{Mat[2,0],8:F3} {Mat[2,1],8:F3} {Mat[2,2],8:F3}]";
}