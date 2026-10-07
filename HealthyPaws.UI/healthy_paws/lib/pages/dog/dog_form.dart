import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/components/dog_bcs_field/dog_bcs_field.dart';
import 'package:healthy_paws/providers/breeds_provider.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_activity_status.dart';
import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';
import 'package:healthy_paws/resources/responses/breed/breed_response.dart';
import 'package:healthy_paws/resources/search_objects/breed/breed_search_object.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/inputs/form_builder_dropdown.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DogForm extends StatefulWidget {
  final int? id;
  const DogForm({super.key, this.id});

  @override
  State<StatefulWidget> createState() => _DogFormState();
}

class _DogFormState extends State<DogForm> {
  final _formKey = GlobalKey<FormBuilderState>();
  var _initialValue = <String, dynamic>{};

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    return PageWrapper(
      appBarTitle: widget.id != null ? "Uredi psa" : "Dodaj psa",
      body: FormBuilder(
        key: _formKey,
        initialValue: _initialValue,
        child: Column(
          spacing: 12,
          children: [
            PaginatedDropdownField<
              BreedResponse,
              BreedSearchObject,
              BreedsProvider
            >(
              label: "Rasa",
              name: "breedId",
              searchObject: BreedSearchObject(page: 0, pageSize: 10),
              itemAsString: (item) => item.name,
              compareFn: (a, b) => a.id == b.id,
              validator: FormBuilderValidators.required(
                errorText: "Rasa je obavezna!",
              ),
            ),
            FormBuilderTextField(
              name: "name",
              decoration: InputDecoration(labelText: "Ime"),
              validator: FormBuilderValidators.compose([
                FormBuilderValidators.required(errorText: "Ime je obavezno!"),
                FormBuilderValidators.range(
                  1,
                  16,
                  errorText:
                      "Ime ne smije biti prazno niti veće od 16 karaktera!",
                ),
              ]),
            ),
            FormBuilderDateTimePicker(
              name: "dateOfBirth",
              decoration: InputDecoration(labelText: "Datum rođenja"),
              keyboardType: TextInputType.datetime,
              inputType: InputType.date,
              firstDate: DateTime(2000),
              lastDate: DateTime.now(),
              validator: FormBuilderValidators.required(
                errorText: "Datum rođenja je obavezan!",
              ),
            ),
            FormBuilderDropdown<DogGender>(
              name: "gender",
              decoration: InputDecoration(labelText: "Spol"),
              items: dogGenderMap.entries
                  .map(
                    (i) => DropdownMenuItem(value: i.key, child: Text(i.value)),
                  )
                  .toList(),
              validator: FormBuilderValidators.required(
                errorText: "Spol je obavezan!",
              ),
            ),
            FormBuilderDropdown<double>(
              name: "activityStatus",
              decoration: InputDecoration(labelText: "Aktivnost"),
              items: dogActivityStatusMap.entries
                  .map(
                    (i) => DropdownMenuItem(value: i.key, child: Text(i.value)),
                  )
                  .toList(),
              validator: FormBuilderValidators.required(
                errorText: "Aktivnost je obavezna!",
              ),
            ),
            Column(
              children: [
                const Row(
                  mainAxisAlignment: MainAxisAlignment.start,
                  children: [Text("Izgled psa")],
                ),
                DogBcsFormField(name: "bcs"),
              ],
            ),
            FormBuilderCheckbox(
              initialValue: false,
              name: "isNeutered",
              title: const Text("Kastriran?"),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                ElevatedButton(
                  onPressed: () =>
                      Constants.navigatorKey.currentState?.pop(false),
                  child: const Text("Otkaži"),
                ),
                PrimaryButton(
                  text: "Spremi",
                  onPressed: () async => await _save(),
                ),
              ],
            ),
            SizedBox.shrink(),
          ],
        ),
      ),
    );
  }

  Future<void> _save() async {
    if (_formKey.currentState?.saveAndValidate() ?? false) {
      Constants.navigatorKey.currentState?.pop(true);
    }
  }

  Future<bool> _init() async {
    if (widget.id != null && mounted) {
      _initialValue = (await context.read<DogsProvider>().getById(widget.id!))
          .toJson();
    }
    return true;
  }
}
