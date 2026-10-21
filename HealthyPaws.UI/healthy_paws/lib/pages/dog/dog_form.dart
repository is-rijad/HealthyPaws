import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/components/dog_form/dog_form_component.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_insert_request.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DogForm extends StatefulWidget {
  final DogResponse dog;
  const DogForm({super.key, required this.dog});

  @override
  State<StatefulWidget> createState() => _DogFormState();
}

class _DogFormState extends State<DogForm> {
  final _formKey = GlobalKey<FormBuilderState>();
  late final Map<String, dynamic> _initialValue;

  @override
  void initState() {
    super.initState();
    final dog = widget.dog;
    _initialValue = {
      "breedId": dog.breed,
      "name": dog.name,
      "dateOfBirth": dog.dateOfBirth.toLocal(),
      "gender": dog.gender.index,
      "activityStatus": dog.activityStatus,
      "isNeutered": dog.isNeutered,
    };
  }

  @override
  Widget build(BuildContext context) {
    return PageWrapper(
      appBarTitle: "Uredi psa",
      body: SingleChildScrollView(
        child: Column(
          spacing: 12,
          children: [
            DogFormComponent(formKey: _formKey, initialValue: _initialValue),
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
    final request = DogInsertRequest.fromJson(_formKey.currentState!.value);
    await context.read<DogsProvider>().update(widget.dog.id, request);
    Constants.messengerKey.currentState?.showSnackBar(
      SuccessSnackbar(message: "Uspješno ste uredili psa!"),
    );
    Constants.navigatorKey.currentState?.pop(true);
  }
}
