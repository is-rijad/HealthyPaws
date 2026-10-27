import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/components/dog_bcs_field/dog_bcs_field.dart';
import 'package:material_ui/material_ui.dart';

class DogWeightComponent extends StatelessWidget {
  final GlobalKey<FormBuilderState> formKey;
  final Map<String, dynamic>? initialValue;
  final AutovalidateMode? autovalidateMode;
  final void Function()? onValidated;
  final void Function()? onChanged;
  final bool showCreatedAt;
  final bool showCalorie;
  final DateTime? newestCreatedAt;

  const DogWeightComponent({
    super.key,
    required this.formKey,
    this.initialValue,
    this.autovalidateMode,
    this.onValidated,
    this.onChanged,
    this.showCreatedAt = false,
    this.showCalorie = true,
    this.newestCreatedAt,
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
          Column(
            children: [
              const Text("Izgled Vašeg psa?"),
              DogBcsFormField(name: "bcs"),
            ],
          ),
          FormBuilderTextField(
            name: "weight",
            valueTransformer: (value) => double.parse(value!),
            keyboardType: .numberWithOptions(signed: true, decimal: true),
            decoration: InputDecoration(labelText: "Težina", suffixText: "kg"),
            validator: FormBuilderValidators.compose([
              FormBuilderValidators.required(errorText: "Težina je obavezna!"),
              FormBuilderValidators.positiveNumber(
                errorText: "Težina ne može biti negativna!",
              ),
            ]),
          ),
          if (showCalorie)
            FormBuilderTextField(
              keyboardType: .numberWithOptions(signed: true, decimal: true),
              valueTransformer: (value) => double.parse(value!),
              name: "calorie",
              decoration: InputDecoration(
                labelText: "Iznos kalorija hrane koju pas koristi",
                suffixText: "kcal/100g",
              ),
              validator: FormBuilderValidators.compose([
                FormBuilderValidators.required(
                  errorText: "Iznos kalorija je obavezan!",
                ),
                FormBuilderValidators.positiveNumber(
                  errorText: "Iznos kalorija ne može biti negativan!",
                ),
              ]),
            ),
          if (showCreatedAt)
            FormBuilderDateTimePicker(
              name: "createdAt",
              lastDate: DateTime.now(),
              firstDate: newestCreatedAt,
              valueTransformer: (value) => value?.toUtc().toIso8601String(),
              decoration: const InputDecoration(
                labelText: "Datum i vrijeme mjerenja",
                helperText: "Ako ostane prazno, koristi se trenutno vrijeme.",
              ),
            ),
        ],
      ),
    );
  }
}
