using System.Text.Json;
using Blobatar.Net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blobatar.Net.Tests;

/// <summary>
/// golden.json holds SVG produced by the original JS (blobatar 2.7.0) for 254 seed/option
/// combinations. The C# port must reproduce every string exactly.
/// </summary>
[TestClass]
public class GoldenTests
{
    static BlobatarOptions ToOptions(JsonElement o)
    {
        var opts = new BlobatarOptions();
        foreach (var p in o.EnumerateObject())
        {
            opts = p.Name switch
            {
                "size" => opts with { Size = p.Value.GetInt32() },
                "hue" => opts with { Hue = p.Value.GetDouble() },
                "tone" => opts with { Tone = p.Value.GetDouble() },
                "contrast" => opts with { Contrast = p.Value.GetBoolean() },
                "normalize" => opts with { Normalize = p.Value.GetBoolean() },
                "background" => opts with { Background = Enum.Parse<BackdropStyle>(p.Value.GetString()!, true) },
                "traits" => opts with { Traits = p.Value.EnumerateObject().ToDictionary(
                    t => t.Name,
                    t => t.Value.ValueKind == JsonValueKind.Array
                        ? t.Value.EnumerateArray().Select(x => x.GetDouble()).ToArray()
                        : new[] { t.Value.GetDouble() }) },
                _ => throw new NotSupportedException(p.Name),
            };
        }
        return opts;
    }

    [TestMethod]
    public void Svg_matches_js_output_exactly()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "golden.json")));
        var failures = new List<string>();
        foreach (var c in doc.RootElement.EnumerateArray())
        {
            var seed = c.GetProperty("seed").GetString()!;
            var opts = ToOptions(c.GetProperty("opts"));
            var expected = c.GetProperty("svg").GetString()!;
            var actual = BlobatarSvg.Render(seed, opts);
            if (actual != expected) failures.Add($"seed '{seed}' opts {c.GetProperty("opts")}\n  js: {expected}\n  c#: {actual}");
        }
        Assert.IsTrue(failures.Count == 0, $"{failures.Count} mismatches:\n" + string.Join("\n", failures.Take(3)));
    }

    [TestMethod]
    public void Corpus_covers_all_ten_shapes()
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < 2000; i++) seen.Add(BlobatarFactory.Create("seed" + i).Layout.Shape);
        Assert.AreEqual(10, seen.Count);
    }

    [TestMethod]
    public void Equivalent_seeds_render_identically()
    {
        Assert.AreEqual(BlobatarSvg.Render("alain@x.com"), BlobatarSvg.Render("  Alain@X.com "));
        Assert.AreEqual(BlobatarSvg.Render("caf\u00e9"), BlobatarSvg.Render("cafe\u0301"));
        Assert.AreNotEqual(BlobatarSvg.Render("alain"), BlobatarSvg.Render("alaim"));
    }

    [TestMethod]
    public void Trait_overrides_are_clamped_and_ordered()
    {
        var t = new Traits("x", true, new Dictionary<string, double[]> { ["a"] = new[] { 5.0 }, ["b"] = new[] { -3.0 }, ["c"] = new[] { double.NaN } });
        Assert.AreEqual(0.999999, t["a"]);
        Assert.AreEqual(0.0, t["b"]);
        Assert.AreEqual(0.0, t["c"]);
    }

    [TestMethod]
    public void Eyes_stay_inside_the_frame()
    {
        for (var i = 0; i < 500; i++)
            foreach (var e in BlobatarFactory.Create("seed" + i).Layout.Eyes)
                Assert.IsTrue(e.Cx >= 0 && e.Cx <= 100);
    }
}
