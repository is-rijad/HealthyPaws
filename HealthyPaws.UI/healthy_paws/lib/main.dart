import 'dart:async';

import 'package:healthy_paws/common/providers/ui_provider.dart';
import 'package:healthy_paws/providers/breeds_provider.dart';
import 'package:healthy_paws/providers/devices_provider.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/providers/treat_logs_provider.dart';
import 'package:healthy_paws/providers/treats_provider.dart';
import 'package:healthy_paws/providers/users_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/first_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/second_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/third_step_provider.dart';
import 'package:material_ui/material_ui.dart';
import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/dio_client.dart';
import 'package:healthy_paws/common/error_handler.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/common/providers/http_provider.dart';
import 'package:healthy_paws/common/providers/shared_prefs_provider.dart';
import 'package:healthy_paws/common/resources/access_token_payload.dart';
import 'package:healthy_paws/widgets/main_app.dart';
import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';

Future<void> main() async {
  runZonedGuarded(() async {
    WidgetsFlutterBinding.ensureInitialized();

    registerServices();

    AccessTokenPayload? payload;
    try {
      payload = await AuthProvider.getPayload();
    } catch (_) {}
    final authProvider = AuthProvider(payload);
    DioClient.init(authProvider);

    runApp(
      MultiProvider(providers: registerProviders(payload), child: MainApp()),
    );
  }, (ex, stack) => ErrorHandler.handleGlobalError(ex, stack));
}

void registerServices() {
  final getIt = GetIt.instance;

  getIt.registerFactory<HttpProvider>(() => HttpProvider());

  getIt.registerSingleton(SharedPrefsProvider());
  getIt.registerSingleton(UiProvider());
}

List<SingleChildWidget> registerProviders(AccessTokenPayload? payload) {
  return [
    ChangeNotifierProvider(create: (_) => AuthProvider(payload)),
    ChangeNotifierProvider(create: (_) => UsersProvider()),
    ChangeNotifierProvider(create: (_) => BreedsProvider()),
    ChangeNotifierProvider(create: (_) => DogsProvider()),
    ChangeNotifierProvider(create: (_) => DevicesProvider()),
    ChangeNotifierProvider(create: (_) => TreatsProvider()),
    ChangeNotifierProvider(create: (_) => TreatLogsProvider()),
    ChangeNotifierProvider(create: (_) => FirstStepProvider()),
    ChangeNotifierProvider(create: (_) => SecondStepProvider()),
    ChangeNotifierProvider(create: (_) => ThirdStepProvider()),
  ];
}
