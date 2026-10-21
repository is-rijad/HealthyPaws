import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/wrappers/load_until_ready_wrapper.dart';
import 'package:healthy_paws/pages/device/device_form.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/providers/users_provider.dart';
import 'package:healthy_paws/resources/requests/user/user_update_request.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/inputs/password_form_text_field.dart';
import 'package:healthy_paws/widgets/wrappers/page_wrapper.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class MyProfilePage extends StatefulWidget {
  const MyProfilePage({super.key});

  @override
  State<StatefulWidget> createState() => _MyProfilePageState();
}

class _MyProfilePageState extends State<MyProfilePage> {
  var _initialValue = <String, dynamic>{};
  final _formKey = GlobalKey<FormBuilderState>();
  late final int _userId;
  bool _changePassword = false;

  final oldPasswordController = TextEditingController();
  final passwordController = TextEditingController();
  final passwordAgainController = TextEditingController();

  @override
  void dispose() {
    oldPasswordController.dispose();
    passwordController.dispose();
    passwordAgainController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return LoadUntilReadyWrapper(buildFunction: _build, futureFunction: _init);
  }

  Widget _build(BuildContext context) {
    return PageWrapper(
      appBarTitle: "Moj profil",
      body: FormBuilder(
        key: _formKey,
        initialValue: _initialValue,
        child: Column(
          spacing: 12,
          children: [
            FormBuilderTextField(
              name: "firstName",
              decoration: InputDecoration(labelText: "Ime"),
              validator: FormBuilderValidators.compose([
                FormBuilderValidators.required(errorText: "Ime je obavezno!"),
                FormBuilderValidators.minLength(
                  1,
                  errorText: "Ime nije validno!",
                ),
                FormBuilderValidators.maxLength(
                  32,
                  errorText: "Ime nije validno!",
                ),
              ]),
            ),
            FormBuilderTextField(
              name: "lastName",
              decoration: InputDecoration(labelText: "Prezime"),
              validator: FormBuilderValidators.compose([
                FormBuilderValidators.required(
                  errorText: "Prezime je obavezno!",
                ),
                FormBuilderValidators.minLength(
                  1,
                  errorText: "Prezime nije validno!",
                ),
                FormBuilderValidators.maxLength(
                  64,
                  errorText: "Prezime nije validno!",
                ),
              ]),
            ),
            FormBuilderTextField(
              name: "email",
              decoration: InputDecoration(labelText: "Email"),
              validator: FormBuilderValidators.compose([
                FormBuilderValidators.required(errorText: "Email je obavezan!"),
                FormBuilderValidators.email(errorText: "Email nije validan!"),
              ]),
            ),
            FormBuilderCheckbox(
              initialValue: false,
              onChanged: (value) => setState(() {
                _changePassword = value ?? false;
              }),
              name: "changePassword",
              title: const Text("Izmjena lozinke?"),
            ),
            if (_changePassword)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Column(
                  children: [
                    PasswordFormTextField(
                      controller: oldPasswordController,
                      isRequired: true,
                      name: "oldPassword",
                      decoration: InputDecoration(labelText: "Stara lozinka"),
                    ),
                    PasswordFormTextField(
                      controller: passwordController,
                      isDetailedMessage: true,
                      isRequired: true,
                      name: "password",
                      decoration: InputDecoration(labelText: "Lozinka"),
                    ),
                    PasswordFormTextField(
                      controller: passwordAgainController,
                      isRequired: true,
                      name: "passwordAgain",
                      decoration: InputDecoration(labelText: "Potvrda lozinke"),
                    ),
                  ],
                ),
              ),

            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                ElevatedButton(
                  onPressed: () => Constants.navigatorKey.currentState?.pop(),
                  child: const Text("Otkaži"),
                ),
                PrimaryButton(
                  text: "Spremi",
                  onPressed: () async => await _save(),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Future<bool> _init() async {
    _userId = await context.read<AuthProvider>().getUserId();
    if (!mounted) return false;
    _initialValue = (await context.read<UsersProvider>().getById(_userId))
        .toJson();

    return true;
  }

  Future<void> _save() async {
    if (_formKey.currentState?.saveAndValidate() ?? false) {
      final request = UserUpdateRequest.fromJson(_formKey.currentState!.value);
      await context.read<UsersProvider>().update(_userId, request);
      if (!mounted) return;
      oldPasswordController.clear();
      passwordController.clear();
      passwordAgainController.clear();
      Constants.messengerKey.currentState?.showSnackBar(
        SuccessSnackbar(message: "Promjene su uspješno spremljene!"),
      );
    }
  }
}
