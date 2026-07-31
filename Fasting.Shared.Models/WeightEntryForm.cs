using System.ComponentModel.DataAnnotations;

namespace Fasting.Models;

public sealed class WeightEntryForm
{
    [Required]
    public DateTime RecordedAt { get; set; } =
        DateTime.Now;

    [Range(
        20,
        500,
        ErrorMessage = "Weight must be between 20 and 500 kg.")]
    public double WeightKg { get; set; }

    [StringLength(
        250,
        ErrorMessage = "Notes cannot exceed 250 characters.")]
    public string Notes { get; set; } = string.Empty;
}