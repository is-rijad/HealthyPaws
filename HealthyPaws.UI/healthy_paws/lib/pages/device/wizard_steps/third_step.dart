import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/components/dog_weight/dog_weight_component.dart';
import 'package:healthy_paws/providers/wizard_providers/third_step_provider.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_insert_request.dart';
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
    return Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          DogWeightComponent(
            formKey: _formKey,
            onValidated: () {
              if (_formKey.currentState?.saveAndValidate() ?? false) {
                context
                    .read<ThirdStepProvider>()
                    .request = DogWeightInsertRequest.fromJson(
                  _formKey.currentState!.value,
                );
                widget.completeStep(true);
              }
            },
            autovalidateMode: AutovalidateMode.onUserInteractionIfError,
          ),
        ],
      ),
    );
  }

  @override
  bool get wantKeepAlive => true;
}
