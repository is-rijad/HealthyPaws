import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/components/dog_weight/dog_weight_component.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/providers/dog_weights_provider.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_insert_request.dart';
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
    widget.device.lastWeightRecord = added;
    context.read<DevicesProvider>().updateLocally(widget.device);
    Constants.messengerKey.currentState?.showSnackBar(
      SuccessSnackbar(message: "Uspješno ste dodali zapis težine psa!"),
    );
    Constants.navigatorKey.currentState?.pop(true);
  }
}
