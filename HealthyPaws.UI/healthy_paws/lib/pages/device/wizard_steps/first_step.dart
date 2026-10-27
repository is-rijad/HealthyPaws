import 'package:healthy_paws/widgets/wrappers/wizard_step_layout.dart';
import 'package:healthy_paws/providers/wizard_providers/first_step_provider.dart';
import 'package:healthy_paws/resources/requests/device/device_insert_request.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:wizard_stepper/wizard_stepper.dart';

class FirstStep extends StatelessWidget with WizardStep {
  final WizardStepperController controller;
  FirstStep({super.key, required this.controller});

  @override
  Widget build(BuildContext context) {
    return WizardStepLayout(
      title: "Povežite uređaj",
      subtitle: "Povežite pametni uređaj sa svojim računom.",
      children: [
        ValueListenableBuilder<bool>(
          valueListenable: isCompleteNotifier,
          builder: (context, isComplete, child) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              spacing: 12,
              children: [
                Card(
                  child: ListTile(
                    leading: const Icon(Icons.devices_other),
                    title: Text(isComplete ? "Uređaj povezan" : "Uređaj"),
                    trailing: isComplete
                        ? const Icon(Icons.check_circle, color: Colors.green)
                        : null,
                  ),
                ),
                if (!isComplete)
                  PrimaryButton(
                    text: "Poveži uređaj",
                    onPressed: () {
                      context
                          .read<FirstStepProvider>()
                          .request = DeviceInsertRequest(
                        macAddress: "AA:AA:AA:AA:AA:AA", //TODO: temp
                      );
                      completeStep(true);
                    },
                  ),
              ],
            );
          },
        ),
      ],
    );
  }
}
