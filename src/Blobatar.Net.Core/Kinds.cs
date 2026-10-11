using System.Diagnostics;

namespace Blobatar.Net.Core;

/// <summary>The thirteen poses plus idle, in <see cref="Expressions.All"/> order. Shared by every UI binding.</summary>
public enum ExpressionKind { Idle, Happy, Sad, Mad, Surprised, Wink, Sleepy, Smug, Unsure, Scared, Love, Shy, Sick, Thinking }

/// <summary>What the eyes follow while a control is animated.</summary>
public enum GazeMode
{
    /// <summary>Idle glances only.</summary>
    None,
    /// <summary>Follow the cursor while it is over the window.</summary>
    Pointer,
    /// <summary>Look straight ahead and keep the idle glances off.</summary>
    Rest,
}

/// <summary>One monotonic millisecond clock for every animated blobatar, so they stay in step.</summary>
public static class Clock
{
    static readonly Stopwatch Watch = Stopwatch.StartNew();
    public static double Now => Watch.Elapsed.TotalMilliseconds;
}
