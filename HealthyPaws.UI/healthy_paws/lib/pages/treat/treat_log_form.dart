import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/pages/treat/treat_form.dart';
import 'package:healthy_paws/providers/treat_logs_provider.dart';
import 'package:healthy_paws/providers/treats_provider.dart';
import 'package:healthy_paws/resources/requests/treat_log/treat_log_insert_request.dart';
import 'package:healthy_paws/resources/responses/treat/treat_response.dart';
import 'package:healthy_paws/resources/search_objects/treat/treat_search_object.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/inputs/form_builder_dropdown.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class TreatLogForm extends StatefulWidget {
  final int dogId;
  final String dogName;
  const TreatLogForm({super.key, required this.dogId, required this.dogName});

  @override
  State<StatefulWidget> createState() => _TreatLogFormState();
}

class _TreatLogFormState extends State<TreatLogForm> {
  final _formKey = GlobalKey<FormBuilderState>();

  @override
  Widget build(BuildContext context) {
    return PageWrapper(
      appBarTitle: "Poslastica za ${widget.dogName}",
      body: SingleChildScrollView(
        child: Column(
          spacing: 12,
          children: [
            FormBuilder(
              key: _formKey,
              initialValue: {"quantity": "1", "givenAt": DateTime.now()},
              child: Column(
                spacing: 12,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    spacing: 8,
                    children: [
                      Expanded(
                        child:
                            PaginatedDropdownField<
                              TreatResponse,
                              TreatSearchObject,
                              TreatsProvider
                            >(
                              label: "Poslastica",
                              name: "treatId",
                              searchObject: TreatSearchObject(
                                page: 1,
                                pageSize: 10,
                              ),
                              itemAsString: (item) =>
                                  "${item.name} (${item.caloriesPerPiece} kcal)",
                              validator: FormBuilderValidators.required(
                                errorText: "Poslastica je obavezna!",
                              ),
                              valueTransformer: (i) => i?.id,
                            ),
                      ),
                      IconButton.filled(
                        tooltip: "Nova poslastica",
                        onPressed: _addTreat,
                        icon: const Icon(Icons.add),
                      ),
                    ],
                  ),
                  FormBuilderTextField(
                    name: "quantity",
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: "Količina",
                      suffixText: "komada",
                    ),
                    valueTransformer: (value) => int.parse(value!),
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.required(
                        errorText: "Količina je obavezna!",
                      ),
                      FormBuilderValidators.integer(
                        errorText: "Količina mora biti cijeli broj!",
                      ),
                      FormBuilderValidators.min(
                        1,
                        errorText: "Količina mora biti barem 1!",
                      ),
                    ]),
                  ),
                  FormBuilderDateTimePicker(
                    name: "givenAt",
                    decoration: const InputDecoration(
                      labelText: "Datum i vrijeme davanja",
                      helperText:
                          "Ako ostane prazno, koristi se trenutno vrijeme.",
                    ),
                    valueTransformer: (value) =>
                        value?.toUtc().toIso8601String(),
                    validator: (value) {
                      if (value != null && value.isAfter(DateTime.now())) {
                        return "Vrijeme davanja ne može biti u budućnosti!";
                      }
                      return null;
                    },
                  ),
                ],
              ),
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
          ],
        ),
      ),
    );
  }

  Future<void> _addTreat() async {
    final created = await Constants.navigatorKey.currentState
        ?.push<TreatResponse>(
          MaterialPageRoute(builder: (_) => const TreatForm()),
        );
    if (created != null) {
      _formKey.currentState?.fields["treatId"]?.didChange(created);
    }
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.saveAndValidate() ?? false)) return;
    final request = TreatLogInsertRequest.fromJson({
      ..._formKey.currentState!.value,
      "dogId": widget.dogId,
    });
    await context.read<TreatLogsProvider>().insert(request);
    Constants.navigatorKey.currentState?.pop(true);
  }
}
