import 'package:dio/dio.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/pages/treat/treat_log_form.dart';
import 'package:healthy_paws/pages/treat/treats_page.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/providers/treat_logs_provider.dart';
import 'package:healthy_paws/resources/responses/dog/calorie_budget_response.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/responses/treat_log/treat_log_response.dart';
import 'package:healthy_paws/resources/search_objects/dog/dog_search_object.dart';
import 'package:healthy_paws/resources/search_objects/treat_log/treat_log_search_object.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class TreatTrackerPage extends StatefulWidget {
  const TreatTrackerPage({super.key});

  @override
  State<StatefulWidget> createState() => _TreatTrackerPageState();
}

class _TreatTrackerPageState extends State<TreatTrackerPage> {
  final _dogsSearchObject = DogSearchObject(page: 1, pageSize: 100);
  List<DogResponse> _dogs = [];
  DogResponse? _dog;
  CalorieBudgetResponse? _budget;
  String? _budgetError;
  List<TreatLogResponse> _logs = [];

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    return PageWrapper(
      showAppBar: false,
      appBarTitle: "Poslastice",
      body: _dog == null
          ? Column(
              spacing: 12,
              children: [
                const Expanded(
                  child: Center(
                    child: Text("Nemate dodanih pasa. Dodajte uređaj."),
                  ),
                ),
                _buildMyTreatsButton(),
              ],
            )
          : Column(
              spacing: 12,
              children: [
                _buildDogPicker(),
                _buildBudgetCard(context),
                Expanded(child: _buildLogs(context)),
                PrimaryButton(
                  text: "Dodaj poslasticu",
                  onPressed: _addTreatLog,
                ),
                _buildMyTreatsButton(),
              ],
            ),
    );
  }

  Widget _buildMyTreatsButton() {
    return OutlinedButton(
      onPressed: () async => await Constants.navigatorKey.currentState?.push(
        MaterialPageRoute(builder: (_) => const TreatsPage()),
      ),
      child: const Text("Moje poslastice"),
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
        await _loadDay();
      },
    );
  }

  Widget _buildBudgetCard(BuildContext context) {
    final budget = _budget;
    if (budget == null) {
      return Card(
        child: ListTile(
          leading: const Icon(Icons.info_outline),
          title: Text(_budgetError ?? "Budžet nije dostupan."),
        ),
      );
    }
    final isOver = budget.remaining < 0;
    final progress = budget.dailyBudget <= 0
        ? 0.0
        : ((budget.treatCalories + budget.foodCalories) / budget.dailyBudget).clamp(0.0, 1.0);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: 6,
          children: [
            Text("Dnevni budžet: ${budget.dailyBudget} kcal"),
            Text("Potrošeno na hranu danas: ${budget.foodCalories} kcal"),
            Text("Date poslastice danas: ${budget.treatCalories} kcal"),
            Text(
              "Preostalo: ${budget.remaining} kcal",
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: isOver ? Colors.red : null,
              ),
            ),
            LinearProgressIndicator(
              value: progress,
              color: isOver ? Colors.red : context.primaryColor,
            ),
            if (isOver)
              const Row(
                spacing: 4,
                children: [
                  Icon(
                    Icons.warning_amber_rounded,
                    color: Colors.red,
                    size: 18,
                  ),
                  Expanded(
                    child: Text(
                      "Dnevni budžet kalorija je prekoračen!",
                      style: TextStyle(color: Colors.red),
                    ),
                  ),
                ],
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildLogs(BuildContext context) {
    if (_logs.isEmpty) {
      return const Center(child: Text("Danas nije data nijedna poslastica."));
    }
    return ListView.separated(
      itemCount: _logs.length,
      separatorBuilder: (_, _) => const SizedBox(height: 8),
      itemBuilder: (_, index) {
        final log = _logs[index];
        return Card(
          child: ListTile(
            leading: const Icon(Icons.cookie_outlined),
            title: Text("${log.treatName} × ${log.quantity}"),
            subtitle: Text(
              "${log.calories} kcal • ${context.getLocalizedTime(log.givenAt)}",
            ),
            trailing: IconButton(
              icon: const Icon(Icons.delete_outline),
              onPressed: () => _deleteLog(log),
            ),
          ),
        );
      },
    );
  }

  Future<void> _addTreatLog() async {
    final added = await Constants.navigatorKey.currentState?.push(
      MaterialPageRoute(
        builder: (_) => TreatLogForm(dogId: _dog!.id, dogName: _dog!.name),
      ),
    );
    if (added == true && mounted) await _loadDay();
  }

  Future<void> _deleteLog(TreatLogResponse log) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text("Brisanje zapisa"),
        content: Text(
          "Obrisati \"${log.treatName} × ${log.quantity}\"? Kalorije će se vratiti u budžet.",
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text("Otkaži"),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text("Obriši"),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    await context.read<TreatLogsProvider>().delete(log.id);
    if (mounted) await _loadDay();
  }

  Future<bool> _init() async {
    final dogsProvider = context.read<DogsProvider>();
    dogsProvider.clearData(_dogsSearchObject);
    await dogsProvider.fetchData(_dogsSearchObject);
    _dogs = List.of(dogsProvider.searchResult.items);
    if (_dogs.isNotEmpty) {
      _dog = _dogs.first;
      await _loadDay(rebuild: false);
    }
    return true;
  }

  Future<void> _loadDay({bool rebuild = true}) async {
    final dog = _dog!;
    final now = DateTime.now();
    final from = DateTime(now.year, now.month, now.day);
    final to = DateTime(now.year, now.month, now.day + 1);

    CalorieBudgetResponse? budget;
    String? budgetError;
    try {
      budget = await context.read<DogsProvider>().getCalorieBudget(
        dog.id,
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
    final search = TreatLogSearchObject(
      page: 1,
      pageSize: 100,
      dogId: dog.id,
      from: from.toUtc(),
      to: to.toUtc(),
    );
    logsProvider.clearData(search);
    await logsProvider.fetchData(search);

    _budget = budget;
    _budgetError = budgetError;
    _logs = List.of(logsProvider.searchResult.items);
    if (rebuild && mounted) setState(() {});
  }
}
