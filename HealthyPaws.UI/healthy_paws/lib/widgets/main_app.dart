import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/providers/ui_provider.dart';
import 'package:material_ui/material_ui.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:flutter_localizations/flutter_localizations.dart'
    hide GlobalMaterialLocalizations;
import 'package:healthy_paws/pages/main_shell.dart';

class MainApp extends StatelessWidget {
  MainApp({super.key});
  final UiProvider _uiProvider = GetIt.I<UiProvider>();

  @override
  Widget build(BuildContext context) {
    return Stack(
      alignment: AlignmentGeometry.topCenter,
      fit: StackFit.expand,
      children: [
        //RepaintBoundary(child: _buildBackgroundImage(context)),
        _buildApp(context),
        _buildLoadingOverlay(context),
      ],
    );
  }

  MaterialApp _buildApp(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Moj Prijevoz',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: context.primaryColor,
          primary: context.primaryColor,
          secondary: context.secondaryColor,
        ),
        useMaterial3: true,
        pageTransitionsTheme: PageTransitionsTheme(
          builders: {
            TargetPlatform.android: FadeUpwardsPageTransitionsBuilder(),
          },
        ),
        appBarTheme: AppBarTheme(
          elevation: 4,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.all(Radius.circular(150)),
          ),
          toolbarHeight: 60,
          backgroundColor: context.primaryColor,
          foregroundColor: context.secondaryColor,
          iconTheme: IconThemeData(color: context.secondaryColor),
        ),
        scaffoldBackgroundColor: Colors.white,
        iconTheme: IconThemeData(color: context.primaryColor),
        fontFamily: "Inter",
      ),
      home: const MainShell(),
      localizationsDelegates: [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: [Locale('hr'), Locale('bs'), Locale('sr')],
      scaffoldMessengerKey: Constants.messengerKey,
      navigatorKey: Constants.navigatorKey,
    );
  }

  static Widget buildLoadingContainer(BuildContext context) {
    return Container(
      color: context.secondaryColor.withAlpha(125),
      child: Center(
        child: CircularProgressIndicator(color: context.secondaryColor),
      ),
    );
  }

  Widget _buildBackgroundImage(BuildContext context) {
    return SizedBox.shrink();
    return Container(
      width: context.screenWidth,
      decoration: BoxDecoration(
        color: const Color(0xFFACD8FF),
        image: DecorationImage(
          image: AssetImage("images/background.png"),
          fit: BoxFit.cover,
          filterQuality: FilterQuality.low,
        ),
      ),
    );
  }

  ValueListenableBuilder<bool> _buildLoadingOverlay(BuildContext context) {
    return ValueListenableBuilder<bool>(
      valueListenable: _uiProvider.isLoading,
      builder: (context, loading, _) {
        if (!loading) return const SizedBox.shrink();
        return _buildLoadingContainer(context);
      },
    );
  }

  Widget _buildLoadingContainer(BuildContext context) {
    return Container(
      color: context.secondaryColor.withAlpha(125),
      child: Center(
        child: CircularProgressIndicator(color: context.primaryColor),
      ),
    );
  }
}
