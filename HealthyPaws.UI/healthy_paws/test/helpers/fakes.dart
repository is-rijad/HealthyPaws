import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:get_it/get_it.dart';
import 'package:healthy_paws/common/providers/http_provider.dart';
import 'package:healthy_paws/common/providers/shared_prefs_provider.dart';
import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:healthy_paws/common/resources/search_result.dart';
import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// JWT bez potpisa (JwtDecoder ga samo dekodira); `sub` je string kao u backendu.
String fakeJwt(int userId, {Duration validFor = const Duration(minutes: 15)}) {
  String enc(Map<String, dynamic> m) =>
      base64Url.encode(utf8.encode(jsonEncode(m))).replaceAll('=', '');
  final exp = DateTime.now().add(validFor).millisecondsSinceEpoch ~/ 1000;
  return '${enc({'alg': 'HS256', 'typ': 'JWT'})}.${enc({'sub': '$userId', 'exp': exp})}.sig';
}

/// Snima pozive umjesto da ide na mrežu.
class FakeHttpProvider extends HttpProvider {
  final List<String> calls = [];
  final List<Object?> bodies = [];
  Object? Function(String url)? onGetSingle;
  Object? nextPostResult;
  Object? nextPutResult;
  SearchResult? nextGetAll;
  Map<String, dynamic>? lastQuery;
  Object? error;

  @override
  Future<TResponse> getSingle<TResponse>(
    String url, {
    int? id,
    Map<String, dynamic>? queryParameters,
  }) async {
    calls.add('GET $url${id != null ? '/$id' : ''}');
    lastQuery = queryParameters;
    if (error != null) throw error!;
    return onGetSingle!(url) as TResponse;
  }

  @override
  Future<SearchResult<TResponse>> getAll<
    TResponse,
    TSearchObject extends BaseSearchObject
  >(String url, TSearchObject search, {Map<String, dynamic>? query}) async {
    calls.add('GETALL $url page=${search.page}');
    lastQuery = search.toJson();
    if (error != null) throw error!;
    final result = nextGetAll!;
    return SearchResult<TResponse>(
      items: List<TResponse>.from(result.items),
      hasMore: result.hasMore,
    );
  }

  @override
  Future<TResponse> post<TRequest extends JsonParsable, TResponse>(
    String url,
    TRequest? request, {
    Map<String, dynamic>? queryParameters,
  }) async {
    calls.add('POST $url');
    bodies.add(request?.toJson());
    if (error != null) throw error!;
    return nextPostResult as TResponse;
  }

  @override
  Future<TResponse?> put<TRequest extends JsonParsable, TResponse>(
    String url,
    int id,
    TRequest? request, {
    Map<String, dynamic>? queryParameters,
  }) async {
    calls.add('PUT $url/$id');
    bodies.add(request?.toJson());
    if (error != null) throw error!;
    return nextPutResult as TResponse?;
  }

  @override
  Future<void> delete(
    String url,
    int? id, {
    Map<String, dynamic>? queryParameters,
  }) async {
    calls.add('DELETE $url/$id');
    if (error != null) throw error!;
  }
}

/// Dio adapter kojim test upravlja: handler vraća (status, json) za svaki zahtjev.
class ScriptedAdapter implements HttpClientAdapter {
  final List<RequestOptions> requests = [];
  final ({int status, Object? body}) Function(RequestOptions options) handler;

  ScriptedAdapter(this.handler);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    final r = handler(options);
    return ResponseBody.fromString(
      jsonEncode(r.body),
      r.status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

/// Registruje GetIt zavisnosti koje provideri očekuju i vraća ih.
Future<({FakeHttpProvider http, SharedPrefsProvider prefs})> registerFakes({
  Map<String, Object> prefsValues = const {},
}) async {
  await GetIt.I.reset();
  SharedPreferences.setMockInitialValues(Map.of(prefsValues));
  final http = FakeHttpProvider();
  final prefs = SharedPrefsProvider();
  GetIt.I.registerSingleton<HttpProvider>(http);
  GetIt.I.registerSingleton<SharedPrefsProvider>(prefs);
  return (http: http, prefs: prefs);
}
