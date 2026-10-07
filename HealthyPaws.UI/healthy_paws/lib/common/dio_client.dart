import 'package:dio/dio.dart';
import 'package:healthy_paws/common/auth_interceptor.dart';
import 'package:healthy_paws/providers/auth_provider.dart';

class DioClient {
  static late Dio dio;

  static void init(AuthProvider? authProvider) {
    dio = Dio(
      BaseOptions(
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 20),
      ),
    );

    if (authProvider != null) {
      dio.interceptors.add(AuthInterceptor(authProvider));
    }
  }
}
