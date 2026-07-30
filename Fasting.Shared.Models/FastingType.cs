using System.ComponentModel;
using System.Runtime.Intrinsics.X86;

namespace Fasting.Models;

public class FastingType
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Hours spent fasting.
    /// </summary>
    public int FastingHours { get; init; }

    /// <summary>
    /// Hours available for eating.
    /// </summary>
    public int EatingHours { get; init; }

    /// <summary>
    /// True if this schedule doesn't follow a daily fasting/eating window.
    /// </summary>
    public bool IsCustomSchedule { get; init; }
}