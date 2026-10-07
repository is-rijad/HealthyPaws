import 'package:material_ui/material_ui.dart';

class ErrorSnackbar extends SnackBar {
  final String message;
  ErrorSnackbar({super.key, required this.message})
    : super(
        content: Text(message),
        duration: Duration(seconds: 5),
        dismissDirection: DismissDirection.horizontal,
      );
}
