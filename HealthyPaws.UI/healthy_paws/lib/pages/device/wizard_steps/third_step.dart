import 'package:healthy_paws/widgets/wrappers/wizard_step_layout.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/components/dog_weight/dog_weight_component.dart';
import 'package:healthy_paws/providers/wizard_providers/third_step_provider.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:wizard_stepper/wizard_stepper.dart';

class ThirdStep extends StatefulWidget with WizardStep {
  final WizardStepperController controller;
  ThirdStep({super.key, required this.controller});

  @override
  State<StatefulWidget> createState() => _ThirdStepState();
}

class _ThirdStepState extends State<ThirdStep>
    with AutomaticKeepAliveClientMixin {
  final _formKey = GlobalKey<FormBuilderState>();

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return WizardStepLayout(
      title: "Težina psa",
      subtitle: "Unesite trenutnu težinu i izgled svog psa.",
      children: [
        DogWeightComponent(
          formKey: _formKey,
          showCalorie: false,
          onValidated: () {
            if (_formKey.currentState?.saveAndValidate() ?? false) {
              final value = _formKey.currentState!.value;
              final provider = context.read<ThirdStepProvider>();
              provider.bcs = value["bcs"] as int;
              provider.weight = value["weight"] as double;
              widget.completeStep(true);
            }
          },
          autovalidateMode: AutovalidateMode.onUserInteractionIfError,
        ),
      ],
    );
  }

  @override
  bool get wantKeepAlive => true;
}
