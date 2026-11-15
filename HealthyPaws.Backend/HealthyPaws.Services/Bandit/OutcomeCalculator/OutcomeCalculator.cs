using HealhtyPaws.Models.Dto.Bandit.Outcome;

namespace HealthyPaws.Services.Bandit.OutcomeCalculator;

public static class OutcomeCalculator
{
    const double MinDispensedG = 5.0;
    const double NoiseFloorG = 2.0;
    const double EatFraction = 0.03;
    static readonly TimeSpan Settle = TimeSpan.FromMinutes(2);
    static readonly TimeSpan Tail = TimeSpan.FromMinutes(2);
    static readonly TimeSpan NoDataGrace = TimeSpan.FromMinutes(5);

    public static OutcomeResult Compute(IReadOnlyList<Sample> samples, DateTimeOffset dispensedAt, TimeSpan window, DateTimeOffset now)
    {
        samples = samples.OrderBy(x => x.Time).ToList();
        var end = dispensedAt + window;

        var settle = samples.Where(x => x.Time >= dispensedAt && x.Time <= dispensedAt + Settle).ToList();
        if (settle.Count == 0)
            return new OutcomeResult(now >= end + NoDataGrace ? OutcomeKind.NotDispensed : OutcomeKind.NotReady, null);

        var before = samples.LastOrDefault(x => x.Time < dispensedAt)?.Grams ?? 0.0;
        var peak = settle.MaxBy(x => x.Grams)!;
        var dispensed = peak.Grams - before;
        if (dispensed < MinDispensedG) return new OutcomeResult(OutcomeKind.NotDispensed, null);

        if (samples[^1].Time < end - Tail) return new OutcomeResult(OutcomeKind.NotReady, null);

        var tail = samples.Where(x => x.Time >= end - Tail && x.Time <= end).Select(x => x.Grams).ToList();
        var endWeight = tail.Count > 0 ? Median(tail) : samples.Last(x => x.Time <= end).Grams;
        var eaten = Math.Max(peak.Grams - endWeight, 0.0);

        var threshold = Math.Max(NoiseFloorG, EatFraction * dispensed);
        var after = samples.Where(x => x.Time > peak.Time && x.Time <= end).ToList();
        double? latency = null;
        for (var i = 0; i < after.Count; i++)
        {
            if (after[i].Grams > peak.Grams - threshold) continue;
            if (i + 1 < after.Count && after[i + 1].Grams > peak.Grams - threshold) continue;
            latency = (after[i].Time - dispensedAt).TotalSeconds;
            break;
        }

        return new OutcomeResult(OutcomeKind.Ready, new OutcomeDto(dispensed, eaten, latency));
    }

    static double Median(List<double> v)
    {
        v.Sort();
        var m = v.Count / 2;
        return v.Count % 2 == 1 ? v[m] : (v[m - 1] + v[m]) / 2.0;
    }
}
