import 'package:healthy_paws/widgets/wrappers/wizard_step_layout.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/components/dog_form/dog_form_component.dart';
import 'package:healthy_paws/providers/wizard_providers/second_step_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_insert_request.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:wizard_stepper/wizard_stepper.dart';

class SecondStep extends StatefulWidget with WizardStep {
  final WizardStepperController controller;
  SecondStep({super.key, required this.controller});

  @override
  State<StatefulWidget> createState() => _SecondStepState();
}

class _SecondStepState extends State<SecondStep>
    with AutomaticKeepAliveClientMixin {
  final _formKey = GlobalKey<FormBuilderState>();

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return WizardStepLayout(
      title: "Podaci o psu",
      subtitle: "Unesite osnovne podatke o svom psu.",
      children: [
        DogFormComponent(
          formKey: _formKey,
          onValidated: () {
            if (_formKey.currentState?.saveAndValidate() ?? false) {
              context.read<SecondStepProvider>().request =
                  DogInsertRequest.fromJson(_formKey.currentState!.value);
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
