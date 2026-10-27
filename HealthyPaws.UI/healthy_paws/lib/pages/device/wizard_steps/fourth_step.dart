import 'package:healthy_paws/widgets/wrappers/wizard_step_layout.dart';
import 'package:healthy_paws/providers/wizard_providers/fourth_step_provider.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:wizard_stepper/wizard_stepper.dart';

class FourthStep extends StatefulWidget with WizardStep {
  final WizardStepperController controller;
  FourthStep({super.key, required this.controller});

  @override
  State<StatefulWidget> createState() => _FourthStepState();
}

class _FourthStepState extends State<FourthStep>
    with AutomaticKeepAliveClientMixin {
  static const _maxMeals = 5;
  static const _presets = <int, List<TimeOfDay>>{
    1: [TimeOfDay(hour: 8, minute: 0)],
    2: [TimeOfDay(hour: 8, minute: 0), TimeOfDay(hour: 18, minute: 0)],
    3: [
      TimeOfDay(hour: 8, minute: 0),
      TimeOfDay(hour: 13, minute: 0),
      TimeOfDay(hour: 18, minute: 0),
    ],
    4: [
      TimeOfDay(hour: 7, minute: 0),
      TimeOfDay(hour: 12, minute: 0),
      TimeOfDay(hour: 17, minute: 0),
      TimeOfDay(hour: 21, minute: 0),
    ],
    5: [
      TimeOfDay(hour: 7, minute: 0),
      TimeOfDay(hour: 10, minute: 0),
      TimeOfDay(hour: 13, minute: 0),
      TimeOfDay(hour: 17, minute: 0),
      TimeOfDay(hour: 20, minute: 0),
    ],
  };

  late List<TimeOfDay> _times;
  final _calorieController = TextEditingController();

  double? get _calorie =>
      double.tryParse(_calorieController.text.trim().replaceAll(',', '.'));

  bool get _isValid => !_hasDuplicates && (_calorie ?? 0) > 0;

  bool get _hasDuplicates =>
      _times.map(_minutes).toSet().length != _times.length;

  int _minutes(TimeOfDay t) => t.hour * 60 + t.minute;

  @override
  void initState() {
    super.initState();
    _times = List.of(_presets[2]!);
    _publish();
    widget.isCurrentStepNotifier.addListener(_onCurrentStepChanged);
  }

  @override
  void dispose() {
    widget.isCurrentStepNotifier.removeListener(_onCurrentStepChanged);
    _calorieController.dispose();
    super.dispose();
  }

  void _onCurrentStepChanged() {
    if (widget.isCurrentStep) widget.completeStep(_isValid);
  }

  void _publish() {
    context.read<FourthStepProvider>().calorie = _calorie;
    final sorted = List.of(_times)..sort((a, b) => _minutes(a) - _minutes(b));
    context.read<FourthStepProvider>().mealTimes = sorted
        .map(
          (t) =>
              "${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}:00",
        )
        .toList();
  }

  void _update(void Function() change) {
    setState(change);
    _publish();
    widget.completeStep(_isValid);
  }

  Future<void> _pickTime(int index) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: _times[index],
    );
    if (picked != null) _update(() => _times[index] = picked);
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return WizardStepLayout(
      title: "Ishrana psa",
      subtitle: "Okvirno: koliko puta dnevno jede, u koje vrijeme i koju hranu koristi.",
      children: [
        Wrap(
          spacing: 8,
          children: [
            for (var n = 1; n <= _maxMeals; n++)
              ChoiceChip(
                label: Text("${n}x"),
                selected: _times.length == n,
                onSelected: (_) =>
                    _update(() => _times = List.of(_presets[n]!)),
              ),
          ],
        ),
        for (var i = 0; i < _times.length; i++)
          Card(
            child: ListTile(
              leading: const Icon(Icons.restaurant),
              title: Text("Obrok ${i + 1}"),
              trailing: Row(
                mainAxisSize: MainAxisSize.min,
                spacing: 8,
                children: [
                  Text(
                    _times[i].format(context),
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const Icon(Icons.edit, size: 18),
                ],
              ),
              onTap: () => _pickTime(i),
            ),
          ),
        TextField(
          controller: _calorieController,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(
            labelText: "Iznos kalorija hrane koju pas koristi",
            suffixText: "kcal/100g",
          ),
          onChanged: (_) => _update(() {}),
        ),
        if (_hasDuplicates)
          const Text(
            "Vremena obroka moraju biti različita!",
            style: TextStyle(color: Colors.red),
          ),
      ],
    );
  }

  @override
  bool get wantKeepAlive => true;
}
