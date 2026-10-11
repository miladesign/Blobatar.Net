using System.Text;

namespace Blobatar.Net.Core;

/// <summary>Static SVG output, identical to the JS <c>blobatar()</c> string API.</summary>
public static class BlobatarSvg
{
    public static string Render(string seed, BlobatarOptions? options = null)
    {
        var o = options ?? new BlobatarOptions();
        var m = BlobatarFactory.Create(seed, o);
        var dim = o.Size is { } s && s != 0 ? $" width=\"{s}\" height=\"{s}\"" : "";

        var sb = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"{dim}>");
        if (!string.IsNullOrEmpty(o.Title)) sb.Append("<title>").Append(Escape(o.Title)).Append("</title>");
        if (m.Backdrop is { } bg) sb.Append($"<path d=\"{bg.D}\" fill=\"{bg.Fill}\"/>");

        var eyeCount = m.Layout.Eyes.Length;
        if (m.Wrap.Length > 0) sb.Append($"<g transform=\"{m.Wrap}\">");
        sb.Append($"<g fill=\"{m.Palette.Head}\">");
        foreach (var k in m.Marks.Take(m.Marks.Count - eyeCount)) Append(sb, k);
        sb.Append($"</g><g fill=\"{m.Palette.Eye}\">");
        foreach (var k in m.Marks.Skip(m.Marks.Count - eyeCount)) Append(sb, k);
        sb.Append("</g>");
        if (m.Wrap.Length > 0) sb.Append("</g>");
        return sb.Append("</svg>").ToString();
    }

    static void Append(StringBuilder sb, Mark k) =>
        sb.Append(k.IsCircle
            ? $"<circle cx=\"{Geo.F(k.Cx)}\" cy=\"{Geo.F(k.Cy)}\" r=\"{Geo.F(k.R)}\"/>"
            : $"<path d=\"{k.D}\"/>");

    static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
