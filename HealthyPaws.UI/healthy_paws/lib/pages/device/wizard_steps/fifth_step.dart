import 'package:healthy_paws/common/utils/food_energy.dart';
import 'package:healthy_paws/components/food_input/food_input.dart';
import 'package:healthy_paws/providers/wizard_providers/fifth_step_provider.dart';
import 'package:healthy_paws/widgets/wrappers/wizard_step_layout.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:wizard_stepper/wizard_stepper.dart';

class FifthStep extends StatefulWidget with WizardStep {
  final WizardStepperController controller;
  FifthStep({super.key, required this.controller});

  @override
  State<StatefulWidget> createState() => _FifthStepState();
}

class _FifthStepState extends State<FifthStep>
    with AutomaticKeepAliveClientMixin {
  bool _isValid = false;

  @override
  void initState() {
    super.initState();
    widget.isCurrentStepNotifier.addListener(_onCurrentStepChanged);
  }

  @override
  void dispose() {
    widget.isCurrentStepNotifier.removeListener(_onCurrentStepChanged);
    super.dispose();
  }

  void _onCurrentStepChanged() {
    if (widget.isCurrentStep) widget.completeStep(_isValid);
  }

  void _onFoodChanged(double? calorie, FoodCalorieSource source) {
    final provider = context.read<FifthStepProvider>();
    provider.calorie = calorie;
    provider.source = source;
    _isValid = (calorie ?? 0) > 0;
    widget.completeStep(_isValid);
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return WizardStepLayout(
      title: "Hrana psa",
      subtitle: "Unesite kalorije hrane ili vrijednosti sa etikete.",
      children: [FoodInput(onChanged: _onFoodChanged)],
    );
  }

  @override
  bool get wantKeepAlive => true;
}
