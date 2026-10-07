import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/enums/profile_dropdown_action.dart';
import 'package:healthy_paws/common/providers/ui_provider.dart';
import 'package:material_ui/material_ui.dart';

class ProfileDropdownItem extends PopupMenuItem<ProfileDropdownAction> {
  @override
  final ProfileDropdownAction value;
  final String text;

  ProfileDropdownItem({super.key, required this.value, required this.text})
    : super(
        value: value,
        enabled: GetIt.I<UiProvider>().profileDropdownAction != value,
        child: Builder(
          builder: (context) {
            final uiProvider = GetIt.I<UiProvider>();
            return Text(
              text,
              style: TextStyle(
                color: uiProvider.profileDropdownAction == value
                    ? context.primaryColor
                    : null,
              ),
            );
          },
        ),
      );
}
