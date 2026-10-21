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
    super.dispose();
  }

  // Korak je validan i sa zadanim vrijednostima, pa se označava završenim
  // čim postane aktivan.
  void _onCurrentStepChanged() {
    if (widget.isCurrentStep) widget.completeStep(!_hasDuplicates);
  }

  void _publish() {
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
    widget.completeStep(!_hasDuplicates);
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
    return Padding(
      padding: const EdgeInsets.all(12),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: 12,
          children: [
            Text(
              "Koliko pas trenutno jede dnevno?",
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const Text("Okvirno: koliko puta dnevno i u koje vrijeme."),
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
                  trailing: Text(
                    _times[i].format(context),
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  onTap: () => _pickTime(i),
                ),
              ),
            if (_hasDuplicates)
              const Text(
                "Vremena obroka moraju biti različita!",
                style: TextStyle(color: Colors.red),
              ),
          ],
        ),
      ),
    );
  }

  @override
  bool get wantKeepAlive => true;
}
