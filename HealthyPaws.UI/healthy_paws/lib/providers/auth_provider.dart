import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/providers/http_provider.dart';
import 'package:healthy_paws/common/providers/shared_prefs_provider.dart';
import 'package:healthy_paws/common/resources/access_token_payload.dart';
import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:healthy_paws/common/utils/json_parser.dart';
import 'package:healthy_paws/resources/requests/auth/login_request.dart';
import 'package:healthy_paws/resources/requests/auth/refresh_token_request.dart';
import 'package:healthy_paws/resources/responses/auth/access_token_response.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

class AuthProvider with ChangeNotifier {
  late AccessTokenPayload _accessTokenPayload;
  AccessTokenPayload get accessTokenPayload => _accessTokenPayload;

  final HttpProvider _httpProvider = GetIt.I<HttpProvider>();
  final _sharedPrefsProvider = GetIt.I<SharedPrefsProvider>();
  final _providerName = "auth";
  static final _refreshTokenKey = "refresh_token";

  AuthProvider(AccessTokenPayload? payload) {
    if (payload != null) {
      _accessTokenPayload = payload;
      notifyListeners();
    }
  }

  Future<AccessTokenResponse> login(LoginRequest request) async {
    final response = await _httpProvider
        .post<LoginRequest, AccessTokenResponse>(_providerName, request);
    await _setAccessToken(response.token);
    await _setRefreshToken(response.refreshToken);
    return response;
  }

  Future<AccessTokenResponse> getNewToken() async {
    final response = await _httpProvider.getSingle<AccessTokenResponse>(
      _providerName,
    );
    await _setAccessToken(response.token);
    await _setRefreshToken(response.refreshToken);
    return response;
  }

  Future<void> logout() async {
    try {
      await _httpProvider.post<JsonParsable, dynamic>(
        "$_providerName/logout",
        null,
      );
    } catch (_) {}
    await _sharedPrefsProvider.deleteString(Constants.accessTokenKey);
    await _sharedPrefsProvider.deleteString(_refreshTokenKey);
  }

  Future<void> _setAccessToken(String token) async {
    await _sharedPrefsProvider.setString(Constants.accessTokenKey, token);
    _accessTokenPayload = await getPayload();
    notifyListeners();
  }

  Future<void> _setRefreshToken(String token) async {
    await _sharedPrefsProvider.setString(_refreshTokenKey, token);
  }

  Future refresh(RefreshTokenRequest request) async {
    try {
      final response = await _httpProvider
          .post<RefreshTokenRequest, AccessTokenResponse>(
            "$_providerName/refresh",
            request,
          );
      await _setAccessToken(response.token);
      await _setRefreshToken(response.refreshToken);
    } on DioException {
      logout();
      rethrow;
    }
  }

  Future checkAuth() async {
    await _httpProvider.getSingle("$_providerName/auth");
  }

  static Future<String> getAccessToken() async {
    final token = await GetIt.I<SharedPrefsProvider>().getString(
      Constants.accessTokenKey,
    );
    if (token == null) {
      throw Exception("User is not logged in!");
    }
    return token;
  }

  static Future<String> getRefreshToken() async {
    final token = await GetIt.I<SharedPrefsProvider>().getString(
      _refreshTokenKey,
    );
    if (token == null) {
      throw Exception("User is not logged in!");
    }
    return token;
  }

  Future<int> getUserId() async {
    var token = await getAccessToken();
    var payload = JwtDecoder.decode(token);
    return int.parse(payload["sub"]);
  }

  static Future<AccessTokenPayload> getPayload() async {
    var token = await getAccessToken();
    var payload = JwtDecoder.decode(token);
    return parseJson<AccessTokenPayload>(payload);
  }
}
