namespace HealthyPaws.Services.AgentInsights;

public static class AlertCategories
{
    public const string Vet = "Veterinar";
    public const string DataCheck = "ProvjeraPodataka";
    public const string ManualAssessment = "RucnaProcjena";
}

/// <summary>Kategorija upozorenja i preporučena radnja za korisnika, prema reason kodu agenta.</summary>
public static class AlertAdvice
{
    private static readonly Dictionary<string, string> CategoryByReason = new()
    {
        ["UNEXPLAINED_WEIGHT_LOSS"] = AlertCategories.Vet,
        ["RAPID_LOSS_ON_DIET"] = AlertCategories.Vet,
        ["RAPID_CHANGE_CONFIRMED"] = AlertCategories.Vet,
        ["REDUCED_APPETITE"] = AlertCategories.Vet,
        ["MODEL_MISMATCH_PERSISTENT"] = AlertCategories.Vet,
        ["ESTIMATE_FAR_FROM_PRIOR"] = AlertCategories.Vet,

        ["INTAKE_IMPLAUSIBLE"] = AlertCategories.DataCheck,
        ["CONTRADICTORY_BCS_WEIGHT"] = AlertCategories.DataCheck,
        ["WEIGHT_OUT_OF_RANGE_CONFIRMED"] = AlertCategories.DataCheck,
        ["PLAN_AT_BOUND_NO_PROGRESS"] = AlertCategories.DataCheck,

        ["UNSUPPORTED_LIFE_STAGE"] = AlertCategories.ManualAssessment,
    };

    private static readonly Dictionary<string, string> RecommendationByCategory = new()
    {
        [AlertCategories.Vet] = "Preporučuje se pregled kod veterinara.",
        [AlertCategories.DataCheck] = "Provjerite podatke o psu (težina, BCS, raspon težine) i ispravnost hranilice.",
        [AlertCategories.ManualAssessment] = "Plan ishrane za ovog psa treba ručno procijeniti (štene, gravidnost ili laktacija).",
    };

    /// <summary>Nepoznat razlog tretira se oprezno, kao veterinarski.</summary>
    public static string CategoryOf(string reason) =>
        CategoryByReason.GetValueOrDefault(reason, AlertCategories.Vet);

    public static string RecommendationOf(string reason) => RecommendationByCategory[CategoryOf(reason)];
}
