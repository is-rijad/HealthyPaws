import 'package:healthy_paws/common/utils/food_energy.dart';
import 'package:material_ui/material_ui.dart';

/// Unos kalorija hrane: direktno (kcal/100g) ili procjena iz sastava sa etikete.
/// [onChanged] dobija kalorije (null kad unos nije valjan) i izvor vrijednosti.
class FoodInput extends StatefulWidget {
  final double? initialKcal;
  final void Function(double? calorie, FoodCalorieSource source) onChanged;

  const FoodInput({super.key, this.initialKcal, required this.onChanged});

  @override
  State<FoodInput> createState() => _FoodInputState();
}

class _FoodInputState extends State<FoodInput> {
  bool _fromLabel = true;
  late final _kcal = TextEditingController(
    text: widget.initialKcal?.toString() ?? "",
  );
  final _protein = TextEditingController();
  final _fat = TextEditingController();
  final _fiber = TextEditingController();
  final _moisture = TextEditingController();
  final _ash = TextEditingController();

  double? _num(TextEditingController c) =>
      double.tryParse(c.text.trim().replaceAll(',', '.'));

  FoodEnergyResult? get _estimate {
    final protein = _num(_protein),
        fat = _num(_fat),
        fiber = _num(_fiber),
        moisture = _num(_moisture);
    if (protein == null || fat == null || fiber == null || moisture == null) {
      return null;
    }
    return FoodEnergy.estimate(
      protein: protein,
      fat: fat,
      fiber: fiber,
      moisture: moisture,
      ash: _num(_ash),
    );
  }

  double? get _calorie => _fromLabel ? _num(_kcal) : _estimate?.kcalPer100g;

  @override
  void dispose() {
    for (final c in [_kcal, _protein, _fat, _fiber, _moisture, _ash]) {
      c.dispose();
    }
    super.dispose();
  }

  void _update(void Function() change) {
    setState(change);
    widget.onChanged(
      _calorie,
      _fromLabel ? FoodCalorieSource.label : FoodCalorieSource.estimated,
    );
  }

  Widget _field(TextEditingController c, String label, {String suffix = "%"}) =>
      TextField(
        controller: c,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        decoration: InputDecoration(labelText: label, suffixText: suffix),
        onChanged: (_) => _update(() {}),
      );

  @override
  Widget build(BuildContext context) {
    final estimate = _fromLabel ? null : _estimate;
    final spread =
        (estimate?.kcalPer100g ?? 0) * FoodEnergy.estimatedUncertainty;
    final invalidComposition =
        !_fromLabel &&
        _estimate == null &&
        [_protein, _fat, _fiber, _moisture].every((c) => _num(c) != null);
    return Column(
      spacing: 12,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SegmentedButton<bool>(
          segments: const [
            ButtonSegment(value: true, label: Text("Znam kcal")),
            ButtonSegment(value: false, label: Text("Sa etikete")),
          ],
          selected: {_fromLabel},
          onSelectionChanged: (s) => _update(() => _fromLabel = s.first),
        ),
        if (_fromLabel)
          _field(
            _kcal,
            "Iznos kalorija hrane koju pas koristi",
            suffix: "kcal/100g",
          )
        else ...[
          _field(_protein, "Protein"),
          _field(_fat, "Masti"),
          _field(_fiber, "Vlakna"),
          _field(_moisture, "Vlaga"),
          _field(_ash, "Pepeo (opcionalno)"),
          if (estimate != null)
            Text(
              "Procijenjeno: ${estimate.kcalPer100g.toStringAsFixed(0)} kcal/100g "
              "(±${spread.toStringAsFixed(0)})"
              "${estimate.ashEstimated ? "\nPepeo procijenjen na ${estimate.ash}%." : ""}"
              "\nProcjena je okvirna, očekivana greška je oko ±15%.",
            ),
          if (invalidComposition)
            const Text(
              "Zbir vrijednosti ne smije preći 100%!",
              style: TextStyle(color: Colors.red),
            ),
        ],
      ],
    );
  }
}
