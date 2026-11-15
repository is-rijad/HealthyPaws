import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/providers/agent_alerts_provider.dart';
import 'package:healthy_paws/providers/agent_logs_provider.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/resources/responses/agent/agent_alert_response.dart';
import 'package:healthy_paws/resources/responses/agent/agent_decision_log_response.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/search_objects/agent/agent_alert_search_object.dart';
import 'package:healthy_paws/resources/search_objects/agent/agent_decision_log_search_object.dart';
import 'package:healthy_paws/resources/search_objects/dog/dog_search_object.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

/// Pregled odluka kalorijskog agenta i alerta za odabranog psa.
class AgentInsightsPage extends StatefulWidget {
  const AgentInsightsPage({super.key});

  @override
  State<StatefulWidget> createState() => _AgentInsightsPageState();
}

class _AgentInsightsPageState extends State<AgentInsightsPage> {
  static const _pageSize = 10;

  static const _actionLabels = {
    "NoChange": "Bez promjene",
    "Increase": "Povećanje obroka",
    "Decrease": "Smanjenje obroka",
    "SetInitial": "Početni plan",
    "Escalate": "Potrebna provjera",
    "RequestRemeasure": "Ponovite vaganje",
  };

  final _dogsSearchObject = DogSearchObject(page: 1, pageSize: 100);
  List<DogResponse> _dogs = [];
  DogResponse? _dog;
  final _alertsScroll = ScrollController();
  final _logsScroll = ScrollController();
  bool _isLoadingMoreAlerts = false;
  bool _isLoadingMoreLogs = false;
  late AgentAlertSearchObject _alertsSearch;
  late AgentDecisionLogSearchObject _logsSearch;

  @override
  void initState() {
    super.initState();
    _alertsScroll.addListener(() => _onScroll(_alertsScroll, _loadMoreAlerts));
    _logsScroll.addListener(() => _onScroll(_logsScroll, _loadMoreLogs));
  }

  @override
  void dispose() {
    _alertsScroll.dispose();
    _logsScroll.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    return PageWrapper(
      showAppBar: false,
      appBarTitle: "Obavijesti",
      body: _dog == null
          ? const Center(child: Text("Nemate dodanih pasa. Dodajte uređaj."))
          : DefaultTabController(
              length: 2,
              child: Column(
                spacing: 12,
                children: [
                  _buildDogPicker(),
                  TabBar(
                    tabs: const [
                      Tab(text: "Upozorenja"),
                      Tab(text: "Logovi"),
                    ],
                  ),
                  Expanded(
                    child: TabBarView(
                      children: [_buildAlerts(context), _buildLogs(context)],
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _buildDogPicker() {
    return DropdownButtonFormField<int>(
      initialValue: _dog!.id,
      decoration: const InputDecoration(labelText: "Pas"),
      items: _dogs
          .map((d) => DropdownMenuItem(value: d.id, child: Text(d.name)))
          .toList(),
      onChanged: (id) async {
        if (id == null) return;
        setState(() => _dog = _dogs.firstWhere((d) => d.id == id));
        await _loadData();
      },
    );
  }

  Widget _buildAlerts(BuildContext context) {
    final result = context.watch<AgentAlertsProvider>().searchResult;
    final alerts = result.items;
    if (alerts.isEmpty) {
      return const Center(child: Text("Nema upozorenja za ovog psa."));
    }
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView.separated(
        controller: _alertsScroll,
        physics: const AlwaysScrollableScrollPhysics(),
        itemCount: alerts.length + (result.hasMore ? 1 : 0),
        separatorBuilder: (_, _) => const SizedBox(height: 8),
        itemBuilder: (_, index) {
          if (index == alerts.length) return _loader();
          final alert = alerts[index];
          return Card(
            child: ListTile(
              isThreeLine: true,
              leading: Icon(
                alert.isOpen
                    ? _categoryIcon(alert.category)
                    : Icons.check_circle_outline,
                color: alert.isOpen
                    ? _categoryColor(alert.category)
                    : Colors.green,
              ),
              title: Text(alert.explanation),
              subtitle: Text(
                "${alert.isOpen ? "Preporuka: ${alert.recommendedAction}\n" : ""}"
                "Zadnji put: ${context.getLocalizedDateTime(alert.lastSeenAt)}"
                "${alert.occurrences > 1 ? " • ${alert.occurrences}x" : ""}"
                "${alert.isOpen ? "" : "\nRiješeno ${context.getLocalizedDateTime(alert.resolvedAt!)} (${alert.resolvedBy == "User" ? "korisnik" : "agent"})"}",
              ),
              trailing: alert.isOpen
                  ? TextButton(
                      onPressed: () => _resolve(alert),
                      child: const Text("Riješi"),
                    )
                  : null,
            ),
          );
        },
      ),
    );
  }

  Widget _loader() => const Padding(
    padding: EdgeInsets.all(12),
    child: Center(child: CircularProgressIndicator()),
  );

  Widget _buildLogs(BuildContext context) {
    final result = context.watch<AgentLogsProvider>().searchResult;
    final logs = result.items;
    if (logs.isEmpty) {
      return const Center(child: Text("Još nema zabilježenih odluka."));
    }
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView.separated(
        controller: _logsScroll,
        physics: const AlwaysScrollableScrollPhysics(),
        itemCount: logs.length + (result.hasMore ? 1 : 0),
        separatorBuilder: (_, _) => const SizedBox(height: 8),
        itemBuilder: (_, index) {
          if (index == logs.length) return _loader();
          final log = logs[index];
          final kcal = log.newDailyKcal;
          return Card(
            child: ExpansionTile(
              leading: Icon(_actionIcon(log), color: _actionColor(log)),
              title: Text(_actionLabels[log.action] ?? log.action),
              subtitle: Text(
                "Mjerenje: ${context.getLocalizedDateTime(log.measuredAt)}",
              ),
              childrenPadding: const EdgeInsets.all(12),
              expandedCrossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(log.explanation),
                if (kcal != null)
                  Text(
                    "Plan: ${kcal.round()} kcal/dan"
                    "${log.newDailyGrams == null ? "" : " (${log.newDailyGrams!.round()} g)"}",
                  ),
                if (log.isRejected)
                  const Text(
                    "Mjerenje je odbijeno, potrebno je ponovno vaganje.",
                  ),
              ],
            ),
          );
        },
      ),
    );
  }

  IconData _categoryIcon(String category) => switch (category) {
    "ProvjeraPodataka" => Icons.fact_check_outlined,
    "RucnaProcjena" => Icons.edit_note,
    _ => Icons.medical_services_outlined,
  };

  Color _categoryColor(String category) => switch (category) {
    "ProvjeraPodataka" => Colors.orange,
    "RucnaProcjena" => Colors.blue,
    _ => Colors.red,
  };

  IconData _actionIcon(AgentDecisionLogResponse log) => switch (log.action) {
    "Increase" => Icons.trending_up,
    "Decrease" => Icons.trending_down,
    "SetInitial" => Icons.flag_outlined,
    "Escalate" => Icons.warning_amber_rounded,
    "RequestRemeasure" => Icons.monitor_weight_outlined,
    _ => Icons.horizontal_rule,
  };

  Color? _actionColor(AgentDecisionLogResponse log) => switch (log.action) {
    "Escalate" => Colors.red,
    "RequestRemeasure" => Colors.orange,
    _ => null,
  };

  Future<void> _resolve(AgentAlertResponse alert) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text("Rješavanje upozorenja"),
        content: const Text("Označiti upozorenje kao riješeno?"),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text("Otkaži"),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text("Riješi"),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    await context.read<AgentAlertsProvider>().resolve(alert.id);
    if (mounted) await _loadData();
  }

  Future<bool> _init() async {
    final dogsProvider = context.read<DogsProvider>();
    dogsProvider.clearData(_dogsSearchObject);
    await dogsProvider.fetchData(_dogsSearchObject);
    _dogs = List.of(dogsProvider.searchResult.items);
    if (_dogs.isNotEmpty) {
      _dog = _dogs.first;
      await _loadData();
    }
    return true;
  }

  Future<void> _loadData() async {
    final dogId = _dog!.id;
    _alertsSearch = AgentAlertSearchObject(
      page: 1,
      pageSize: _pageSize,
      dogId: dogId,
    );
    _logsSearch = AgentDecisionLogSearchObject(
      page: 1,
      pageSize: _pageSize,
      dogId: dogId,
    );
    final alertsProvider = context.read<AgentAlertsProvider>();
    final logsProvider = context.read<AgentLogsProvider>();
    alertsProvider.clearData(_alertsSearch);
    logsProvider.clearData(_logsSearch);
    await alertsProvider.fetchData(_alertsSearch);
    await logsProvider.fetchData(_logsSearch);
  }

  void _onScroll(
    ScrollController controller,
    Future<void> Function() loadMore,
  ) {
    if (!controller.hasClients) return;
    final position = controller.position;
    if (position.pixels >= position.maxScrollExtent - 200) loadMore();
  }

  Future<void> _loadMoreAlerts() async {
    final provider = context.read<AgentAlertsProvider>();
    if (_isLoadingMoreAlerts || !provider.searchResult.hasMore) return;
    _isLoadingMoreAlerts = true;
    try {
      await provider.fetchData(_alertsSearch);
    } finally {
      _isLoadingMoreAlerts = false;
    }
  }

  Future<void> _loadMoreLogs() async {
    final provider = context.read<AgentLogsProvider>();
    if (_isLoadingMoreLogs || !provider.searchResult.hasMore) return;
    _isLoadingMoreLogs = true;
    try {
      await provider.fetchData(_logsSearch);
    } finally {
      _isLoadingMoreLogs = false;
    }
  }
}
