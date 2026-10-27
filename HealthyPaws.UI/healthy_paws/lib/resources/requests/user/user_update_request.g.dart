// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user_update_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

UserUpdateRequest _$UserUpdateRequestFromJson(Map<String, dynamic> json) =>
    UserUpdateRequest(
      email: json['email'] as String,
      changePassword: json['changePassword'] as bool,
      oldPassword: json['oldPassword'] as String?,
      password: json['password'] as String?,
      passwordAgain: json['passwordAgain'] as String?,
    );

Map<String, dynamic> _$UserUpdateRequestToJson(UserUpdateRequest instance) =>
    <String, dynamic>{
      'email': instance.email,
      'changePassword': instance.changePassword,
      'oldPassword': instance.oldPassword,
      'password': instance.password,
      'passwordAgain': instance.passwordAgain,
    };
