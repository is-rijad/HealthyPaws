import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/utils/food_energy.dart';
import 'package:healthy_paws/components/dog_form/dog_form_component.dart';
import 'package:healthy_paws/components/food_input/food_input.dart';
import 'package:healthy_paws/providers/dog_weights_provider.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_insert_request.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_update_request.dart';
import 'package:healthy_paws/resources/responses/dog_weight/dog_weight_response.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DogForm extends StatefulWidget {
  final DogResponse dog;

  /// Najnovije mjerenje; njegova hrana se uređuje u tabu "Hrana".
  final DogWeightResponse? lastWeight;
  const DogForm({super.key, required this.dog, this.lastWeight});

  @override
  State<StatefulWidget> createState() => _DogFormState();
}

class _DogFormState extends State<DogForm> with SingleTickerProviderStateMixin {
  final _formKey = GlobalKey<FormBuilderState>();
  late final TabController _tabController;
  double? _foodCalorie;
  FoodCalorieSource? _foodSource;
  bool _foodChanged = false;
  late final Map<String, dynamic> _initialValue;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this)
      ..addListener(() => setState(() {}));
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
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return PageWrapper(
      appBarTitle: "Uredi psa",
      body: Column(
        spacing: 12,
        children: [
          TabBar(
            controller: _tabController,
            tabs: const [
              Tab(text: "Podaci"),
              Tab(text: "Hrana"),
            ],
          ),
          Expanded(
            child: SingleChildScrollView(
              // IndexedStack čuva stanje obje forme dok se mijenja tab.
              child: IndexedStack(
                index: _tabController.index,
                children: [
                  DogFormComponent(
                    formKey: _formKey,
                    initialValue: _initialValue,
                  ),
                  _buildFoodTab(),
                ],
              ),
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

  Widget _buildFoodTab() {
    final last = widget.lastWeight;
    if (last == null) {
      return const Padding(
        padding: EdgeInsets.all(12),
        child: Text(
          "Pas još nema mjerenja. Hranu možete urediti nakon prvog vaganja.",
        ),
      );
    }
    return Column(
      spacing: 12,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          "Trenutna hrana: ${last.calorie.toStringAsFixed(0)} kcal/100g"
          "${last.calorieSource == 1 ? " (procijenjeno)" : ""}",
        ),
        FoodInput(
          initialKcal: last.calorie,
          onChanged: (calorie, source) => setState(() {
            _foodCalorie = calorie;
            _foodSource = source;
            _foodChanged = true;
          }),
        ),
      ],
    );
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.saveAndValidate() ?? false)) {
      _tabController.animateTo(0);
      return;
    }
    final last = widget.lastWeight;
    final updateFood = _foodChanged && last != null;
    if (updateFood && (_foodCalorie ?? 0) <= 0) {
      _tabController.animateTo(1);
      Constants.messengerKey.currentState?.showSnackBar(
        const SnackBar(content: Text("Unesite ispravan iznos kalorija hrane!")),
      );
      return;
    }
    final request = DogInsertRequest.fromJson(_formKey.currentState!.value);
    await context.read<DogsProvider>().update(widget.dog.id, request);
    if (updateFood && mounted) {
      await context.read<DogWeightsProvider>().update(
        last.id,
        DogWeightUpdateRequest(
          calorie: _foodCalorie,
          calorieSource: (_foodSource ?? FoodCalorieSource.label).apiValue,
        ),
      );
    }
    Constants.messengerKey.currentState?.showSnackBar(
      SuccessSnackbar(message: "Uspješno ste uredili psa!"),
    );
    Constants.navigatorKey.currentState?.pop(true);
  }
}
