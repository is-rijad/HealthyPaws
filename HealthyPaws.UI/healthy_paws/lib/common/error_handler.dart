import 'package:material_ui/material_ui.dart';

abstract class ErrorHandler {
  static void handleGlobalError(Object ex, StackTrace stack) {
    debugPrint(ex.toString());
    debugPrint(stack.toString());
  }
}
