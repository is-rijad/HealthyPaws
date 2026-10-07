import 'package:material_ui/material_ui.dart';

abstract class Constants {
  static final GlobalKey<ScaffoldMessengerState> messengerKey =
      GlobalKey<ScaffoldMessengerState>();
  static final GlobalKey<NavigatorState> navigatorKey =
      GlobalKey<NavigatorState>();

  static const Color secondaryTextColor = Color(0xFF284F73);
  static const Color primaryButtonTextColor = Color(0xFFD1E9FE);
  static const Color placeholderTextColor = Color(0xFF747474);
  static const String accessTokenKey = "access_token";
}
