import 'package:dio/dio.dart';
import 'package:material_ui/material_ui.dart';
import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/dio_client.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/common/providers/shared_prefs_provider.dart';
import 'package:healthy_paws/pages/login_page.dart';
import 'package:healthy_paws/resources/requests/auth/refresh_token_request.dart';

class AuthInterceptor extends Interceptor {
  final AuthProvider authProvider;
  Future<String>? _refreshFuture;

  AuthInterceptor(this.authProvider);

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      try {
        final accessToken = await (_refreshFuture ??= _refreshAccessToken());
        final request = err.requestOptions;
        request.headers['Authorization'] = 'Bearer $accessToken';

        final response = await DioClient.dio.fetch(request);
        return handler.resolve(response);
      } catch (_) {}
    }
    handler.reject(err);
  }

  Future<String> _refreshAccessToken() async {
    try {
      final refreshToken = await AuthProvider.getRefreshToken();
      final accessToken = await AuthProvider.getAccessToken();
      await authProvider.refresh(
        RefreshTokenRequest(
          refreshToken: refreshToken,
          accessToken: accessToken,
        ),
      );
      return await AuthProvider.getAccessToken();
    } catch (_) {
      await GetIt.I<SharedPrefsProvider>().deleteString(
        Constants.accessTokenKey,
      );

      await Constants.navigatorKey.currentState?.pushAndRemoveUntil(
        MaterialPageRoute(builder: (context) => LoginPage()),
        (route) => false,
      );
      rethrow;
    } finally {
      _refreshFuture = null;
    }
  }
}
