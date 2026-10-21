import 'package:material_ui/material_ui.dart';

class SuccessSnackbar extends SnackBar {
  final String message;
  SuccessSnackbar({super.key, required this.message})
    : super(
        content: Text(message),
        duration: Duration(seconds: 5),
        dismissDirection: DismissDirection.horizontal,
      );
}
