using System.Globalization;

namespace Blobatar.Net.Core;

public enum SegmentKind { Move, Line, Cubic, Quad, Close }

/// <summary>
/// One path segment in absolute coordinates. Cubic: (X1,Y1) and (X2,Y2) are the control points.
/// Quad: (X1,Y1) is the control point. Move and Line use only (X,Y).
/// </summary>
public readonly record struct Segment(SegmentKind Kind, double X1, double Y1, double X2, double Y2, double X, double Y);

/// <summary>
/// Reads the path strings Blobatar itself writes: absolute M, L, H, V, C, Q and Z, each command written out.
/// Lets a UI framework build native geometry without relying on a path-markup parser.
/// </summary>
public static class PathData
{
    public static IReadOnlyList<Segment> Parse(string d)
    {
        var segs = new List<Segment>();
        double cx = 0, cy = 0, sx = 0, sy = 0;
        var i = 0;
        while (i < d.Length)
        {
            var c = d[i++];
            switch (c)
            {
                case ' ': case ',': break;
                case 'M':
                    (cx, cy) = (Num(d, ref i), Num(d, ref i));
                    (sx, sy) = (cx, cy);
                    segs.Add(new(SegmentKind.Move, 0, 0, 0, 0, cx, cy));
                    break;
                case 'L':
                    (cx, cy) = (Num(d, ref i), Num(d, ref i));
                    segs.Add(new(SegmentKind.Line, 0, 0, 0, 0, cx, cy));
                    break;
                case 'H':
                    cx = Num(d, ref i);
                    segs.Add(new(SegmentKind.Line, 0, 0, 0, 0, cx, cy));
                    break;
                case 'V':
                    cy = Num(d, ref i);
                    segs.Add(new(SegmentKind.Line, 0, 0, 0, 0, cx, cy));
                    break;
                case 'C':
                {
                    double x1 = Num(d, ref i), y1 = Num(d, ref i), x2 = Num(d, ref i), y2 = Num(d, ref i);
                    (cx, cy) = (Num(d, ref i), Num(d, ref i));
                    segs.Add(new(SegmentKind.Cubic, x1, y1, x2, y2, cx, cy));
                    break;
                }
                case 'Q':
                {
                    double x1 = Num(d, ref i), y1 = Num(d, ref i);
                    (cx, cy) = (Num(d, ref i), Num(d, ref i));
                    segs.Add(new(SegmentKind.Quad, x1, y1, 0, 0, cx, cy));
                    break;
                }
                case 'Z':
                    (cx, cy) = (sx, sy);
                    segs.Add(new(SegmentKind.Close, 0, 0, 0, 0, sx, sy));
                    break;
                default:
                    throw new FormatException($"Unsupported path command '{c}' at {i - 1}.");
            }
        }
        return segs;
    }

    static double Num(string d, ref int i)
    {
        while (i < d.Length && (d[i] == ' ' || d[i] == ',')) i++;
        var s = i;
        if (i < d.Length && (d[i] == '-' || d[i] == '+')) i++;
        while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '.')) i++;
        if (i < d.Length && (d[i] == 'e' || d[i] == 'E'))
        {
            i++;
            if (i < d.Length && (d[i] == '-' || d[i] == '+')) i++;
            while (i < d.Length && char.IsDigit(d[i])) i++;
        }
        if (i == s) throw new FormatException($"Number expected at {s}.");
        return double.Parse(d.AsSpan(s, i - s), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
