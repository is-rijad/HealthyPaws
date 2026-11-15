import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/resources/access_token_payload.dart';
import 'package:healthy_paws/providers/auth_provider.dart';
import 'package:healthy_paws/resources/requests/auth/login_request.dart';
import 'package:healthy_paws/resources/requests/auth/refresh_token_request.dart';
import 'package:healthy_paws/resources/responses/auth/access_token_response.dart';

import '../helpers/fakes.dart';

void main() {
  late FakeHttpProvider http;
  late AuthProvider auth;

  AccessTokenResponse tokens(int userId, [String refresh = 'refresh-1']) =>
      AccessTokenResponse(token: fakeJwt(userId), refreshToken: refresh);

  setUp(() async {
    final fakes = await registerFakes();
    http = fakes.http;
    auth = AuthProvider(null);
  });

  test('constructor seeds payload from stored token and notifies', () {
    final seeded = AuthProvider(AccessTokenPayload(id: 5));
    expect(seeded.accessTokenPayload.id, 5);
  });

  test('login stores both tokens and exposes the user id from the JWT subject', () async {
    http.nextPostResult = tokens(42);
    var notified = 0;
    auth.addListener(() => notified++);

    await auth.login(LoginRequest(email: 'ana@test.ba', password: 'Lozinka1!'));

    expect(auth.accessTokenPayload.id, 42);
    expect(await AuthProvider.getRefreshToken(), 'refresh-1');
    expect(AccessTokenPayload.fromJson({'sub': '42'}).id, (await AuthProvider.getPayload()).id);
    expect(await auth.getUserId(), 42);
    expect(notified, greaterThan(0));
    expect(http.bodies.single, {'email': 'ana@test.ba', 'password': 'Lozinka1!'});
  });

  test('failed login stores nothing', () async {
    http.error = DioException(requestOptions: RequestOptions(path: 'auth/login'));
    await expectLater(
      auth.login(LoginRequest(email: 'a@b.c', password: 'x')),
      throwsA(isA<DioException>()),
    );
    expect(AuthProvider.getAccessToken(), throwsException);
    expect(AuthProvider.getRefreshToken(), throwsException);
  });

  test('tokens are unavailable before login', () {
    expect(AuthProvider.getAccessToken(), throwsA(predicate((e) => '$e'.contains('not logged in'))));
  });

  test('refresh replaces the stored tokens', () async {
    http.nextPostResult = tokens(7, 'refresh-1');
    await auth.login(LoginRequest(email: 'a@b.c', password: 'x'));
    http.nextPostResult = tokens(7, 'refresh-2');

    await auth.refresh(RefreshTokenRequest(accessToken: 'a', refreshToken: 'refresh-1'));

    expect(await AuthProvider.getRefreshToken(), 'refresh-2');
    expect(http.calls.last, 'POST auth/refresh');
  });

  test('logout clears stored tokens even when the server call fails', () async {
    http.nextPostResult = tokens(7);
    await auth.login(LoginRequest(email: 'a@b.c', password: 'x'));
    http.error = DioException(requestOptions: RequestOptions(path: 'auth/logout'));

    await auth.logout();

    expect(AuthProvider.getAccessToken(), throwsException);
    expect(AuthProvider.getRefreshToken(), throwsException);
  });

  test('access token key is the one the interceptor clears on failed refresh', () async {
    http.nextPostResult = tokens(7);
    await auth.login(LoginRequest(email: 'a@b.c', password: 'x'));
    expect(Constants.accessTokenKey, 'access_token');
    expect(await AuthProvider.getAccessToken(), isNotEmpty);
  });

  test('getNewToken fetches and stores a fresh pair', () async {
    http.onGetSingle = (_) => tokens(9, 'refresh-9');
    await auth.getNewToken();
    expect(await auth.getUserId(), 9);
    expect(await AuthProvider.getRefreshToken(), 'refresh-9');
  });
}
