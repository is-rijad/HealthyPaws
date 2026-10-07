import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/resources/requests/dog/dog_body_condition_score.dart';
import 'package:material_ui/material_ui.dart';

class DogBcsFormField extends FormBuilderField<int> {
  DogBcsFormField({
    super.key,
    required super.name,
    super.onSaved,
    super.initialValue,
  }) : super(
         builder: (field) {
           return Column(
             crossAxisAlignment: CrossAxisAlignment.start,
             children: [
               _DogBcsField(valueBind: field.value, onChanged: field.didChange),
               if (field.errorText != null)
                 Padding(
                   padding: const EdgeInsets.only(top: 4.0),
                   child: Text(
                     field.errorText!,
                     style: TextStyle(color: Colors.red[700], fontSize: 12),
                   ),
                 ),
             ],
           );
         },
         validator: (value) {
           if (value == null) {
             return "Izgled psa je obavezan!";
           }
           return null;
         },
       );
}

class _DogBcsField extends StatefulWidget {
  final int? valueBind;
  final ValueChanged<int> onChanged;
  const _DogBcsField({required this.valueBind, required this.onChanged});

  @override
  State<StatefulWidget> createState() => _DogBcsFieldState();
}

class _DogBcsFieldState extends State<_DogBcsField> {
  late int? valueBind;
  late final PageController _pageController;
  int _currentPage = 0;
  @override
  void initState() {
    super.initState();
    valueBind = widget.valueBind;
    var pageIndex = 0;
    if (valueBind != null) {
      pageIndex = dogBodyConditionScoreMap.entries.toList().indexWhere(
        (i) => i.key == valueBind,
      );
    }
    _pageController = PageController(
      viewportFraction: 0.7,
      initialPage: pageIndex,
    );
    _onPageChanged(pageIndex);
  }

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  void _onPageChanged(int index) {
    setState(() {
      _currentPage = index;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        SizedBox(
          height: 295,
          child: PageView(
            controller: _pageController,
            onPageChanged: _onPageChanged,
            children: dogBodyConditionScoreMap.entries
                .map((entry) => _buildCard(context, entry.value, entry.key))
                .toList(),
          ),
        ),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: List.generate(dogBodyConditionScoreMap.length, (i) {
            final isActive = i == _currentPage;
            return AnimatedContainer(
              duration: const Duration(milliseconds: 300),
              margin: const EdgeInsets.symmetric(horizontal: 4),
              width: isActive ? 12 : 8,
              height: 8,
              decoration: BoxDecoration(
                color: isActive ? context.primaryColor : Colors.grey,
                borderRadius: BorderRadius.circular(4),
              ),
            );
          }),
        ),
      ],
    );
  }

  Widget _buildCard(BuildContext context, String imagePath, int index) {
    return GestureDetector(
      onTap: () => setState(() {
        valueBind = index;
        widget.onChanged(index);
      }),
      child: Column(
        children: [
          Card(
            color: valueBind == index ? context.primaryColor : null,
            child: Image.asset(
              imagePath,
              width: 300,
              height: 280,
              fit: BoxFit.contain,
            ),
          ),
        ],
      ),
    );
  }
}
