import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/pages/device/device_page.dart';
import 'package:healthy_paws/pages/treat/treat_tracker_page.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:provider/provider.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<StatefulWidget> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    return PageWrapper(
      appBarTitle: "Healthy Paws",
      body: Column(
        children: [
          PrimaryButton(
            text: "Moji uređaji",
            onPressed: () async => await Constants.navigatorKey.currentState
                ?.push(MaterialPageRoute(builder: (builder) => DevicePage())),
          ),
          PrimaryButton(
            text: "Praćenje poslastica",
            onPressed: () async =>
                await Constants.navigatorKey.currentState?.push(
                  MaterialPageRoute(builder: (builder) => TreatTrackerPage()),
                ),
          ),
        ],
      ),
    );
  }

  Future<bool> _init() async {
    await context.read<AuthProvider>().checkAuth();
    return true;
  }
}
