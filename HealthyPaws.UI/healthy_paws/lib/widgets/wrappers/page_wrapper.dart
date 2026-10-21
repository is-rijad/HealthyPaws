import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/build_context_extensions.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/enums/profile_dropdown_action.dart';
import 'package:healthy_paws/common/providers/ui_provider.dart';
import 'package:healthy_paws/common/resources/access_token_payload.dart';
import 'package:healthy_paws/pages/login_page.dart';
import 'package:healthy_paws/pages/my_profile_page.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/widgets/profile_dropdown/profile_dropdown_item.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class PageWrapper extends StatefulWidget {
  final Widget body;
  final String? appBarTitle;

  const PageWrapper({super.key, required this.body, this.appBarTitle});

  @override
  State<StatefulWidget> createState() => _PageWrapperState();
}

class _PageWrapperState extends State<PageWrapper> {
  final UiProvider _uiProvider = GetIt.I<UiProvider>();
  final _avatarKey = GlobalKey();
  final GlobalKey<ScaffoldState> _scaffoldKey = GlobalKey<ScaffoldState>();
  bool _isShowingDropdown = false;

  late AccessTokenPayload _accessTokenPayload;

  @override
  void didChangeDependencies() {
    _accessTokenPayload = context.watch<AuthProvider>().accessTokenPayload;
    super.didChangeDependencies();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Scaffold(
          appBar: _buildAppBar(context),
          body: Padding(
            padding: const EdgeInsets.all(12.0),
            child: widget.body,
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
      actions: [_buildProfileIcon(context)],
      actionsPadding: EdgeInsets.all(8),
      centerTitle: true,
    );
  }

  Widget _buildProfileIcon(BuildContext context) {
    return Padding(
      padding: EdgeInsets.all(2.5).copyWith(right: 10),
      child: GestureDetector(
        key: _avatarKey,
        onTap: () => _showDropdown(context),
        child: CircleAvatar(
          radius: 30,
          child: Text(
            "${_accessTokenPayload.firstName[0]}${_accessTokenPayload.lastName[0]}",
          ),
        ),
      ),
    );
  }

  Future<void> _showDropdown(BuildContext context) async {
    if (_isShowingDropdown) return;
    _isShowingDropdown = true;
    final RenderBox renderBox =
        _avatarKey.currentContext!.findRenderObject() as RenderBox;
    final Offset offset = renderBox.localToGlobal(Offset.zero);
    final Size size = renderBox.size;

    _uiProvider.profileDropdownAction = await showMenu(
      context: context,
      color: context.secondaryColor,
      position: RelativeRect.fromLTRB(
        offset.dx,
        offset.dy + size.height + 7,
        offset.dx + size.width,
        offset.dy + size.height,
      ),
      popUpAnimationStyle: AnimationStyle(
        curve: Curves.easeInOut,
        duration: Duration(milliseconds: 300),
      ),
      items: [
        ProfileDropdownItem(
          text: "Moj profil",
          value: ProfileDropdownAction.myProfile,
        ),
        ProfileDropdownItem(
          text: "Odjava",
          value: ProfileDropdownAction.logout,
        ),
      ],
    );
    _isShowingDropdown = false;

    switch (_uiProvider.profileDropdownAction) {
      case ProfileDropdownAction.myProfile:
        setState(() {
          _uiProvider.profileDropdownAction = ProfileDropdownAction.myProfile;
        });
        await Constants.navigatorKey.currentState?.push(
          MaterialPageRoute(builder: (context) => MyProfilePage()),
        );
      case ProfileDropdownAction.logout:
        _uiProvider.startLoading();
        setState(() {
          _uiProvider.profileDropdownAction = null;
        });
        if (!context.mounted) return;
        await context.read<AuthProvider>().logout();
        if (!context.mounted) return;
        _uiProvider.stopLoading();
        await Constants.navigatorKey.currentState?.pushAndRemoveUntil(
          MaterialPageRoute(builder: (context) => LoginPage()),
          (route) => false,
        );
        break;
      default:
    }

    if (mounted) {
      setState(() {
        _uiProvider.profileDropdownAction = null;
      });
    }
  }
}
