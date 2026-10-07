import 'package:dio/dio.dart';
import 'package:healthy_paws/common/components/snackbars/error_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/dio_client.dart';
import 'package:healthy_paws/common/env.dart';
import 'package:healthy_paws/common/resources/json_parsable.dart';
import 'package:healthy_paws/common/resources/search_result.dart';
import 'package:healthy_paws/common/utils/json_parser.dart';
import 'package:healthy_paws/resources/search_objects/base_search_object/base_search_object.dart';

class HttpProvider {
  final String _apiUrl = Environment.apiUrl;

  Future<TResponse> getSingle<TResponse>(
    String url, {
    int? id,
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      var path = "$_apiUrl$url";
      if (id != null) path += "/$id";
      var response = await DioClient.dio.get(
        path,
        queryParameters: queryParameters,
      );
      if (response.data is Map<String, dynamic>) {
        return parseJson<TResponse>(response.data);
      }
      return response.data as TResponse;
    } finally {}
  }

  Future<SearchResult<TResponse>> getAll<
    TResponse,
    TSearchObject extends BaseSearchObject
  >(String url, TSearchObject search, {Map<String, dynamic>? query}) async {
    try {
      var queryParameters = search.toJson();
      if (query != null) {
        queryParameters.addEntries(query.entries);
      }
      var response = await DioClient.dio.get(
        "$_apiUrl$url",
        queryParameters: queryParameters,
      );
      return SearchResult<TResponse>(
        items: List<TResponse>.from(
          response.data["items"].map((it) => parseJson<TResponse>(it)),
        ),
        hasMore: response.data["hasMore"],
      );
    } finally {}
  }

  Future<TResponse> post<TRequest extends JsonParsable, TResponse>(
    String url,
    TRequest? request, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      var response = await DioClient.dio.post(
        "$_apiUrl$url",
        data: request?.toJson(),
        queryParameters: queryParameters,
      );
      if (response.data != "") {
        return parseJson<TResponse>(response.data);
      }
      return response.data;
    } on DioException catch (e) {
      Constants.messengerKey.currentState?.showSnackBar(
        ErrorSnackbar(message: e.response?.data["message"]),
      );
      rethrow;
    } on Exception catch (_) {
      rethrow;
    } finally {}
  }

  Future<TResponse?> put<TRequest extends JsonParsable, TResponse>(
    String url,
    int id,
    TRequest? request, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      var response = await DioClient.dio.put(
        "$_apiUrl$url/$id",
        data: request?.toJson(),
        queryParameters: queryParameters,
      );
      if (response.data != "") {
        return parseJson<TResponse>(response.data);
      }
      return null;
    } finally {}
  }

  Future<void> delete(
    String url,
    int? id, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      var requestUrl = url.endsWith("/") ? "$_apiUrl$url" : "$_apiUrl$url/";
      if (id != null) {
        requestUrl += id.toString();
      }
      await DioClient.dio.delete(requestUrl, queryParameters: queryParameters);
    } finally {}
  }
}
