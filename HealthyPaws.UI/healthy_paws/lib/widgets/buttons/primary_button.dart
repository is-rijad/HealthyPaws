import 'dart:async';

import 'package:material_ui/material_ui.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';

class PrimaryButton extends StatefulWidget {
  final String text;
  final FutureOr<void> Function()? onPressed;

  const PrimaryButton({super.key, this.onPressed, required this.text});

  @override
  State<StatefulWidget> createState() => _PrimaryButtonState();
}

class _PrimaryButtonState extends State<PrimaryButton> {
  bool _isLoading = false;
  @override
  Widget build(BuildContext context) {
    return Align(
      widthFactor: 1,
      heightFactor: 1,
      child: GestureDetector(
        onTap: (!_isLoading && widget.onPressed != null)
            ? () async {
                setState(() {
                  _isLoading = true;
                });
                try {
                  await widget.onPressed!.call();
                } catch (e) {
                  rethrow;
                } finally {
                  if (mounted) {
                    setState(() {
                      _isLoading = false;
                    });
                  }
                }
              }
            : null,
        child: Container(
          decoration: BoxDecoration(
            color: context.primaryColor,
            borderRadius: BorderRadius.circular(30),
            border: Border.all(color: const Color(0xff559bd6), width: 1),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.15),
                blurRadius: 10,
                offset: const Offset(0, 6),
              ),
              const BoxShadow(
                color: Color(0xff2d6694),
                blurRadius: 0,
                offset: Offset(1.5, 2),
                spreadRadius: -0.5,
              ),
            ],
          ),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 12),
            child: Stack(
              alignment: Alignment.center,
              children: [
                Opacity(opacity: _isLoading ? 0 : 1, child: Text(widget.text)),
                if (_isLoading)
                  SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: context.secondaryColor,
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
