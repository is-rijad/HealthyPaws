import 'package:healthy_paws/providers/wizard_providers/first_step_provider.dart';
import 'package:healthy_paws/resources/requests/device/device_insert_request.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:wizard_stepper/wizard_stepper.dart';

class FirstStep extends StatelessWidget with WizardStep {
  final WizardStepperController controller;
  FirstStep({super.key, required this.controller});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(12),
      child: ValueListenableBuilder<bool>(
        valueListenable: isCompleteNotifier,
        builder: (context, isComplete, child) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              TextButton(
                onPressed: () {
                  context
                      .read<FirstStepProvider>()
                      .request = DeviceInsertRequest(
                    macAddress: "AA:AA:AA:AA:AA:AA", //TODO: temp
                  );
                  completeStep(true);
                },
                child: Text('Poveži uređaj'),
              ),
            ],
          );
        },
      ),
    );
  }
}
