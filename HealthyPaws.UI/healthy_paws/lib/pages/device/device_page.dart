import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/pages/device/device_form.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';

class DevicePage extends StatefulWidget {
  const DevicePage({super.key});

  @override
  State<StatefulWidget> createState() => _DevicePageState();
}

class _DevicePageState extends State<DevicePage> {
  @override
  Widget build(BuildContext context) {
    return PageWrapper(
      appBarTitle: "Moji uređaji",
      body: Column(
        spacing: 12,
        children: [
          const Text("Moji uređaji"),
          PrimaryButton(
            text: "Dodaj uređaj",
            onPressed: () async =>
                await Constants.navigatorKey.currentState?.push(
                  MaterialPageRoute(builder: (builder) => DeviceForm(id: null)),
                ),
          ),
        ],
      ),
    );
  }
}
