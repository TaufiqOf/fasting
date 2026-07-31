using System.ComponentModel.DataAnnotations;

namespace Fasting.Models;

public sealed class UserProfile
{
    [Range(
        50,
        250,
        ErrorMessage = "Height must be between 50 and 250 cm.")]
    public double HeightCm { get; set; }

    [Range(
        20,
        500,
        ErrorMessage = "Weight must be between 20 and 500 kg.")]
    public double WeightKg { get; set; }

    [Range(
        13,
        120,
        ErrorMessage = "Age must be between 13 and 120.")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Please select a gender.")]
    public string Gender { get; set; } = string.Empty;

    [Range(
        20,
        500,
        ErrorMessage = "Target weight must be between 20 and 500 kg.")]
    public double TargetWeightKg { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}