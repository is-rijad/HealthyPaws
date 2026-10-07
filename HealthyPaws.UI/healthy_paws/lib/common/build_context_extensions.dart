import 'package:material_ui/material_ui.dart';
import 'package:intl/intl.dart';

extension BuildContextExtensions on BuildContext {
  double get screenWidth => MediaQuery.sizeOf(this).width;

  double get screenHeight => MediaQuery.sizeOf(this).height;

  Color get primaryColor => Theme.of(this).colorScheme.primary;

  Color get secondaryColor => Theme.of(this).colorScheme.secondary;

  Color get canvasColor => Theme.of(this).canvasColor;

  String getLocalizedDate(DateTime date) {
    final locale = Localizations.localeOf(this).toString();
    return DateFormat.yMMMd(locale).format(date.toLocal());
  }

  String getLocalizedTime(DateTime date) {
    final locale = Localizations.localeOf(this).toString();
    return DateFormat.Hm(locale).format(date.toLocal());
  }
}
