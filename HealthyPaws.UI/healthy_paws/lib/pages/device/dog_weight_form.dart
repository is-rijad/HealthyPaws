import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/components/dog_weight/dog_weight_component.dart';
import 'package:healthy_paws/pages/main_shell.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/providers/dog_weights_provider.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_insert_request.dart';
import 'package:healthy_paws/resources/responses/agent/agent_decision_response.dart';
import 'package:healthy_paws/resources/responses/device/device_response.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DogWeightForm extends StatefulWidget {
  final DeviceResponse device;
  const DogWeightForm({super.key, required this.device});

  @override
  State<StatefulWidget> createState() => _DogWeightFormState();
}

class _DogWeightFormState extends State<DogWeightForm> {
  final _formKey = GlobalKey<FormBuilderState>();
  late final Map<String, dynamic> _initialValue;

  @override
  void initState() {
    super.initState();
    final last = widget.device.lastWeightRecord;
    _initialValue = {
      if (last != null) ...{
        "bcs": last.bcs,
        "weight": last.weight.toString(),
        "calorie": last.calorie.toString(),
      },
      "createdAt": DateTime.now(),
    };
  }

  @override
  Widget build(BuildContext context) {
    return PageWrapper(
      appBarTitle: widget.device.dog?.name ?? "Težina psa",
      body: SingleChildScrollView(
        child: Column(
          spacing: 12,
          children: [
            DogWeightComponent(
              formKey: _formKey,
              initialValue: _initialValue,
              showCreatedAt: true,
              newestCreatedAt: widget.device.lastWeightRecord?.createdAt,
              autovalidateMode: AutovalidateMode.onUserInteractionIfError,
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                ElevatedButton(
                  onPressed: () =>
                      Constants.navigatorKey.currentState?.pop(false),
                  child: const Text("Otkaži"),
                ),
                PrimaryButton(text: "Spremi", onPressed: _save),
              ],
            ),
            SizedBox.shrink(),
          ],
        ),
      ),
    );
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.saveAndValidate() ?? false)) return;
    final request = DogWeightInsertRequest.fromJson({
      ..._formKey.currentState!.value,
      "dogId": widget.device.dogId,
    });
    final added = await context.read<DogWeightsProvider>().insert(request);
    if (!mounted) return;
    if (added.isRejected) {
      final remeasure = await _showRemeasureDialog(added.agent?.explanation);
      if (!mounted) return;
      if (remeasure) return;
      Constants.navigatorKey.currentState?.pop(true);
      return;
    }
    widget.device.lastWeightRecord = added;
    context.read<DevicesProvider>().updateLocally(widget.device);
    final agent = added.agent;
    if (agent != null && agent.action == "Escalate") {
      final openAlerts = await _showEscalateDialog(agent.explanation);
      if (!mounted) return;
      Constants.navigatorKey.currentState?.pop(true);
      if (openAlerts) MainShell.goToTab(3);
      return;
    }
    Constants.messengerKey.currentState?.showSnackBar(
      SuccessSnackbar(message: _resultMessage(agent)),
    );
    Constants.navigatorKey.currentState?.pop(true);
  }

  String _resultMessage(AgentDecisionResponse? agent) {
    const base = "Uspješno ste dodali zapis težine psa!";
    if (agent == null) return base;
    final kcal = agent.newDailyKcal;
    if (agent.planChanged && kcal != null) {
      final grams = agent.newDailyGrams;
      return "$base Plan ishrane ažuriran: ${kcal.round()} kcal/dan"
          "${grams == null ? "" : " (${grams.round()} g)"}.";
    }
    return "$base ${agent.explanation}";
  }

  /// Vraća true ako korisnik želi otvoriti ekran s upozorenjima.
  Future<bool> _showEscalateDialog(String explanation) async {
    final openAlerts = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        title: const Text("Potrebna provjera"),
        content: Text(explanation),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text("Zatvori"),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text("Pogledaj upozorenja"),
          ),
        ],
      ),
    );
    return openAlerts ?? false;
  }

  /// Vraća true ako korisnik želi odmah ponoviti vaganje (forma ostaje otvorena).
  Future<bool> _showRemeasureDialog(String? explanation) async {
    final remeasure = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        title: const Text("Ponovite vaganje"),
        content: Text(
          explanation ??
              "Unesena težina izgleda sumnjivo. Molimo ponovite vaganje.",
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text("Zatvori"),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text("Ponovi vaganje"),
          ),
        ],
      ),
    );
    return remeasure ?? false;
  }
}
