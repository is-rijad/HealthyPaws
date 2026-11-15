using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Services.Bandit.OutcomeCalculator;

namespace HealthyPaws.Tests.Bandit;

public class OutcomeCalculatorTests
{
    static readonly DateTimeOffset T0 = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
    static readonly TimeSpan Window = TimeSpan.FromMinutes(30);

    static Sample S(double minutes, double grams) => new(T0 + TimeSpan.FromMinutes(minutes), grams);

    static OutcomeResult Compute(IReadOnlyList<Sample> samples, double nowMinutes = 40) =>
        OutcomeCalculator.Compute(samples, T0, Window, T0 + TimeSpan.FromMinutes(nowMinutes));

    /// <summary>Tipično hranjenje: ispusti 100 g, pas jede tokom prvih minuta, ostane 20 g.</summary>
    static List<Sample> TypicalMeal() =>
    [
        S(-1, 0), S(0.5, 100), S(1, 100), S(2, 100), S(5, 70), S(10, 40), S(20, 20), S(28, 20), S(29, 20), S(30, 20),
    ];

    [Fact]
    public void Typical_meal_is_ready_with_dispensed_eaten_and_latency()
    {
        var r = Compute(TypicalMeal());
        Assert.Equal(OutcomeKind.Ready, r.Kind);
        Assert.Equal(100, r.Outcome!.DispensedG);
        Assert.Equal(80, r.Outcome.EatenG);
        Assert.Equal(300, r.Outcome.LatencyS);   // prvi pad > 3 g (3% od 100) je u 5. minuti
    }

    [Fact]
    public void Eaten_is_never_negative_when_weight_increases()
    {
        var r = Compute([S(0.5, 100), S(1, 100), S(15, 105), S(28, 105), S(30, 105)]);
        Assert.Equal(OutcomeKind.Ready, r.Kind);
        Assert.Equal(0, r.Outcome!.EatenG);
        Assert.Null(r.Outcome.LatencyS);
    }

    [Fact]
    public void Untouched_food_has_zero_eaten_and_no_latency()
    {
        var r = Compute([S(0.5, 100), S(10, 100), S(29, 100), S(30, 100)]);
        Assert.Equal(0, r.Outcome!.EatenG);
        Assert.Null(r.Outcome.LatencyS);
    }

    [Fact]
    public void Everything_eaten()
    {
        var r = Compute([S(0.5, 100), S(2, 100), S(3, 40), S(8, 0), S(29, 0), S(30, 0)]);
        Assert.Equal(100, r.Outcome!.EatenG);
        Assert.Equal(180, r.Outcome.LatencyS);
    }

    [Fact]
    public void Baseline_before_dispense_is_subtracted()
    {
        // zdjela već ima 50 g -> ispušteno je 100, ne 150
        var r = Compute([S(-2, 50), S(1, 150), S(2, 150), S(10, 100), S(29, 70), S(30, 70)]);
        Assert.Equal(100, r.Outcome!.DispensedG);
        Assert.Equal(80, r.Outcome.EatenG);  // eaten se računa od vrha (150) do kraja (70)
    }

    [Fact]
    public void No_sample_before_dispense_assumes_empty_bowl()
    {
        var r = Compute([S(1, 100), S(29, 40), S(30, 40)]);
        Assert.Equal(100, r.Outcome!.DispensedG);
    }

    [Fact]
    public void Samples_are_sorted_internally()
    {
        var shuffled = TypicalMeal().OrderByDescending(x => x.Time).ToList();
        Assert.Equivalent(Compute(TypicalMeal()), Compute(shuffled));
    }

    [Fact]
    public void Peak_is_taken_within_settle_window_only()
    {
        // kasniji skok poslije 2 min (npr. neko doda hranu) ne mijenja ispušteno
        var r = Compute([S(0.5, 100), S(1, 100), S(2, 100), S(10, 300), S(29, 300), S(30, 300)]);
        Assert.Equal(100, r.Outcome!.DispensedG);
    }

    [Theory]
    [InlineData(0, OutcomeKind.NotDispensed)]
    [InlineData(4.9, OutcomeKind.NotDispensed)]
    [InlineData(5.0, OutcomeKind.Ready)]
    [InlineData(5.1, OutcomeKind.Ready)]
    public void Minimum_dispensed_amount_boundary(double grams, OutcomeKind expected)
    {
        var r = Compute([S(0.5, grams), S(1, grams), S(29, grams), S(30, grams)]);
        Assert.Equal(expected, r.Kind);
        Assert.Equal(expected == OutcomeKind.Ready, r.Outcome is not null);
    }

    [Fact]
    public void No_samples_in_settle_window_is_not_ready_until_grace_passes()
    {
        Assert.Equal(OutcomeKind.NotReady, Compute([], nowMinutes: 30 + 4.9).Kind);
        Assert.Equal(OutcomeKind.NotDispensed, Compute([], nowMinutes: 30 + 5).Kind);
    }

    [Fact]
    public void Only_samples_outside_settle_window_count_as_no_data()
    {
        var r = Compute([S(10, 100), S(30, 20)], nowMinutes: 40);
        Assert.Equal(OutcomeKind.NotDispensed, r.Kind);
    }

    [Fact]
    public void Samples_stop_before_end_of_window_is_not_ready()
    {
        var r = Compute([S(0.5, 100), S(1, 100), S(10, 60), S(20, 40)], nowMinutes: 25);
        Assert.Equal(OutcomeKind.NotReady, r.Kind);
        Assert.Null(r.Outcome);
    }

    [Fact]
    public void Last_sample_exactly_tail_before_end_is_ready()
    {
        var r = Compute([S(0.5, 100), S(1, 100), S(10, 60), S(28, 40)]);
        Assert.Equal(OutcomeKind.Ready, r.Kind);
        Assert.Equal(60, r.Outcome!.EatenG);
    }

    [Fact]
    public void End_weight_is_median_of_tail_so_one_spike_is_ignored()
    {
        var r = Compute([S(0.5, 100), S(1, 100), S(10, 50), S(28.1, 50), S(29, 90), S(30, 50)]);
        Assert.Equal(50, r.Outcome!.EatenG);
    }

    [Fact]
    public void Even_tail_count_uses_average_of_middle_values()
    {
        var r = Compute([S(0.5, 100), S(1, 100), S(28.5, 40), S(29.5, 60)]);
        Assert.Equal(50, r.Outcome!.EatenG);
    }

    [Fact]
    public void Latency_ignores_single_sample_dip()
    {
        // jedan šum-pad na 4. min koji se odmah vrati ne računa se kao početak jela
        var r = Compute([S(0.5, 100), S(1, 100), S(4, 90), S(4.5, 100), S(8, 100), S(12, 70), S(29, 70), S(30, 70)]);
        Assert.Equal(720, r.Outcome!.LatencyS);
    }

    [Fact]
    public void Latency_threshold_scales_with_portion_size()
    {
        // 3% od 400 g = 12 g; pad od 6 g je šum, pad od 20 g je jelo
        var noise = Compute([S(0.5, 400), S(1, 400), S(5, 394), S(29, 394), S(30, 394)]);
        var eating = Compute([S(0.5, 400), S(1, 400), S(5, 380), S(29, 380), S(30, 380)]);
        Assert.Null(noise.Outcome!.LatencyS);
        Assert.Equal(300, eating.Outcome!.LatencyS);
    }

    [Fact]
    public void Latency_threshold_has_noise_floor_of_two_grams()
    {
        // 3% od 10 g = 0.3 g, ali prag je najmanje 2 g
        var r = Compute([S(0.5, 10), S(1, 10), S(5, 9), S(29, 9), S(30, 9)]);
        Assert.Null(r.Outcome!.LatencyS);
        var r2 = Compute([S(0.5, 10), S(1, 10), S(5, 7), S(29, 7), S(30, 7)]);
        Assert.Equal(300, r2.Outcome!.LatencyS);
    }

    [Fact]
    public void Latency_is_measured_from_dispense_time_not_from_peak()
    {
        var r = Compute([S(0.5, 100), S(1.5, 100), S(2, 100), S(6, 50), S(29, 50), S(30, 50)]);
        Assert.Equal(360, r.Outcome!.LatencyS);
    }

    [Fact]
    public void Late_arrival_of_data_after_window_still_computes_using_end_of_window_tail()
    {
        var samples = TypicalMeal();
        samples.Add(S(45, 0));  // podaci poslije prozora se ne koriste
        var r = Compute(samples, nowMinutes: 60);
        Assert.Equal(80, r.Outcome!.EatenG);
    }

    [Fact]
    public void Window_is_configurable()
    {
        var r = OutcomeCalculator.Compute([S(0.5, 100), S(1, 100), S(8, 50), S(9.5, 50), S(10, 50)], T0,
            TimeSpan.FromMinutes(10), T0 + TimeSpan.FromMinutes(20));
        Assert.Equal(OutcomeKind.Ready, r.Kind);
        Assert.Equal(50, r.Outcome!.EatenG);
    }

    [Fact]
    public void Result_values_are_always_consistent_for_random_meals()
    {
        var rng = new Random(1234);
        for (var i = 0; i < 300; i++)
        {
            var portion = rng.Next(20, 400);
            var eaten = rng.NextDouble() * portion;
            var samples = new List<Sample> { S(-1, 0), S(0.5, portion), S(1.5, portion) };
            for (var m = 3; m <= 30; m += 3)
                samples.Add(S(m, portion - eaten * Math.Min(1, m / 15.0) + (rng.NextDouble() - .5) * 0.4));
            var r = Compute(samples);
            Assert.Equal(OutcomeKind.Ready, r.Kind);
            Assert.InRange(r.Outcome!.EatenG, 0, portion + 1);
            Assert.InRange(r.Outcome.DispensedG, portion - 1, portion + 1);
            Assert.Equal(eaten, r.Outcome.EatenG, 2.0);
            if (r.Outcome.LatencyS is { } latency) Assert.InRange(latency, 0, Window.TotalSeconds);
        }
    }
}
