namespace Fasting.Models;

public static class FastingTypes
{
    public static readonly FastingType TwelveTwelve = new()
    {
        Id = "12:12",
        Name = "12:12",
        Description = "Beginner fasting schedule.",
        FastingHours = 12,
        EatingHours = 12
    };

    public static readonly FastingType FourteenTen = new()
    {
        Id = "14:10",
        Name = "14:10",
        Description = "Easy transition from 12:12.",
        FastingHours = 14,
        EatingHours = 10
    };

    public static readonly FastingType SixteenEight = new()
    {
        Id = "16:8",
        Name = "16:8",
        Description = "Most popular intermittent fasting schedule.",
        FastingHours = 16,
        EatingHours = 8
    };

    public static readonly FastingType EighteenSix = new()
    {
        Id = "18:6",
        Name = "18:6",
        Description = "Intermediate fasting schedule.",
        FastingHours = 18,
        EatingHours = 6
    };

    public static readonly FastingType TwentyFour = new()
    {
        Id = "20:4",
        Name = "20:4",
        Description = "Warrior Diet.",
        FastingHours = 20,
        EatingHours = 4
    };

    public static readonly FastingType Omad = new()
    {
        Id = "23:1",
        Name = "OMAD",
        Description = "One Meal A Day.",
        FastingHours = 23,
        EatingHours = 1
    };

    public static readonly FastingType FiveTwo = new()
    {
        Id = "5:2",
        Name = "5:2",
        Description = "Normal eating five days, reduced calories on two days.",
        IsCustomSchedule = true
    };

    public static readonly FastingType AlternateDay = new()
    {
        Id = "ADF",
        Name = "Alternate-Day Fasting",
        Description = "Fast every other day.",
        IsCustomSchedule = true
    };

    public static readonly FastingType EatStopEat = new()
    {
        Id = "ESE",
        Name = "Eat Stop Eat",
        Description = "One or two 24-hour fasts per week.",
        IsCustomSchedule = true
    };

    public static readonly FastingType Custom = new()
    {
        Id = "CUSTOM",
        Name = "Custom",
        Description = "User-defined fasting schedule.",
        IsCustomSchedule = true
    };

    public static IReadOnlyList<FastingType> All { get; } =
    [
        TwelveTwelve,
        FourteenTen,
        SixteenEight,
        EighteenSix,
        TwentyFour,
        Omad,
        FiveTwo,
        AlternateDay,
        EatStopEat,
        Custom
    ];
}