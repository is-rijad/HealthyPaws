import 'package:flutter/services.dart';
import 'package:healthy_paws/pages/device/device_page.dart';
import 'package:healthy_paws/pages/home_page.dart';
import 'package:healthy_paws/pages/my_profile_page.dart';
import 'package:healthy_paws/pages/treat/treat_tracker_page.dart';
import 'package:material_ui/material_ui.dart';

/// Glavni ekran s donjom navigacijom. Tab se gradi iznova pri svakom
/// prebacivanju, pa uvijek prikazuje svježe podatke.
class MainShell extends StatefulWidget {
  const MainShell({super.key});

  static final ValueNotifier<int> tabIndex = ValueNotifier(0);

  static void goToTab(int index) => tabIndex.value = index;

  @override
  State<StatefulWidget> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  static const _destinations = [
    NavigationDestination(icon: Icon(Icons.home_outlined), label: "Početna"),
    NavigationDestination(icon: Icon(Icons.devices_other), label: "Uređaji"),
    NavigationDestination(
      icon: Icon(Icons.cookie_outlined),
      label: "Poslastice",
    ),
    NavigationDestination(icon: Icon(Icons.person_outline), label: "Profil"),
  ];

  @override
  void initState() {
    super.initState();
    MainShell.tabIndex.value = 0;
  }

  Widget _tab(int index) => switch (index) {
    0 => const HomePage(key: ValueKey("tab_home")),
    1 => const DevicePage(key: ValueKey("tab_devices")),
    2 => const TreatTrackerPage(key: ValueKey("tab_treats")),
    _ => const MyProfilePage(key: ValueKey("tab_profile")),
  };

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<int>(
      valueListenable: MainShell.tabIndex,
      builder: (context, index, _) => PopScope(
        canPop: false,
        onPopInvokedWithResult: (didPop, _) {
          if (didPop) return;
          if (index != 0) {
            MainShell.goToTab(0);
          } else {
            SystemNavigator.pop();
          }
        },
        child: Scaffold(
          body: _tab(index),
          bottomNavigationBar: NavigationBar(
            selectedIndex: index,
            onDestinationSelected: MainShell.goToTab,
            destinations: _destinations,
          ),
        ),
      ),
    );
  }
}
