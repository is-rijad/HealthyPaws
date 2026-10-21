import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/pages/device/device_form.dart';
import 'package:healthy_paws/pages/device/device_wizard.dart';
import 'package:healthy_paws/pages/device/dog_weight_form.dart';
import 'package:healthy_paws/pages/dog/dog_form.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';
import 'package:healthy_paws/resources/responses/device/device_response.dart';
import 'package:healthy_paws/resources/search_objects/device/device_search_object.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DevicePage extends StatefulWidget {
  const DevicePage({super.key});

  @override
  State<StatefulWidget> createState() => _DevicePageState();
}

class _DevicePageState extends State<DevicePage> {
  static const _weightMaxAge = Duration(days: 7);
  final _searchObject = DeviceSearchObject(page: 1, pageSize: 10);

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    final devices = context.watch<DevicesProvider>().searchResult.items;
    return PageWrapper(
      appBarTitle: "Moji uređaji",
      body: Column(
        spacing: 12,
        children: [
          Expanded(
            child: devices.isEmpty
                ? const Center(child: Text("Nemate dodanih uređaja."))
                : ListView.separated(
                    itemCount: devices.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 8),
                    itemBuilder: (_, index) =>
                        _buildDeviceCard(context, devices[index]),
                  ),
          ),
          PrimaryButton(
            text: "Dodaj uređaj",
            onPressed: () async {
              final added = await Constants.navigatorKey.currentState?.push(
                MaterialPageRoute(builder: (builder) => DeviceWizard()),
              );
              if (added == true && mounted) await _refresh();
            },
          ),
        ],
      ),
    );
  }

  Widget _buildDeviceCard(BuildContext context, DeviceResponse device) {
    final dog = device.dog;
    final lastWeight = device.lastWeightRecord;
    final isWeightOutdated =
        lastWeight == null ||
        DateTime.now().difference(lastWeight.createdAt) > _weightMaxAge;
    return Card(
      child: ListTile(
        onTap: () async => await Constants.navigatorKey.currentState?.push(
          MaterialPageRoute(builder: (_) => DogWeightForm(device: device)),
        ),
        onLongPress: () => _showEditDialog(device),
        leading: Icon(
          device.isConnected ? Icons.sensors : Icons.sensors_off,
          color: device.isConnected ? Colors.green : Colors.grey,
        ),
        title: Text(dog?.name ?? "Nepoznat pas"),
        subtitle: dog == null
            ? null
            : Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    [
                      dog.breed?.name,
                      dogGenderMap[dog.gender],
                      "Rođen: ${context.getLocalizedDate(dog.dateOfBirth)}",
                      lastWeight?.weight != null
                          ? "Težina: ${lastWeight?.weight} kg"
                          : null,
                      lastWeight?.bcs != null
                          ? "BCS: ${lastWeight?.bcs}"
                          : null,
                      if (lastWeight != null)
                        "Izmjereno: ${context.getLocalizedDate(lastWeight.createdAt)}",
                      if (dog.isNeutered) "Kastriran/sterilisan",
                    ].whereType<String>().join(" • "),
                  ),
                  if (isWeightOutdated)
                    Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Row(
                        spacing: 4,
                        children: [
                          const Icon(
                            Icons.warning_amber_rounded,
                            color: Colors.orange,
                            size: 18,
                          ),
                          Expanded(
                            child: Text(
                              lastWeight == null
                                  ? "Nema zabilježenih mjerenja težine."
                                  : "Težina nije mjerena duže od 7 dana.",
                              style: const TextStyle(color: Colors.orange),
                            ),
                          ),
                        ],
                      ),
                    ),
                ],
              ),
        isThreeLine: dog != null,
        trailing: Text(device.isConnected ? "Povezan" : "Nije povezan"),
      ),
    );
  }

  Future<void> _showEditDialog(DeviceResponse device) async {
    final dog = device.dog;
    final action = await showDialog<_EditAction>(
      context: context,
      builder: (context) => SimpleDialog(
        title: Text(dog?.name ?? "Uređaj"),
        children: [
          if (dog != null)
            SimpleDialogOption(
              onPressed: () => Navigator.pop(context, _EditAction.dog),
              child: const Text("Uredi psa"),
            ),
          SimpleDialogOption(
            onPressed: () => Navigator.pop(context, _EditAction.device),
            child: const Text("Uredi uređaj"),
          ),
        ],
      ),
    );
    final Widget? page = switch (action) {
      _EditAction.dog => DogForm(dog: dog!),
      _EditAction.device => DeviceForm(device: device),
      null => null,
    };
    if (page == null) return;
    final saved = await Constants.navigatorKey.currentState?.push(
      MaterialPageRoute(builder: (_) => page),
    );
    if (saved == true && mounted) await _refresh();
  }

  Future<bool> _init() async {
    await _refresh();
    return true;
  }

  Future<void> _refresh() async {
    final provider = context.read<DevicesProvider>();
    provider.clearData(_searchObject);
    await provider.fetchData(_searchObject);
  }
}

enum _EditAction { dog, device }
