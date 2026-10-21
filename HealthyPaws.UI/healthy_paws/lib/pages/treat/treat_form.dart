import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/providers/treats_provider.dart';
import 'package:healthy_paws/resources/requests/treat/treat_insert_request.dart';
import 'package:healthy_paws/resources/responses/treat/treat_response.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class TreatForm extends StatefulWidget {
  final TreatResponse? treat;
  const TreatForm({super.key, this.treat});

  @override
  State<StatefulWidget> createState() => _TreatFormState();
}

class _TreatFormState extends State<TreatForm> {
  final _formKey = GlobalKey<FormBuilderState>();

  @override
  Widget build(BuildContext context) {
    final treat = widget.treat;
    return PageWrapper(
      appBarTitle: treat != null ? "Uredi poslasticu" : "Dodaj poslasticu",
      body: SingleChildScrollView(
        child: Column(
          spacing: 12,
          children: [
            FormBuilder(
              key: _formKey,
              initialValue: {
                if (treat != null) ...{
                  "name": treat.name,
                  "caloriesPerPiece": treat.caloriesPerPiece.toString(),
                },
              },
              child: Column(
                spacing: 12,
                children: [
                  FormBuilderTextField(
                    name: "name",
                    decoration: const InputDecoration(labelText: "Naziv"),
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.required(
                        errorText: "Naziv je obavezan!",
                      ),
                      FormBuilderValidators.maxLength(
                        64,
                        errorText: "Naziv ne smije biti duži od 64 karaktera!",
                      ),
                    ]),
                  ),
                  FormBuilderTextField(
                    name: "caloriesPerPiece",
                    keyboardType: const TextInputType.numberWithOptions(
                      decimal: true,
                    ),
                    decoration: const InputDecoration(
                      labelText: "Kalorije",
                      suffixText: "kcal/komad",
                    ),
                    valueTransformer: (value) =>
                        double.parse(value!.replaceAll(",", ".")),
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.required(
                        errorText: "Kalorije su obavezne!",
                      ),
                      FormBuilderValidators.positiveNumber(
                        errorText: "Kalorije moraju biti veće od 0!",
                      ),
                    ]),
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

  Future<void> _save() async {
    if (!(_formKey.currentState?.saveAndValidate() ?? false)) return;
    final request = TreatInsertRequest.fromJson(_formKey.currentState!.value);
    final provider = context.read<TreatsProvider>();
    final treat = widget.treat;
    if (treat == null) {
      await provider.insert(request);
    } else {
      await provider.update(treat.id, request);
    }
    Constants.messengerKey.currentState?.showSnackBar(
      SuccessSnackbar(
        message: treat == null
            ? "Uspješno ste dodali poslasticu!"
            : "Uspješno ste uredili poslasticu!",
      ),
    );
    Constants.navigatorKey.currentState?.pop(true);
  }
}
