import 'package:material_ui/material_ui.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/constants.dart';

class InputDecorationWithIcon extends InputDecoration {
  final IconData? iconData;
  final String? iconHint;
  InputDecorationWithIcon({
    this.iconData,
    this.iconHint,
    super.border,
    super.hintText,
  }) : super(
         hintStyle: TextStyle(
           color: Constants.placeholderTextColor,
           fontSize: 16,
           fontWeight: FontWeight(400),
         ),
         prefixIcon: Builder(
           builder: (context) {
             if (iconData != null) {
               return Tooltip(
                 message: iconHint ?? "",
                 child: Icon(iconData, color: context.primaryColor),
               );
             } else {
               return SizedBox.shrink();
             }
           },
         ),
       );
}
