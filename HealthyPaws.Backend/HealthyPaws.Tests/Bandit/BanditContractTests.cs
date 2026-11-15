using System.Text.Json;
using System.Text.Json.Nodes;
using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;

namespace HealthyPaws.Tests.Bandit;

/// <summary>
/// Ugovor C# &lt;-&gt; Python (LinUCB preko RabbitMQ). Golden fajlovi su u <c>HealthyPaws.Backend/contracts/bandit</c>;
/// isti fajlove čitaju i Python testovi (healthy_paws.ai/tests/meal_scheduler/test_contract.py).
/// Request fajlove generiše C# (ovdje), response fajlove generiše Python oblik izlaza.
/// Za osvježavanje golden request fajlova: UPDATE_CONTRACTS=1 dotnet test.
/// </summary>
public class BanditContractTests
{
    static string ContractsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "contracts", "bandit")))
            dir = dir.Parent;
        return dir is null ? throw new DirectoryNotFoundException("contracts/bandit nije pronađen") : Path.Combine(dir.FullName, "contracts", "bandit");
    }

    static JsonNode Read(string name) => JsonNode.Parse(File.ReadAllText(Path.Combine(ContractsDir(), name)))!;

    static void AssertMatchesGolden(string name, object message)
    {
        var actual = JsonSerializer.SerializeToNode(message)!;
        var path = Path.Combine(ContractsDir(), name);
        if (Environment.GetEnvironmentVariable("UPDATE_CONTRACTS") == "1")
            File.WriteAllText(path, actual.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Assert.True(JsonNode.DeepEquals(Read(name), actual),
            $"Serijalizacija se razlikuje od {name}. Ako je promjena namjerna, pokreni s UPDATE_CONTRACTS=1 i provjeri Python testove.\nActual: {actual}");
    }

    static readonly BanditContext Context = new(HoursSinceLastMeal: 4.5, AvgEatenFraction: 0.8, TreatBudgetShare: 0.05, BaselineHour: 10.0, MealIndex: 2);
    static readonly JsonElement State = JsonDocument.Parse("""{"d":7,"version":3,"arms":{}}""").RootElement.Clone();

    [Fact]
    public void Select_request_matches_golden()
    {
        var request = new SelectRequest("11111111-1111-1111-1111-111111111111", "7", Context, [-60, 0, 60], null);
        AssertMatchesGolden("select_request.json", request);
    }

    [Fact]
    public void Select_request_with_state_matches_golden()
    {
        var request = new SelectRequest("11111111-1111-1111-1111-111111111111", "7", Context, [-60, 0, 60], State);
        AssertMatchesGolden("select_request_with_state.json", request);
    }

    [Fact]
    public void Feedback_request_matches_golden()
    {
        var request = new FeedbackRequest("11111111-1111-1111-1111-111111111111", "7", 60, Context,
            new OutcomePayload(120, 95, 180), State);
        AssertMatchesGolden("feedback_request.json", request);
    }

    [Fact]
    public void Feedback_request_with_unknown_latency_serializes_null_not_missing()
    {
        var node = JsonSerializer.SerializeToNode(new FeedbackRequest("d", "1", 0, Context, new OutcomePayload(100, 0, null), null))!;
        Assert.True(node["outcome"]!.AsObject().ContainsKey("latencyS"));
        Assert.Null(node["outcome"]!["latencyS"]);
    }

    [Fact]
    public void Context_with_unknown_values_serializes_nulls()
    {
        var node = JsonSerializer.SerializeToNode(new BanditContext(null, null, null, 8, 0))!.AsObject();
        Assert.Null(node["hoursSinceLastMeal"]);
        Assert.Null(node["avgEatenFraction"]);
        Assert.Null(node["treatBudgetShare"]);
        Assert.Equal(8, node["baselineHour"]!.GetValue<double>());
    }

    [Fact]
    public void Python_select_response_deserializes()
    {
        var response = JsonSerializer.Deserialize<SelectResponse>(Read("select_response.json").ToJsonString())!;
        Assert.Equal(("11111111-1111-1111-1111-111111111111", "7", 60, 3),
            (response.DecisionId, response.DogId, response.Arm, response.StateVersion));
    }

    [Fact]
    public void Python_feedback_response_deserializes_and_exposes_version_for_optimistic_concurrency()
    {
        var response = JsonSerializer.Deserialize<FeedbackResponse>(Read("feedback_response.json").ToJsonString())!;
        Assert.Equal(0.6 * (95 / 120.0) + 0.4 * (1 - 180 / 1800.0), response.Reward, 6);
        // FeedbackUpdatedConsumer: newVersion = state.version, očekivana verzija u bazi = newVersion - 1
        Assert.Equal(4, response.State.GetProperty("version").GetInt32());
    }

    [Fact]
    public void Arms_sent_to_python_are_all_within_python_limits()
    {
        // Python: |arm| <= 60 (MAX_OFFSET_MIN) i najviše 5 armova (MAX_ARMS)
        Assert.All(HealthyPaws.Services.Bandit.BanditOptions.Arms, a => Assert.InRange(a, -60, 60));
        Assert.InRange(HealthyPaws.Services.Bandit.BanditOptions.Arms.Length, 1, 5);
    }

    [Fact]
    public void Reward_window_default_matches_python_default()
    {
        // Python: BANDIT_REWARD_WINDOW_S default 1800
        if (Environment.GetEnvironmentVariable("BANDIT_REWARD_WINDOW_S") is null)
            Assert.Equal(TimeSpan.FromSeconds(1800), HealthyPaws.Services.Bandit.BanditOptions.RewardWindow);
    }

    [Fact]
    public void Select_request_contains_every_field_python_requires()
    {
        // Python build_features: mealIndex je obavezan (0..3), ostalo je opcionalno
        var context = JsonSerializer.SerializeToNode(Context)!.AsObject();
        Assert.Equal(2, context["mealIndex"]!.GetValue<int>());
        Assert.InRange(context["mealIndex"]!.GetValue<int>(), 0, 3);
    }
}
