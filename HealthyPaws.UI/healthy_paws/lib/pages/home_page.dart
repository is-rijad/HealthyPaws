import 'package:dio/dio.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/pages/device/device_wizard.dart';
import 'package:healthy_paws/pages/device/dog_weight_form.dart';
import 'package:healthy_paws/pages/dog/dog_form.dart';
import 'package:healthy_paws/pages/dog/dog_weight_history_page.dart';
import 'package:healthy_paws/pages/treat/treat_log_form.dart';
import 'package:healthy_paws/pages/main_shell.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/providers/dog_weights_provider.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/providers/treat_logs_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';
import 'package:healthy_paws/resources/responses/device/device_response.dart';
import 'package:healthy_paws/resources/responses/dog/calorie_budget_response.dart';
import 'package:healthy_paws/resources/responses/dog_weight/dog_weight_response.dart';
import 'package:healthy_paws/resources/responses/treat_log/treat_log_response.dart';
import 'package:healthy_paws/resources/search_objects/device/device_search_object.dart';
import 'package:healthy_paws/resources/search_objects/dog_weight/dog_weight_search_object.dart';
import 'package:healthy_paws/resources/search_objects/treat_log/treat_log_search_object.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/charts/weight_sparkline.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<StatefulWidget> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  static const _weightMaxAge = Duration(days: 7);
  static const _sparklinePoints = 10;

  int? _dogId;
  CalorieBudgetResponse? _budget;
  String? _budgetError;
  List<TreatLogResponse> _todayLogs = [];
  List<DogWeightResponse> _weights = [];
  bool _fabOpen = false;

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  List<DeviceResponse> _devices(BuildContext context) =>
      context.watch<DevicesProvider>().searchResult.items;

  /// Jedan uređaj po psu (prvi), za izbor psa.
  List<DeviceResponse> _distinctDogDevices(List<DeviceResponse> devices) {
    final seen = <int>{};
    return devices.where((d) => d.dog != null && seen.add(d.dogId)).toList();
  }

  Widget _build(BuildContext context) {
    final devices = _devices(context);
    final dogDevices = _distinctDogDevices(devices);
    final selected = dogDevices.where((d) => d.dogId == _dogId).firstOrNull;

    return PageWrapper(
      showAppBar: false,
      floatingActionButton: selected == null
          ? null
          : _buildSpeedDial(context, selected),
      body: RefreshIndicator(
        onRefresh: _reload,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.only(bottom: 96),
          children: [
            if (selected == null)
              _buildEmpty()
            else ...[
              _buildDogChips(dogDevices),
              const SizedBox(height: 8),
              _buildDogHeader(context, selected),
              _buildWeightCard(context, selected),
              _buildBudgetCard(context),
              _buildDeviceCard(
                devices.where((d) => d.dogId == selected.dogId).toList(),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildEmpty() {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 32),
      child: Column(
        spacing: 12,
        children: [
          const Icon(Icons.pets, size: 48),
          const Text("Nemate dodanih uređaja."),
          PrimaryButton(
            text: "Dodaj uređaj",
            onPressed: () async {
              final added = await Constants.navigatorKey.currentState?.push(
                MaterialPageRoute(builder: (_) => DeviceWizard()),
              );
              if (added == true && mounted) await _reload();
            },
          ),
        ],
      ),
    );
  }

  Widget _buildDogChips(List<DeviceResponse> dogDevices) {
    if (dogDevices.length < 2) return const SizedBox.shrink();
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        spacing: 8,
        children: dogDevices
            .map(
              (d) => ChoiceChip(
                label: Text(d.dog!.name),
                selected: d.dogId == _dogId,
                onSelected: (_) async {
                  setState(() => _dogId = d.dogId);
                  await _loadDogData();
                },
              ),
            )
            .toList(),
      ),
    );
  }

  Widget _buildDogHeader(BuildContext context, DeviceResponse device) {
    final dog = device.dog!;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  dog.name,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                Text(
                  [
                    dog.breed?.name,
                    dogGenderMap[dog.gender],
                    _formatAge(dog.dateOfBirth),
                  ].whereType<String>().join(" • "),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: "Uredi psa",
            icon: const Icon(Icons.edit_outlined),
            onPressed: () async {
              final saved = await Constants.navigatorKey.currentState?.push(
                MaterialPageRoute(builder: (_) => DogForm(dog: dog)),
              );
              if (saved == true && mounted) await _reload();
            },
          ),
        ],
      ),
    );
  }

  Widget _buildWeightCard(BuildContext context, DeviceResponse device) {
    final last = device.lastWeightRecord;
    final isOutdated =
        last == null ||
        DateTime.now().difference(last.createdAt) > _weightMaxAge;
    return Card(
      child: InkWell(
        onTap: () async => await Constants.navigatorKey.currentState?.push(
          MaterialPageRoute(
            builder: (_) => DogWeightHistoryPage(
              dogId: device.dogId,
              dogName: device.dog!.name,
            ),
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            spacing: 6,
            children: [
              const Text("Težina"),
              Text(
                last == null ? "—" : "${last.weight} kg",
                style: Theme.of(context).textTheme.headlineMedium,
              ),
              if (last != null)
                Text(
                  "BCS: ${last.bcs} • Izmjereno: ${context.getLocalizedDate(last.createdAt)}",
                ),
              if (isOutdated)
                Row(
                  spacing: 4,
                  children: [
                    const Icon(
                      Icons.warning_amber_rounded,
                      color: Colors.orange,
                      size: 18,
                    ),
                    Expanded(
                      child: Text(
                        last == null
                            ? "Nema zabilježenih mjerenja težine."
                            : "Težina nije mjerena duže od 7 dana.",
                        style: const TextStyle(color: Colors.orange),
                      ),
                    ),
                  ],
                ),
              WeightSparkline(values: _weights.map((w) => w.weight).toList()),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBudgetCard(BuildContext context) {
    final budget = _budget;
    final pieces = _todayLogs.fold<int>(0, (sum, l) => sum + l.quantity);
    return Card(
      child: InkWell(
        onTap: () => MainShell.goToTab(2),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: budget == null
              ? Row(
                  spacing: 8,
                  children: [
                    const Icon(Icons.info_outline),
                    Expanded(
                      child: Text(_budgetError ?? "Budžet nije dostupan."),
                    ),
                  ],
                )
              : _budgetContent(context, budget, pieces),
        ),
      ),
    );
  }

  Widget _budgetContent(
    BuildContext context,
    CalorieBudgetResponse budget,
    int pieces,
  ) {
    final isOver = budget.remaining < 0;
    final progress = budget.dailyBudget <= 0
        ? 0.0
        : (budget.treatCalories / budget.dailyBudget).clamp(0.0, 1.0);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      spacing: 6,
      children: [
        const Text("Budžet kalorija danas"),
        Text(
          "${budget.remaining} kcal preostalo",
          style: Theme.of(context).textTheme.headlineSmall
              ?.copyWith(color: isOver ? Colors.red : null),
        ),
        LinearProgressIndicator(
          value: progress,
          color: isOver ? Colors.red : context.primaryColor,
        ),
        Text(
          "Poslastice: ${budget.treatCalories} / ${budget.dailyBudget} kcal"
          "${pieces > 0 ? " • $pieces kom." : ""}",
        ),
        if (isOver)
          const Text(
            "Dnevni budžet kalorija je prekoračen!",
            style: TextStyle(color: Colors.red),
          ),
      ],
    );
  }

  Widget _buildDeviceCard(List<DeviceResponse> dogDevices) {
    return Card(
      child: Column(
        children: dogDevices
            .map(
              (d) => ListTile(
                dense: true,
                leading: Icon(
                  d.isConnected ? Icons.sensors : Icons.sensors_off,
                  color: d.isConnected ? Colors.green : Colors.grey,
                ),
                title: Text(d.isConnected ? "Povezan" : "Nije povezan"),
                subtitle: Text(d.macAddress),
              ),
            )
            .toList(),
      ),
    );
  }

  Widget _buildSpeedDial(BuildContext context, DeviceResponse device) {
    Widget action(String label, IconData icon, Future<void> Function() onTap) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        spacing: 8,
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              child: Text(label),
            ),
          ),
          FloatingActionButton.small(
            heroTag: label,
            onPressed: () async {
              setState(() => _fabOpen = false);
              await onTap();
            },
            child: Icon(icon),
          ),
        ],
      );
    }

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.end,
      spacing: 12,
      children: [
        if (_fabOpen) ...[
          action("Dodaj poslasticu", Icons.cookie_outlined, () async {
            final added = await Constants.navigatorKey.currentState?.push(
              MaterialPageRoute(
                builder: (_) => TreatLogForm(
                  dogId: device.dogId,
                  dogName: device.dog!.name,
                ),
              ),
            );
            if (added == true && mounted) await _loadDogData();
          }),
          action("Dodaj težinu", Icons.monitor_weight_outlined, () async {
            final added = await Constants.navigatorKey.currentState?.push(
              MaterialPageRoute(builder: (_) => DogWeightForm(device: device)),
            );
            if (added == true && mounted) await _loadDogData();
          }),
        ],
        FloatingActionButton(
          heroTag: "home_fab",
          onPressed: () => setState(() => _fabOpen = !_fabOpen),
          child: Icon(_fabOpen ? Icons.close : Icons.add),
        ),
      ],
    );
  }

  String? _formatAge(DateTime dateOfBirth) {
    final now = DateTime.now();
    var months =
        (now.year - dateOfBirth.year) * 12 + now.month - dateOfBirth.month;
    if (now.day < dateOfBirth.day) months--;
    if (months < 0) return null;
    final years = months ~/ 12;
    final rest = months % 12;
    if (years == 0) return "$rest mj.";
    return rest == 0 ? "$years god." : "$years god. $rest mj.";
  }

  Future<bool> _init() async {
    await context.read<AuthProvider>().checkAuth();
    if (!mounted) return false;
    await _loadDevices();
    await _loadDogData(rebuild: false);
    return true;
  }

  Future<void> _reload() async {
    await _loadDevices();
    await _loadDogData();
  }

  Future<void> _loadDevices() async {
    final provider = context.read<DevicesProvider>();
    final search = DeviceSearchObject(page: 1, pageSize: 100);
    provider.clearData(search);
    await provider.fetchData(search);

    final dogDevices = _distinctDogDevices(provider.searchResult.items);
    if (dogDevices.every((d) => d.dogId != _dogId)) {
      _dogId = dogDevices.firstOrNull?.dogId;
    }
  }

  Future<void> _loadDogData({bool rebuild = true}) async {
    final dogId = _dogId;
    if (dogId == null) {
      _budget = null;
      _todayLogs = [];
      _weights = [];
      if (rebuild && mounted) setState(() {});
      return;
    }

    final now = DateTime.now();
    final from = DateTime(now.year, now.month, now.day);
    final to = DateTime(now.year, now.month, now.day + 1);

    CalorieBudgetResponse? budget;
    String? budgetError;
    try {
      budget = await context.read<DogsProvider>().getCalorieBudget(
        dogId,
        from: from,
        to: to,
      );
    } on DioException catch (e) {
      final data = e.response?.data;
      budgetError = data is Map ? data["message"] as String? : null;
      budgetError ??= "Budžet nije moguće izračunati.";
    }
    if (!mounted) return;

    final logsProvider = context.read<TreatLogsProvider>();
    final logsSearch = TreatLogSearchObject(
      page: 1,
      pageSize: 100,
      dogId: dogId,
      from: from.toUtc(),
      to: to.toUtc(),
    );
    logsProvider.clearData(logsSearch);
    await logsProvider.fetchData(logsSearch);
    if (!mounted) return;

    final weightsProvider = context.read<DogWeightsProvider>();
    final weightsSearch = DogWeightSearchObject(
      page: 1,
      pageSize: _sparklinePoints,
      dogId: dogId,
      orderBy: "createdAt",
      descending: true,
    );
    weightsProvider.clearData(weightsSearch);
    await weightsProvider.fetchData(weightsSearch);

    _budget = budget;
    _budgetError = budgetError;
    _todayLogs = List.of(logsProvider.searchResult.items);
    _weights = weightsProvider.searchResult.items.reversed.toList();
    if (rebuild && mounted) setState(() {});
  }
}
