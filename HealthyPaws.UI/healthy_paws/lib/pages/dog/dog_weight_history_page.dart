import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/providers/dog_weights_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_body_condition_score.dart';
import 'package:healthy_paws/resources/responses/dog_weight/dog_weight_response.dart';
import 'package:healthy_paws/resources/search_objects/dog_weight/dog_weight_search_object.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DogWeightHistoryPage extends StatefulWidget {
  final int dogId;
  final String dogName;
  const DogWeightHistoryPage({
    super.key,
    required this.dogId,
    required this.dogName,
  });

  @override
  State<StatefulWidget> createState() => _DogWeightHistoryPageState();
}

class _DogWeightHistoryPageState extends State<DogWeightHistoryPage> {
  static const _pageSize = 20;
  static const _orderOptions = {
    "createdAt": "Datum",
    "weight": "Težina",
    "bcs": "BCS",
  };

  final _minWeightController = TextEditingController();
  final _maxWeightController = TextEditingController();
  DateTime? _from;
  DateTime? _to;
  int? _bcs;
  String _orderBy = "createdAt";
  bool _descending = true;
  final _scrollController = ScrollController();
  bool _isLoadingMore = false;
  late DogWeightSearchObject _search;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _minWeightController.dispose();
    _maxWeightController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    final provider = context.watch<DogWeightsProvider>();
    final items = provider.searchResult.items;
    return PageWrapper(
      appBarTitle: "Težine: ${widget.dogName}",
      body: Column(
        spacing: 8,
        children: [
          _buildFilters(context),
          _buildOrdering(),
          Expanded(
            child: items.isEmpty
                ? const Center(child: Text("Nema zabilježenih mjerenja."))
                : ListView.separated(
                    controller: _scrollController,
                    itemCount:
                        items.length + (provider.searchResult.hasMore ? 1 : 0),
                    separatorBuilder: (_, _) => const SizedBox(height: 8),
                    itemBuilder: (_, index) {
                      if (index == items.length) {
                        return const Padding(
                          padding: EdgeInsets.all(12),
                          child: Center(child: CircularProgressIndicator()),
                        );
                      }
                      return _buildItem(context, items[index]);
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilters(BuildContext context) {
    return Card(
      child: ExpansionTile(
        title: const Text("Filteri"),
        leading: const Icon(Icons.filter_list),
        childrenPadding: const EdgeInsets.all(12),
        children: [
          Column(
            spacing: 12,
            children: [
              Row(
                spacing: 8,
                children: [
                  Expanded(
                    child: _dateButton(
                      label: "Od datuma",
                      value: _from,
                      onPicked: (d) => setState(() => _from = d),
                    ),
                  ),
                  Expanded(
                    child: _dateButton(
                      label: "Do datuma",
                      value: _to,
                      onPicked: (d) => setState(() => _to = d),
                    ),
                  ),
                ],
              ),
              Row(
                spacing: 8,
                children: [
                  Expanded(child: _weightField(_minWeightController, "Min kg")),
                  Expanded(child: _weightField(_maxWeightController, "Max kg")),
                ],
              ),
              DropdownButtonFormField<int?>(
                initialValue: _bcs,
                decoration: const InputDecoration(labelText: "BCS"),
                items: [
                  const DropdownMenuItem<int?>(value: null, child: Text("Svi")),
                  ...dogBodyConditionScoreMap.keys.map(
                    (k) => DropdownMenuItem<int?>(value: k, child: Text("$k")),
                  ),
                ],
                onChanged: (v) => setState(() => _bcs = v),
              ),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  TextButton(onPressed: _reset, child: const Text("Resetuj")),
                  FilledButton(
                    onPressed: _apply,
                    child: const Text("Primijeni"),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _dateButton({
    required String label,
    required DateTime? value,
    required void Function(DateTime?) onPicked,
  }) {
    return OutlinedButton(
      onPressed: () async {
        final picked = await showDatePicker(
          context: context,
          initialDate: value ?? DateTime.now(),
          firstDate: DateTime(2000),
          lastDate: DateTime.now(),
        );
        if (picked != null) onPicked(picked);
      },
      onLongPress: () => onPicked(null),
      child: Text(value == null ? label : context.getLocalizedDate(value)),
    );
  }

  Widget _weightField(TextEditingController controller, String label) {
    return TextField(
      controller: controller,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      decoration: InputDecoration(labelText: label),
    );
  }

  Widget _buildOrdering() {
    return Row(
      spacing: 8,
      children: [
        const Text("Sortiraj po:"),
        DropdownButton<String>(
          value: _orderBy,
          items: _orderOptions.entries
              .map((e) => DropdownMenuItem(value: e.key, child: Text(e.value)))
              .toList(),
          onChanged: (v) async {
            if (v == null) return;
            setState(() => _orderBy = v);
            await _apply();
          },
        ),
        IconButton(
          tooltip: _descending ? "Opadajuće" : "Rastuće",
          icon: Icon(_descending ? Icons.arrow_downward : Icons.arrow_upward),
          onPressed: () async {
            setState(() => _descending = !_descending);
            await _apply();
          },
        ),
      ],
    );
  }

  Widget _buildItem(BuildContext context, DogWeightResponse item) {
    return Card(
      child: ListTile(
        leading: const Icon(Icons.monitor_weight_outlined),
        title: Text("${item.weight} kg"),
        subtitle: Text(
          "BCS: ${item.bcs} • ${item.calorie} kcal/100g\n"
          "${context.getLocalizedDate(item.createdAt)} ${context.getLocalizedTime(item.createdAt)}",
        ),
        isThreeLine: true,
      ),
    );
  }

  Future<bool> _init() async {
    _search = _buildSearch();
    await _load();
    return true;
  }

  DogWeightSearchObject _buildSearch() {
    double? parse(TextEditingController c) =>
        double.tryParse(c.text.trim().replaceAll(",", "."));
    return DogWeightSearchObject(
      page: 1,
      pageSize: _pageSize,
      dogId: widget.dogId,
      from: _from == null
          ? null
          : DateTime(_from!.year, _from!.month, _from!.day).toUtc(),
      to: _to == null
          ? null
          : DateTime(_to!.year, _to!.month, _to!.day + 1).toUtc(),
      minWeight: parse(_minWeightController),
      maxWeight: parse(_maxWeightController),
      bcs: _bcs,
      orderBy: _orderBy,
      descending: _descending,
    );
  }

  Future<void> _load() async {
    final provider = context.read<DogWeightsProvider>();
    provider.clearData(_search);
    await provider.fetchData(_search);
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - 200) _loadMore();
  }

  Future<void> _loadMore() async {
    final provider = context.read<DogWeightsProvider>();
    if (_isLoadingMore || !provider.searchResult.hasMore) return;
    _isLoadingMore = true;
    try {
      await provider.fetchData(_search);
    } finally {
      _isLoadingMore = false;
    }
    // Ako lista još ne popunjava ekran, nastavi s učitavanjem.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _onScroll();
    });
  }

  Future<void> _apply() async {
    _search = _buildSearch();
    await _load();
  }

  Future<void> _reset() async {
    _minWeightController.clear();
    _maxWeightController.clear();
    setState(() {
      _from = null;
      _to = null;
      _bcs = null;
    });
    await _apply();
  }
}
