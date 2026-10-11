namespace Blobatar.Net.Core;

/// <summary>
/// Trait reader. Every value is addressed by string key (append-only namespace).
/// <paramref name="overrides"/> pin a key to a 0-1 position; a list means
/// "any of these", chosen per seed by the key's own hash.
/// </summary>
public sealed class Traits
{
    readonly uint _state;
    readonly IReadOnlyDictionary<string, double[]>? _overrides;

    public Traits(string seed, bool normalize = true, IReadOnlyDictionary<string, double[]>? overrides = null)
    {
        _state = Hash.SeedState(seed, normalize);
        _overrides = overrides;
    }

    /// <summary>Uniform double in [0, 1).</summary>
    public double this[string key]
    {
        get
        {
            double? o = null;
            if (_overrides is not null && _overrides.TryGetValue(key, out var v) && v.Length > 0)
                o = v[(int)Math.Floor(Hash.Stream(_state, key) * v.Length)];
            if (o is null) return Hash.Stream(_state, key);
            var x = o.Value;
            return x > 0 ? (x < 1 ? x : 0.999999) : 0; // NaN falls to 0
        }
    }

    public double Num(string key, double min, double max) => min + this[key] * (max - min);
    public int Int(string key, int min, int max) => min + (int)Math.Floor(this[key] * (max - min + 1));
    public T Pick<T>(string key, IReadOnlyList<T> options) => options[(int)Math.Floor(this[key] * options.Count)];
    public bool Bool(string key, double p = 0.5) => this[key] < p;
    public double Jitter(string key, double amount) => (this[key] * 2 - 1) * amount;
}
