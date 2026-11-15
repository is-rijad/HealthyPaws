import 'package:flutter_test/flutter_test.dart';
import 'package:healthy_paws/common/resources/access_token_payload.dart';
import 'package:healthy_paws/common/resources/search_result.dart';
import 'package:healthy_paws/common/user_exception.dart';
import 'package:healthy_paws/common/utils/json_parser.dart';
import 'package:healthy_paws/resources/requests/dog/dog_activity_status.dart';
import 'package:healthy_paws/resources/requests/dog/dog_body_condition_score.dart';
import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';
import 'package:healthy_paws/resources/requests/dog/dog_insert_request.dart';
import 'package:healthy_paws/resources/responses/auth/access_token_response.dart';
import 'package:healthy_paws/resources/responses/device/device_response.dart';
import 'package:healthy_paws/resources/responses/dog/calorie_budget_response.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/responses/treat_log/treat_log_response.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_insert_request.dart';
import 'package:healthy_paws/resources/search_objects/dog_weight/dog_weight_search_object.dart';

void main() {
  group('parseJson', () {
    test('uses the factory for the requested type', () {
      final budget = parseJson<CalorieBudgetResponse>({
        'dogId': 3,
        'dailyBudget': 1200.5,
        'treatCalories': 50.0,
        'foodCalories': 300.0,
        'remaining': 850.5,
      });
      expect(budget.dogId, 3);
      expect(budget.remaining, 850.5);
    });

    test('throws for a type without a registered factory', () {
      expect(() => parseJson<String>({}), throwsException);
    });

    test('integer JSON numbers are accepted for double fields', () {
      // ASP.NET serializira 1000.0 kao "1000" -> mora se parsirati u double
      final budget = parseJson<CalorieBudgetResponse>({
        'dogId': 1, 'dailyBudget': 1000, 'treatCalories': 0, 'foodCalories': 0, 'remaining': 1000,
      });
      expect(budget.dailyBudget, 1000.0);
    });

    test('missing required field fails loudly', () {
      expect(() => parseJson<CalorieBudgetResponse>({'dogId': 1}), throwsA(anything));
    });
  });

  group('backend JSON compatibility', () {
    test('AccessTokenResponse', () {
      final r = AccessTokenResponse.fromJson({'token': 'a', 'refreshToken': 'r'});
      expect((r.token, r.refreshToken), ('a', 'r'));
    });

    test('AccessTokenPayload reads subject given as string (JWT standard)', () {
      expect(AccessTokenPayload.fromJson({'sub': '42'}).id, 42);
      expect(AccessTokenPayload(id: 7).toJson(), {'sub': 7});
    });

    test('AccessTokenPayload fails on non numeric subject', () {
      expect(() => AccessTokenPayload.fromJson({'sub': 'abc'}), throwsFormatException);
    });

    test('DogResponse with nested breed and enum gender', () {
      final dog = DogResponse.fromJson({
        'id': 1, 'breedId': 2, 'name': 'Rex', 'dateOfBirth': '2022-05-01T00:00:00',
        'gender': 1, 'activityStatus': 1.2, 'isNeutered': true,
        'breed': {'id': 2, 'name': 'Labrador', 'minWeight': 25.0, 'maxWeight': 36.0},
      });
      expect(dog.gender, DogGender.female);
      expect(dog.breed!.name, 'Labrador');
      expect(dog.dateOfBirth, DateTime(2022, 5, 1));
    });

    test('DogResponse without breed', () {
      final dog = DogResponse.fromJson({
        'id': 1, 'breedId': 2, 'name': 'Rex', 'dateOfBirth': '2022-05-01T00:00:00',
        'gender': 0, 'activityStatus': 1.2, 'isNeutered': false, 'breed': null,
      });
      expect(dog.breed, isNull);
      expect(dog.gender, DogGender.male);
    });

    test('unknown gender value is rejected', () {
      expect(
        () => DogResponse.fromJson({
          'id': 1, 'breedId': 2, 'name': 'R', 'dateOfBirth': '2022-05-01T00:00:00',
          'gender': 5, 'activityStatus': 1.2, 'isNeutered': false,
        }),
        throwsA(anything),
      );
    });

    test('DeviceResponse nests dog and last weight record', () {
      final device = DeviceResponse.fromJson({
        'id': 9, 'dogId': 1, 'macAddress': 'AA:BB:CC:DD:EE:FF', 'isConnected': true,
        'dog': {
          'id': 1, 'breedId': 2, 'name': 'Rex', 'dateOfBirth': '2022-05-01T00:00:00',
          'gender': 0, 'activityStatus': 1.2, 'isNeutered': true,
        },
        'lastWeightRecord': {
          'id': 5, 'bcs': 5, 'weight': 20.5, 'calorie': 350, 'createdAt': '2026-03-01T08:00:00Z',
        },
      });
      expect(device.isConnected, isTrue);
      expect(device.lastWeightRecord!.weight, 20.5);
      expect(device.lastWeightRecord!.createdAt.isUtc, isTrue);
    });

    test('TreatLogResponse', () {
      final log = TreatLogResponse.fromJson({
        'id': 1, 'dogId': 2, 'treatId': 3, 'treatName': 'Kost', 'quantity': 2,
        'calories': 50.0, 'givenAt': '2026-03-01T08:00:00Z',
      });
      expect((log.treatName, log.quantity, log.calories), ('Kost', 2, 50.0));
    });
  });

  group('request serialization matches backend contract', () {
    test('DogInsertRequest uses camelCase and numeric gender', () {
      final json = DogInsertRequest(
        breedId: 2, name: 'Rex', dateOfBirth: DateTime.utc(2022, 5, 1),
        gender: DogGender.female, activityStatus: 1.2, isNeutered: true,
      ).toJson();
      expect(json, {
        'breedId': 2, 'name': 'Rex', 'dateOfBirth': '2022-05-01T00:00:00.000Z',
        'gender': 1, 'activityStatus': 1.2, 'isNeutered': true,
      });
    });

    test('DogWeightInsertRequest roundtrip', () {
      final request = DogWeightInsertRequest.fromJson({
        'dogId': 1, 'bcs': 5, 'weight': 20.0, 'calorie': 350.0, 'createdAt': null,
      });
      final json = request.toJson();
      expect(json['bcs'], 5);
      expect(json['weight'], 20.0);
    });

    test('DogWeightSearchObject omits nulls so server defaults apply', () {
      final json = DogWeightSearchObject(page: 1, pageSize: 10, dogId: 4).toJson();
      expect(json.containsKey('minWeight'), isFalse);
      expect(json.containsKey('orderBy'), isFalse);
      expect(json['dogId'], 4);
      expect(json['descending'], true);
    });
  });

  group('value constants agree with the calorie agent', () {
    test('BCS options are exactly the values the Python agent accepts (1,3,5,7,9)', () {
      expect(dogBodyConditionScoreMap.keys.toList()..sort(), [1, 3, 5, 7, 9]);
    });

    test('activity factor offered by the UI is one the agent supports (1.2)', () {
      expect(dogActivityStatusMap.keys, [1.2]);
    });

    test('gender labels exist for every enum value', () {
      expect(dogGenderMap.keys.toSet(), DogGender.values.toSet());
    });
  });

  group('SearchResult', () {
    test('copyTo appends items and takes hasMore from the source', () {
      final target = SearchResult<int>(items: [1, 2], hasMore: true);
      SearchResult<int>(items: [3], hasMore: false).copyTo(target);
      expect(target.items, [1, 2, 3]);
      expect(target.hasMore, isFalse);
    });

    test('default items list is growable and independent per instance', () {
      final a = SearchResult<int>()..items.add(1);
      expect(SearchResult<int>().items, isEmpty);
      expect(a.items, [1]);
    });
  });

  test('UserException exposes its message', () {
    expect(UserException('Greška').toString(), 'Greška');
    expect(UserException(null).toString(), '');
  });
}
