import 'dart:async';

import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/pages/main_shell.dart';
import 'package:healthy_paws/providers/users_provider.dart';
import 'package:healthy_paws/resources/requests/auth/login_request.dart';
import 'package:healthy_paws/resources/requests/user/user_insert_request.dart';
import 'package:healthy_paws/widgets/inputs/password_form_text_field.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:provider/provider.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<StatefulWidget> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _formKey = GlobalKey<FormBuilderState>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Registracija"), centerTitle: true),
      body: FormBuilder(
        key: _formKey,
        child: Padding(
          padding: const EdgeInsets.all(12.0),
          child: SingleChildScrollView(
            child: Column(
              spacing: 12,
              children: [
                const Text("Registracija"),
                FormBuilderTextField(
                  name: "email",
                  decoration: InputDecoration(labelText: "Email"),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                      errorText: "Email je obavezan!",
                    ),
                    FormBuilderValidators.email(
                      errorText: "Email nije validan!",
                    ),
                  ]),
                ),
                PasswordFormTextField(
                  isDetailedMessage: true,
                  isRequired: true,
                  name: "password",
                  decoration: InputDecoration(labelText: "Lozinka"),
                ),
                PasswordFormTextField(
                  isRequired: true,
                  name: "passwordAgain",
                  decoration: InputDecoration(labelText: "Potvrda lozinke"),
                ),
                PrimaryButton(
                  text: "Registruj se",
                  onPressed: () async => await _save(),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _save() async {
    if (_formKey.currentState?.saveAndValidate() ?? false) {
      final request = UserInsertRequest.fromJson(_formKey.currentState!.value);
      await context.read<UsersProvider>().insert(request);
      if (!mounted) return;
      await context.read<AuthProvider>().login(
        LoginRequest(email: request.email, password: request.password),
      );
      if (!mounted) return;
      await Constants.navigatorKey.currentState?.pushAndRemoveUntil(
        MaterialPageRoute(builder: (builder) => MainShell()),
        (_) => false,
      );
    }
  }
}
