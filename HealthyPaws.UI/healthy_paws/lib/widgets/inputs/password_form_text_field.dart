import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:material_ui/material_ui.dart';

class PasswordFormTextField extends StatefulWidget {
  final bool isRequired;
  final String name;
  final InputDecoration? decoration;
  final bool isDetailedMessage;
  final TextEditingController? controller;

  const PasswordFormTextField({
    super.key,
    required this.isRequired,
    required this.name,
    this.decoration,
    this.isDetailedMessage = false,
    this.controller,
  });

  @override
  State<StatefulWidget> createState() => _PasswordFormTextFieldState();
}

class _PasswordFormTextFieldState extends State<PasswordFormTextField> {
  bool isObscure = true;
  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: FormBuilderTextField(
            name: widget.name,
            obscureText: isObscure,
            controller: widget.controller,
            decoration: widget.decoration ?? const InputDecoration(),
            validator: FormBuilderValidators.conditional(
              (_) => widget.isRequired,
              FormBuilderValidators.compose([
                FormBuilderValidators.required(
                  errorText: "Ovo polje je obavezno!",
                ),
                FormBuilderValidators.password(
                  errorText: widget.isDetailedMessage
                      ? "Lozinka mora sadržavati minimalno 8 znakova, veliko i malo slovo, specijalni znak i jedan broj!"
                      : "Unos nije validan!",
                  minLength: 8,
                  minNumberCount: 1,
                  minUppercaseCount: 1,
                  minLowercaseCount: 1,
                  minSpecialCharCount: 1,
                ),
              ]),
            ),
          ),
        ),
        IconButton(
          onPressed: () => setState(() {
            isObscure = !isObscure;
          }),
          icon: Icon(isObscure ? Icons.visibility : Icons.visibility_off),
        ),
      ],
    );
  }
}
