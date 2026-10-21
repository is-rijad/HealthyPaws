import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/resources/requests/device/device_update_request.dart';
import 'package:healthy_paws/resources/responses/device/device_response.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/search_objects/dog/dog_search_object.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/inputs/form_builder_dropdown.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DeviceForm extends StatefulWidget {
  final DeviceResponse device;
  const DeviceForm({super.key, required this.device});

  @override
  State<StatefulWidget> createState() => _DeviceFormState();
}

class _DeviceFormState extends State<DeviceForm> {
  final _formKey = GlobalKey<FormBuilderState>();

  @override
  Widget build(BuildContext context) {
    return PageWrapper(
      appBarTitle: "Uredi uređaj",
      body: Column(
        spacing: 12,
        children: [
          FormBuilder(
            key: _formKey,
            child:
                PaginatedDropdownField<
                  DogResponse,
                  DogSearchObject,
                  DogsProvider
                >(
                  label: "Pas",
                  name: "dogId",
                  initialValue: widget.device.dog,
                  searchObject: DogSearchObject(page: 1, pageSize: 10),
                  itemAsString: (item) => item.name,
                  validator: FormBuilderValidators.required(
                    errorText: "Pas je obavezan!",
                  ),
                  valueTransformer: (i) => i?.id,
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
    );
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.saveAndValidate() ?? false)) return;
    final request = DeviceUpdateRequest.fromJson(_formKey.currentState!.value);
    await context.read<DevicesProvider>().update(widget.device.id, request);
    Constants.messengerKey.currentState?.showSnackBar(
      SuccessSnackbar(message: "Uspješno ste uredili uređaj!"),
    );
    Constants.navigatorKey.currentState?.pop(true);
  }
}
