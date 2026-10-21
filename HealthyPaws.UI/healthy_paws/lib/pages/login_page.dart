import 'dart:async';

import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/pages/main_shell.dart';
import 'package:healthy_paws/pages/register_page.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/resources/requests/auth/login_request.dart';
import 'package:healthy_paws/widgets/buttons/primary_button.dart';
import 'package:healthy_paws/widgets/inputs/password_form_text_field.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<StatefulWidget> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormBuilderState>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(12.0),
        child: FormBuilder(
          key: _formKey,
          child: Column(
            spacing: 12,
            children: [
              const Text("Login"),
              FormBuilderTextField(
                name: "email",
                decoration: InputDecoration(labelText: "Email"),
                validator: FormBuilderValidators.compose([
                  FormBuilderValidators.required(
                    errorText: "Email je obavezan!",
                  ),
                  FormBuilderValidators.email(errorText: "Email nije validan!"),
                ]),
              ),
              PasswordFormTextField(
                isRequired: true,
                name: "password",
                decoration: InputDecoration(labelText: "Lozinka"),
              ),
              PrimaryButton(
                text: "Prijavi se",
                onPressed: () async => await _save(),
              ),
              PrimaryButton(
                text: "Registracija",
                onPressed: () async =>
                    await Constants.navigatorKey.currentState?.push(
                      MaterialPageRoute(builder: (builder) => RegisterPage()),
                    ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _save() async {
    if (_formKey.currentState?.saveAndValidate() ?? false) {
      final request = LoginRequest.fromJson(_formKey.currentState!.value);
      await context.read<AuthProvider>().login(request);
      if (!mounted) return;
      await Constants.navigatorKey.currentState?.pushAndRemoveUntil(
        MaterialPageRoute(builder: (builder) => MainShell()),
        (_) => false,
      );
    }
  }
}
