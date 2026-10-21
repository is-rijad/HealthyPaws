import 'package:healthy_paws/common/constants.dart';
import 'package:material_ui/material_ui.dart';

class PageWrapper extends StatefulWidget {
  final Widget body;
  final String? appBarTitle;
  final Widget? floatingActionButton;

  /// Glavne stranice (tabovi donje navigacije) nemaju gornji bar; naslov se
  /// tada prikazuje kao zaglavlje iznad sadržaja.
  final bool showAppBar;

  const PageWrapper({
    super.key,
    required this.body,
    this.appBarTitle,
    this.floatingActionButton,
    this.showAppBar = true,
  });

  @override
  State<StatefulWidget> createState() => _PageWrapperState();
}

class _PageWrapperState extends State<PageWrapper> {
  final GlobalKey<ScaffoldState> _scaffoldKey = GlobalKey<ScaffoldState>();

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Scaffold(
          appBar: widget.showAppBar ? _buildAppBar(context) : null,
          floatingActionButton: widget.floatingActionButton,
          body: Padding(
            padding: const EdgeInsets.all(12.0),
            child: widget.showAppBar || widget.appBarTitle == null
                ? widget.body
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    spacing: 12,
                    children: [
                      Text(
                        widget.appBarTitle!,
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                      Expanded(child: widget.body),
                    ],
                  ),
          ),
          key: _scaffoldKey,
        ),
      ),
    );
  }

  PreferredSizeWidget _buildAppBar(BuildContext context) {
    return AppBar(
      leading: Constants.navigatorKey.currentState?.canPop() ?? false
          ? IconButton(
              icon: Icon(Icons.arrow_back_ios_new_rounded),
              onPressed: () => Constants.navigatorKey.currentState?.pop(),
            )
          : null,
      automaticallyImplyLeading: false,
      title: Text(widget.appBarTitle ?? ""),
      centerTitle: true,
    );
  }
}
