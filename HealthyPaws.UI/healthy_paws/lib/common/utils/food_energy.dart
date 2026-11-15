/// Izvor vrijednosti kalorija hrane.
enum FoodCalorieSource {
  /// Proizvođač je naveo kcal na etiketi.
  label,

  /// Izračunato iz analitičkog sastava (modificirani Atwater).
  estimated;

  /// Vrijednost enuma `CalorieSource` na backendu.
  int get apiValue => index;
}

/// Rezultat procjene energije hrane.
class FoodEnergyResult {
  /// Metabolizirajuća energija u kcal/100g.
  final double kcalPer100g;

  /// Ugljikohidrati (NFE) u %.
  final double nfe;

  /// Korišteni pepeo u %.
  final double ash;

  /// Da li je pepeo procijenjen jer nije naveden.
  final bool ashEstimated;

  const FoodEnergyResult({
    required this.kcalPer100g,
    required this.nfe,
    required this.ash,
    required this.ashEstimated,
  });
}

class FoodEnergy {
  /// Okvirna greška procjene sa etikete (±15%).
  static const estimatedUncertainty = 0.15;

  /// Pepeo kad nije naveden: ~7% suha hrana, ~2.5% konzerva.
  static double defaultAsh(double moisture) => moisture >= 50 ? 2.5 : 7;

  /// Modificirani Atwater: ME(kcal/kg) = 10 * (3.5 * P + 8.5 * F + 3.5 * NFE),
  /// NFE = 100 - protein - masti - vlakna - vlaga - pepeo. Sve vrijednosti u %.
  /// Vraća null kad su ulazi nevaljani (negativni, zbir > 100 ili NFE < 0).
  static FoodEnergyResult? estimate({
    required double protein,
    required double fat,
    required double fiber,
    required double moisture,
    double? ash,
  }) {
    final values = [protein, fat, fiber, moisture, ?ash];
    if (values.any((v) => v < 0 || v > 100)) return null;
    final usedAsh = ash ?? defaultAsh(moisture);
    final nfe = 100 - protein - fat - fiber - moisture - usedAsh;
    if (nfe < 0) return null;
    return FoodEnergyResult(
      kcalPer100g: 3.5 * protein + 8.5 * fat + 3.5 * nfe,
      nfe: nfe,
      ash: usedAsh,
      ashEstimated: ash == null,
    );
  }
}
