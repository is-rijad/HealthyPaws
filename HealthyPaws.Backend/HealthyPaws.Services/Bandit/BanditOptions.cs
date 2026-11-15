namespace HealthyPaws.Services.Bandit;

public static class BanditOptions
{
    public static readonly int[] Arms = [-60, 0, 60];

    public static int BaselineOffsetMinutes => 60;

    public static TimeSpan DispenseGrace => TimeSpan.FromMinutes(15);

    public static TimeSpan LostJobAfter => TimeSpan.FromMinutes(5);

    public static int SelectLeadMinutes => 75;

    public static int FallbackBeforeBaselineMinutes => 62;

    public static string TimeZone => "Europe/Sarajevo";

    public static TimeSpan RewardWindow { get; } = TimeSpan.FromSeconds(
        double.TryParse(Environment.GetEnvironmentVariable("BANDIT_REWARD_WINDOW_S"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) && s > 0
            ? s
            : 1800);

    public static TimeSpan OutcomeDelay => RewardWindow + TimeSpan.FromMinutes(1);
}
