import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/providers/breeds_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_activity_status.dart';
import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';
import 'package:healthy_paws/resources/responses/breed/breed_response.dart';
import 'package:healthy_paws/resources/search_objects/breed/breed_search_object.dart';
import 'package:healthy_paws/widgets/inputs/form_builder_dropdown.dart';
import 'package:material_ui/material_ui.dart';

class DogFormComponent extends StatelessWidget {
  final GlobalKey<FormBuilderState> formKey;
  final Map<String, dynamic>? initialValue;
  final AutovalidateMode? autovalidateMode;
  final void Function()? onValidated;
  final void Function()? onChanged;

  const DogFormComponent({
    super.key,
    required this.formKey,
    this.initialValue,
    this.autovalidateMode,
    this.onValidated,
    this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return FormBuilder(
      autovalidateMode: autovalidateMode,
      onChanged: () {
        onChanged?.call();
        if (formKey.currentState?.isValid ?? false) {
          onValidated?.call();
        }
      },
      key: formKey,
      initialValue: initialValue ?? const <String, dynamic>{},
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
            validator: FormBuilderValidators.required(
              errorText: "Rasa je obavezna!",
            ),
            valueTransformer: (i) => i?.id,
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
            valueTransformer: (value) => value?.toIso8601String(),
          ),
          FormBuilderDropdown<int>(
            name: "gender",
            decoration: InputDecoration(labelText: "Spol"),
            items: dogGenderMap.entries
                .map(
                  (i) => DropdownMenuItem(
                    value: i.key.index,
                    child: Text(i.value),
                  ),
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
          FormBuilderCheckbox(
            initialValue: initialValue?["isNeutered"] ?? false,
            name: "isNeutered",
            title: const Text("Kastriran?"),
          ),
        ],
      ),
    );
  }
}
