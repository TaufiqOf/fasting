using Microsoft.Maui.Graphics;
using Color = Microsoft.Maui.Graphics.Color;

namespace Fasting.Shared.Services;

public sealed class FastingTipHelper
{
    public string GetTip(double totalSeconds)
    {
        var inHour = Convert.ToInt32((totalSeconds / 60) / 60);
                 return GetFastingStage(inHour);
    }
    public string GetColor(double totalSeconds)
    {
        var inHour = Convert.ToInt32((totalSeconds / 60) / 60);
        return GetFastingStageColor(inHour);
    }

    private static string GetFastingStage(int hours) =>
        hours switch
        {
            >= 0 and < 4 =>
                "Stage 1: Your body mainly uses glucose from your recent meal for energy.",

            >= 4 and < 12 =>
                "Stage 2: Insulin levels begin to fall, and your body starts relying more on glycogen stored in the liver. Fat burning gradually increases.",

            >= 12 and < 24 =>
                "Stage 3: As liver glycogen becomes more depleted, your body shifts further toward burning fat. Fatty acids become a larger fuel source, and the liver begins producing small amounts of ketones.",

            >= 24 and < 48 =>
                "Stage 4: Fat burning increases substantially, and ketone production rises, especially for the brain.",

            >= 48 =>
                "Stage 5: Ketones become a significant fuel source, and your body adapts more to prolonged fasting.",

            _ => throw new ArgumentOutOfRangeException(nameof(hours), "Hours cannot be negative.")
        };
    
    private static string GetFastingStageColor(int hours) =>
        hours switch
        {
            >= 0 and < 4   => "#2196F3", // Stage 1 - Blue
            >= 4 and < 12  => "#ffd800", // Stage 2 - Yellow
            >= 12 and < 24 => "#FF9800", // Stage 3 - Orange
            >= 24 and < 48 => "#D84315", // Stage 4 - Deep Reddish Orange
            >= 48          => "#F44336", // Stage 5 - Red
            _ => throw new ArgumentOutOfRangeException(nameof(hours), "Hours cannot be negative.")
        };
}