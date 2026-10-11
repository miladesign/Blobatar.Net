using Godot;

// Register the library script separately from any project-local derived control.
// Its stable resource path lets Godot reuse the base script during editor reloads.
[assembly: AssemblyHasScripts(new[] { typeof(Blobatar.Net.Godot.BlobatarControl) })]

namespace Blobatar.Net.Godot;

/// <summary>Registers packaged scripts when Godot scans the game assembly.</summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class BlobatarScriptsAttribute : Attribute
{
    static readonly object RegistrationLock = new();
    static bool _registered;
    // Godot automatically scans only the game assembly. Scan the package when
    // it loads, including each fresh assembly context created by hot reload.
    public BlobatarScriptsAttribute()
    {
        lock (RegistrationLock)
        {
            if (_registered) return;
            global::Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(BlobatarControl).Assembly);
            _registered = true;
        }
    }
}
