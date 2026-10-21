import 'package:healthy_paws/pages/device/wizard_steps/second_step.dart';
import 'package:healthy_paws/pages/device/wizard_steps/third_step.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/pages/device/wizard_steps/first_step.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:wizard_stepper/wizard_stepper.dart';

class DeviceWizard extends StatefulWidget {
  const DeviceWizard({super.key});

  @override
  State<StatefulWidget> createState() => _DeviceWizardState();
}

class _DeviceWizardState extends State<DeviceWizard> {
  late final WizardStepperController _controller;
  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _controller = WizardStepperController(
      onMovedToLastStep: () async =>
          await context.read<DevicesProvider>().completeWizard(context),
      showNavigationButtons: true,
      usePageView: true,
    );
  }

  @override
  Widget build(BuildContext context) {
    return PageWrapper(
      body: WizardStepper(
        controller: _controller,
        steps: [
          FirstStep(controller: _controller),
          SecondStep(controller: _controller),
          ThirdStep(controller: _controller),
        ],
      ),
    );
  }
}
