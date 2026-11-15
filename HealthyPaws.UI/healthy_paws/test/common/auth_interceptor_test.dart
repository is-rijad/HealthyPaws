import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/dio_client.dart';
import 'package:healthy_paws/common/providers/http_provider.dart';
import 'package:healthy_paws/common/providers/shared_prefs_provider.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../helpers/fakes.dart';

/// Pravi HttpProvider + pravi Dio s AuthInterceptor-om; mrežu glumi ScriptedAdapter.
void main() {
  late ScriptedAdapter adapter;
  late String validToken;
  late int refreshCalls;
  late ({int status, Object? body}) Function(RequestOptions) onRefresh;

  setUp(() async {
    await GetIt.I.reset();
    validToken = fakeJwt(1);
    SharedPreferences.setMockInitialValues({
      Constants.accessTokenKey: fakeJwt(1, validFor: const Duration(seconds: -5)),
      'refresh_token': 'refresh-old',
    });
    GetIt.I.registerSingleton<SharedPrefsProvider>(SharedPrefsProvider());
    GetIt.I.registerSingleton<HttpProvider>(HttpProvider());

    refreshCalls = 0;
    onRefresh = (_) => (status: 200, body: {'token': validToken, 'refreshToken': 'refresh-new'});

    adapter = ScriptedAdapter((options) {
      if (options.path.endsWith('auth/refresh')) {
        refreshCalls++;
        return onRefresh(options);
      }
      final authorized = options.headers['Authorization'] == 'Bearer $validToken';
      return authorized ? (status: 200, body: {'ok': true}) : (status: 401, body: {'message': 'istekao'});
    });

    final auth = AuthProvider(null);
    DioClient.init(auth);
    DioClient.dio.httpClientAdapter = adapter;
  });

  tearDown(() async => GetIt.I.reset());

  test('request that is authorized passes through without any refresh', () async {
    final response = await DioClient.dio.get(
      'protected',
      options: Options(headers: {'Authorization': 'Bearer $validToken'}),
    );
    expect(response.statusCode, 200);
    expect(refreshCalls, 0);
  });

  test('401 triggers one refresh and the original request is retried with the new token', () async {
    final response = await DioClient.dio.get(
      'protected',
      options: Options(headers: {'Authorization': 'Bearer stale'}),
    );

    expect(response.data, {'ok': true});
    expect(refreshCalls, 1);
    final protectedCalls = adapter.requests.where((r) => r.path == 'protected').toList();
    expect(protectedCalls.length, 2);
    expect(protectedCalls.last.headers['Authorization'], 'Bearer $validToken');
    // novi tokeni su sačuvani
    expect(await AuthProvider.getAccessToken(), validToken);
    expect(await AuthProvider.getRefreshToken(), 'refresh-new');
  });

  test('refresh request carries the stored refresh token and expired access token', () async {
    await DioClient.dio.get('protected', options: Options(headers: {'Authorization': 'Bearer stale'}));
    final refresh = adapter.requests.firstWhere((r) => r.path.endsWith('auth/refresh'));
    expect(refresh.data['accessToken'], isA<String>());
    expect(refresh.data['refreshToken'], 'refresh-old');
  });

  test('simultaneous 401s share a single refresh', () async {
    final results = await Future.wait([
      for (var i = 0; i < 5; i++)
        DioClient.dio.get('protected', options: Options(headers: {'Authorization': 'Bearer stale'})),
    ]);
    expect(results.every((r) => r.statusCode == 200), isTrue);
    expect(refreshCalls, 1);
  });

  test('refresh can happen again for a later 401 (the shared future is cleared)', () async {
    await DioClient.dio.get('protected', options: Options(headers: {'Authorization': 'Bearer stale'}));
    validToken = fakeJwt(1, validFor: const Duration(minutes: 30));
    await DioClient.dio.get('protected', options: Options(headers: {'Authorization': 'Bearer stale'}));
    expect(refreshCalls, 2);
  });

  test('refresh rejected by the server: request fails and the access token is removed', () async {
    onRefresh = (_) => (status: 400, body: {'message': 'Neispravan refresh token!'});
    await expectLater(
      DioClient.dio.get('protected', options: Options(headers: {'Authorization': 'Bearer stale'})),
      throwsA(isA<DioException>()),
    );
    await expectLater(AuthProvider.getAccessToken(), throwsException);   // korisnik je odjavljen
    expect(adapter.requests.where((r) => r.path == 'protected').length, 1);  // nema beskonačnog ponavljanja
  });

  test('non-401 errors are passed through untouched and do not refresh', () async {
    final failing = ScriptedAdapter((_) => (status: 500, body: {'message': 'greška'}));
    DioClient.dio.httpClientAdapter = failing;
    await expectLater(
      DioClient.dio.get('protected'),
      throwsA(isA<DioException>().having((e) => e.response?.statusCode, 'status', 500)),
    );
    expect(refreshCalls, 0);
  });

  test('without an auth provider no interceptor is installed', () {
    DioClient.init(null);
    expect(DioClient.dio.interceptors.whereType<Interceptor>().any((i) => i.runtimeType.toString() == 'AuthInterceptor'), isFalse);
  });
}
