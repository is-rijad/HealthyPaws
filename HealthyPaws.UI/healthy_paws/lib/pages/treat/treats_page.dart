import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/pages/treat/treat_form.dart';
import 'package:healthy_paws/providers/treats_provider.dart';
import 'package:healthy_paws/resources/responses/treat/treat_response.dart';
import 'package:healthy_paws/resources/search_objects/treat/treat_search_object.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class TreatsPage extends StatefulWidget {
  const TreatsPage({super.key});

  @override
  State<StatefulWidget> createState() => _TreatsPageState();
}

class _TreatsPageState extends State<TreatsPage> {
  final _searchObject = TreatSearchObject(page: 1, pageSize: 100);

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    final treats = context.watch<TreatsProvider>().searchResult.items;
    return PageWrapper(
      appBarTitle: "Moje poslastice",
      body: Column(
        spacing: 12,
        children: [
          Expanded(
            child: treats.isEmpty
                ? const Center(child: Text("Nemate dodanih poslastica."))
                : ListView.separated(
                    itemCount: treats.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 8),
                    itemBuilder: (_, index) => _buildCard(treats[index]),
                  ),
          ),
          PrimaryButton(
            text: "Dodaj poslasticu",
            onPressed: () => _openForm(null),
          ),
        ],
      ),
    );
  }

  Widget _buildCard(TreatResponse treat) {
    return Card(
      child: ListTile(
        onTap: () => _openForm(treat),
        leading: const Icon(Icons.cookie_outlined),
        title: Text(treat.name),
        subtitle: Text("${treat.caloriesPerPiece} kcal / komad"),
        trailing: IconButton(
          icon: const Icon(Icons.delete_outline),
          onPressed: () => _delete(treat),
        ),
      ),
    );
  }

  Future<void> _openForm(TreatResponse? treat) async {
    final saved = await Constants.navigatorKey.currentState?.push(
      MaterialPageRoute(builder: (_) => TreatForm(treat: treat)),
    );
    if (saved == true && mounted) await _refresh();
  }

  Future<void> _delete(TreatResponse treat) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text("Brisanje poslastice"),
        content: Text(
          "Da li ste sigurni da želite obrisati \"${treat.name}\"? Evidencija ranije datih poslastica ostaje sačuvana.",
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
    await context.read<TreatsProvider>().deleteWithEvent(treat.id);
  }

  Future<bool> _init() async {
    await _refresh();
    return true;
  }

  Future<void> _refresh() async {
    final provider = context.read<TreatsProvider>();
    provider.clearData(_searchObject);
    await provider.fetchData(_searchObject);
  }
}
