namespace Blobatar.Net.Core;

public static partial class Gaze
{
    /// <summary>
    /// Surveys a neutral (unposed) model: the head is every mark but the eyes, which come last.
    /// Same measurement as the web driver, taken from the geometry instead of the DOM.
    /// </summary>
    public static GazeFace? Survey(BlobatarModel neutral)
    {
        var eyeCount = neutral.Layout.Eyes.Length;
        var headCount = neutral.Marks.Count - eyeCount;
        if (headCount <= 0 || eyeCount == 0) return null;

        var shapes = neutral.Marks.Select(PathShape.FromMark).ToArray();
        var head = shapes[0].Bounds;
        for (var i = 1; i < headCount; i++) head = Union(head, shapes[i].Bounds);
        var eyes = shapes.Skip(headCount).Select(s => s.Bounds).ToList();
        return Survey(head, eyes, (x, y) =>
        {
            for (var i = 0; i < headCount; i++) if (shapes[i].Contains(x, y)) return true;
            return false;
        });
    }

    static (double X, double Y, double W, double H) Union((double X, double Y, double W, double H) a, (double X, double Y, double W, double H) b)
    {
        var x0 = Math.Min(a.X, b.X);
        var y0 = Math.Min(a.Y, b.Y);
        return (x0, y0, Math.Max(a.X + a.W, b.X + b.W) - x0, Math.Max(a.Y + a.H, b.Y + b.H) - y0);
    }
}
