import 'package:dropdown_search/dropdown_search.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:healthy_paws/resources/search_objects/base_search_object/string_search_object.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class PaginatedDropdownField<
  T extends JsonResponse,
  TSearchObject extends StringSearchObject,
  TProvider extends BaseGetProvider<T, TSearchObject>
>
    extends StatelessWidget {
  const PaginatedDropdownField({
    super.key,
    required this.name,
    required this.searchObject,
    required this.itemAsString,
    this.label,
    this.hint,
    this.validator,
    this.initialValue,
    this.onChanged,
    this.enabled = true,
    this.showSearchBox = true,
    this.searchDelay = const Duration(milliseconds: 400),
    this.valueTransformer,
    this.itemBuilder,
  });

  final String name;
  final String Function(T item) itemAsString;
  bool compareFn(T a, T b) => a.id == b.id;
  final TSearchObject searchObject;

  final String? label;
  final String? hint;
  final String? Function(T?)? validator;
  final T? initialValue;
  final ValueChanged<T?>? onChanged;
  final bool enabled;
  final bool showSearchBox;
  final Duration searchDelay;

  final dynamic Function(T?)? valueTransformer;

  final Widget Function(
    BuildContext context,
    T item,
    bool isDisabled,
    bool isSelected,
  )?
  itemBuilder;

  @override
  Widget build(BuildContext context) {
    return FormBuilderField<T>(
      name: name,
      initialValue: initialValue,
      enabled: enabled,
      validator: validator,
      valueTransformer: valueTransformer,
      onChanged: onChanged,
      builder: (field) {
        return DropdownSearch<T>(
          enabled: enabled,
          selectedItem: field.value,
          compareFn: compareFn,
          itemAsString: itemAsString,
          items: (filter, loadProps) => _getItems(
            context,
            filter,
            loadProps?.skip ?? 0,
            loadProps?.take ?? searchObject.pageSize,
          ),
          onSelected: (value) => field.didChange(value),
          decoratorProps: DropDownDecoratorProps(
            decoration: InputDecoration(
              labelText: label,
              hintText: hint,
              errorText: field.errorText,
              border: const OutlineInputBorder(),
            ),
          ),
          popupProps: PopupProps.menu(
            showSearchBox: showSearchBox,
            searchDelay: searchDelay,
            fit: FlexFit.loose,
            constraints: const BoxConstraints(maxHeight: 320),
            itemBuilder: itemBuilder,
            infiniteScrollProps: InfiniteScrollProps(
              loadProps: LoadProps(skip: 0, take: searchObject.pageSize),
            ),
            searchFieldProps: const TextFieldProps(
              autofocus: true,
              decoration: InputDecoration(
                hintText: 'Pretraži...',
                prefixIcon: Icon(Icons.search),
                border: OutlineInputBorder(),
                contentPadding: EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
              ),
            ),
            loadingBuilder: (context, searchEntry) =>
                const Center(child: CircularProgressIndicator()),
            emptyBuilder: (context, searchEntry) => const Center(
              child: Padding(
                padding: EdgeInsets.all(16),
                child: Text('Nema rezultata'),
              ),
            ),
            errorBuilder: (context, searchEntry, exception) => Center(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Text('Greška pri učitavanju: $exception'),
              ),
            ),
          ),
        );
      },
    );
  }

  Future<List<T>> _getItems(
    BuildContext context,
    String search,
    int skip,
    int take,
  ) async {
    context.read<TProvider>().clearData(searchObject);
    searchObject.contains = search;
    searchObject.page = (skip / take).toInt() + 1;
    await context.read<TProvider>().fetchData(searchObject);
    if (!context.mounted) return List.empty();
    return context.read<TProvider>().searchResult.items;
  }
}
