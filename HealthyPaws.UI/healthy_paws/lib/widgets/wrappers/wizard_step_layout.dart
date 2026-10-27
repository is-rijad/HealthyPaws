import 'package:material_ui/material_ui.dart';

class WizardStepLayout extends StatelessWidget {
  final String title;
  final String? subtitle;
  final List<Widget> children;

  const WizardStepLayout({
    super.key,
    required this.title,
    this.subtitle,
    required this.children,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(12),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: 12,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleMedium),
            if (subtitle != null) Text(subtitle!),
            ...children,
          ],
        ),
      ),
    );
  }
}
