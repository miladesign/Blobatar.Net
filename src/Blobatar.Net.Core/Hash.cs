using System.Text;

namespace Blobatar.Net.Core;

/// <summary>
/// Seed hashing: murmur3-style avalanche, streamed per trait key so adding a
/// trait later never disturbs existing blobatars. Bit-exact with the JS source.
/// </summary>
public static class Hash
{
    static readonly byte[] Sep = { 0xff };

    static uint Feed(uint h, ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            foreach (var b in bytes)
            {
                h = (h ^ b) * 3432918353u;
                h = (h << 13) | (h >> 19);
            }
        }
        return h;
    }

    static uint Finalize(uint h)
    {
        unchecked
        {
            h = (h ^ (h >> 16)) * 2246822507u;
            h = (h ^ (h >> 13)) * 3266489909u;
            return h ^ (h >> 16);
        }
    }

    /// <summary>NFC, trim, lowercase: inputs a human considers equal hash equally.</summary>
    public static string NormalizeSeed(string seed) =>
        seed.Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();

    public static uint SeedState(string seed, bool normalize = true)
    {
        var s = normalize ? NormalizeSeed(seed) : seed;
        return Feed(1779033703u ^ (uint)s.Length, Encoding.UTF8.GetBytes(s));
    }

    /// <summary>Uniform double in [0, 1) for <paramref name="key"/>, independent of other keys.</summary>
    public static double Stream(uint state, string key) =>
        Finalize(Feed(Feed(state, Sep), Encoding.UTF8.GetBytes(key))) / 4294967296.0;
}
