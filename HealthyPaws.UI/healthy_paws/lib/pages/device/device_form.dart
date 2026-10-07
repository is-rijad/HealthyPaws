import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/pages/dog/dog_form.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/search_objects/dog/dog_search_object.dart';
import 'package:healthy_paws/widgets/inputs/form_builder_dropdown.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DeviceForm extends StatefulWidget {
  final int? id;
  const DeviceForm({super.key, this.id});

  @override
  State<StatefulWidget> createState() => _DeviceFormState();
}

class _DeviceFormState extends State<DeviceForm> {
  final _formKey = GlobalKey<FormBuilderState>();
  var _initialValue = <String, dynamic>{};
  final _dogsSearchObject = DogSearchObject(page: 1, pageSize: 5);

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    return PageWrapper(
      appBarTitle: widget.id != null ? "Uredi uređaj" : "Dodaj uređaj",
      body: FormBuilder(
        initialValue: _initialValue,
        child: Column(
          children: [
            Row(
              children: [
                Expanded(
                  child:
                      PaginatedDropdownField<
                        DogResponse,
                        DogSearchObject,
                        DogsProvider
                      >(
                        name: "dogId",
                        searchObject: DogSearchObject(page: 1, pageSize: 10),
                        itemAsString: (item) => item.name,
                        compareFn: (a, b) => a.id == b.id,
                      ),
                ),
                IconButton(
                  onPressed: () async =>
                      await Constants.navigatorKey.currentState?.push(
                        MaterialPageRoute(builder: (builder) => DogForm()),
                      ),
                  icon: Icon(Icons.add),
                ),
              ],
            ),
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
    context.read<DogsProvider>().clearData(_dogsSearchObject);
    await context.read<DogsProvider>().fetchData(_dogsSearchObject);

    if (widget.id != null && mounted) {
      _initialValue = (await context.read<DevicesProvider>().getById(
        widget.id!,
      )).toJson();
    }
    return true;
  }
}
