import 'package:flutter_test/flutter_test.dart';
import 'package:healthy_paws/common/resources/search_result.dart';
import 'package:healthy_paws/providers/dogs_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/first_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/fourth_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/second_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/third_step_provider.dart';
import 'package:healthy_paws/resources/requests/dog/dog_gender.dart';
import 'package:healthy_paws/resources/requests/dog/dog_insert_request.dart';
import 'package:healthy_paws/resources/responses/dog/calorie_budget_response.dart';
import 'package:healthy_paws/resources/responses/dog/dog_response.dart';
import 'package:healthy_paws/resources/search_objects/dog/dog_search_object.dart';

import '../helpers/fakes.dart';

DogResponse dog(int id, [String name = 'Rex']) => DogResponse(
  id: id, breedId: 1, name: name, dateOfBirth: DateTime(2022), gender: DogGender.male,
  activityStatus: 1.2, isNeutered: true,
);

DogInsertRequest insertRequest() => DogInsertRequest(
  breedId: 1, name: 'Rex', dateOfBirth: DateTime(2022), gender: DogGender.male,
  activityStatus: 1.2, isNeutered: true,
);

void main() {
  late FakeHttpProvider http;
  late DogsProvider provider;

  setUp(() async {
    http = (await registerFakes()).http;
    provider = DogsProvider();
  });

  group('fetchData paging', () {
    test('first page replaces existing items, later pages append', () async {
      final search = DogSearchObject(page: 1, pageSize: 10);
      http.nextGetAll = SearchResult(items: [dog(1), dog(2)], hasMore: true);
      await provider.fetchData(search);
      expect(provider.searchResult.items.map((d) => d.id), [1, 2]);
      expect(search.page, 2);

      http.nextGetAll = SearchResult(items: [dog(3)], hasMore: false);
      await provider.fetchData(search);
      expect(provider.searchResult.items.map((d) => d.id), [1, 2, 3]);
      expect(provider.searchResult.hasMore, isFalse);

      search.page = 1;
      http.nextGetAll = SearchResult(items: [dog(9)], hasMore: true);
      await provider.fetchData(search);
      expect(provider.searchResult.items.map((d) => d.id), [9]);
    });

    test('page 0 is treated as first page', () async {
      http.nextGetAll = SearchResult(items: [dog(1)]);
      await provider.fetchData(DogSearchObject(page: 1, pageSize: 10));
      http.nextGetAll = SearchResult(items: [dog(2)]);
      await provider.fetchData(DogSearchObject(page: 0, pageSize: 10));
      expect(provider.searchResult.items.map((d) => d.id), [2]);
    });

    test('notifies listeners once per fetch', () async {
      var notifications = 0;
      provider.addListener(() => notifications++);
      http.nextGetAll = SearchResult(items: [dog(1)]);
      await provider.fetchData(DogSearchObject(page: 1, pageSize: 10));
      expect(notifications, 1);
    });

    test('a failing request leaves page counter and list consistent', () async {
      final search = DogSearchObject(page: 2, pageSize: 10);
      provider.searchResult.items.add(dog(1));
      http.error = Exception('offline');
      await expectLater(provider.fetchData(search), throwsException);
      expect(search.page, 2);
      expect(provider.searchResult.items.length, 1);
    });

    test('clearData resets list, hasMore and page', () {
      provider.searchResult.items.add(dog(1));
      provider.searchResult.hasMore = false;
      final search = DogSearchObject(page: 5, pageSize: 10);
      provider.clearData(search);
      expect(provider.searchResult.items, isEmpty);
      expect(provider.searchResult.hasMore, isTrue);
      expect(search.page, 1);
    });
  });

  group('insert / update / delete', () {
    test('insertWithEvent puts the new item first and notifies', () async {
      provider.searchResult.items.add(dog(1));
      http.nextPostResult = dog(2);
      var notified = false;
      provider.addListener(() => notified = true);
      final created = await provider.insertWithEvent(insertRequest());
      expect(created.id, 2);
      expect(provider.searchResult.items.map((d) => d.id), [2, 1]);
      expect(notified, isTrue);
      expect(http.bodies.single, containsPair('name', 'Rex'));
    });

    test('insert does not touch the local list when the request fails', () async {
      http.error = Exception('400');
      await expectLater(provider.insertWithEvent(insertRequest()), throwsException);
      expect(provider.searchResult.items, isEmpty);
    });

    test('updateWithEvent replaces the item in place', () async {
      provider.searchResult.items.addAll([dog(1), dog(2)]);
      http.nextPutResult = dog(2, 'Maks');
      final updated = await provider.updateWithEvent(2, insertRequest());
      expect(updated!.name, 'Maks');
      expect(provider.searchResult.items.map((d) => d.name), ['Rex', 'Maks']);
      expect(http.calls.last, 'PUT dogs/2');
    });

    test('updateWithEvent with empty server response keeps local list', () async {
      provider.searchResult.items.add(dog(1));
      http.nextPutResult = null;
      expect(await provider.updateWithEvent(1, insertRequest()), isNull);
      expect(provider.searchResult.items.single.name, 'Rex');
    });

    test('deleteWithEvent removes the item after the server confirms', () async {
      provider.searchResult.items.addAll([dog(1), dog(2)]);
      await provider.deleteWithEvent(1);
      expect(provider.searchResult.items.map((d) => d.id), [2]);
      expect(http.calls.last, 'DELETE dogs/1');
    });

    test('delete keeps the item when the server fails', () async {
      provider.searchResult.items.add(dog(1));
      http.error = Exception('500');
      await expectLater(provider.deleteWithEvent(1), throwsException);
      expect(provider.searchResult.items.length, 1);
    });

    test('update/delete of an item missing from the local list is a no-op locally', () async {
      // npr. stavka obrisana s drugog ekrana dok je zahtjev u toku; server je zahtjev već obradio
      provider.searchResult.items.add(dog(1));
      http.nextPutResult = dog(99);
      await provider.updateWithEvent(99, insertRequest());
      await provider.deleteWithEvent(99);
      expect(provider.searchResult.items.map((d) => d.id), [1]);
      expect(http.calls, containsAll(['PUT dogs/99', 'DELETE dogs/99']));
    });
  });

  group('DogsProvider', () {
    test('calorie budget sends UTC ISO range to the nested route', () async {
      http.onGetSingle = (_) => CalorieBudgetResponse(
        dogId: 3, dailyBudget: 1000, treatCalories: 10, foodCalories: 200, remaining: 790,
      );
      final from = DateTime(2026, 3, 10, 0, 0);
      final to = DateTime(2026, 3, 11, 0, 0);
      final budget = await provider.getCalorieBudget(3, from: from, to: to);
      expect(budget.remaining, 790);
      expect(http.calls.single, 'GET dogs/3/calorie-budget');
      expect(http.lastQuery!['from'], from.toUtc().toIso8601String());
      expect(http.lastQuery!['to'], to.toUtc().toIso8601String());
      expect(http.lastQuery!['from'], endsWith('Z'));
    });
  });

  group('wizard state providers', () {
    test('start empty and hold values across steps', () {
      final first = FirstStepProvider();
      final second = SecondStepProvider();
      final third = ThirdStepProvider();
      final fourth = FourthStepProvider();
      expect([first.request, second.request, third.bcs, third.weight, fourth.mealTimes, fourth.calorie],
          everyElement(isNull));

      second.request = insertRequest();
      third..bcs = 5..weight = 20.5;
      fourth..mealTimes = ['08:00:00', '18:00:00']..calorie = 350;
      expect(second.request!.name, 'Rex');
      expect((third.bcs, third.weight), (5, 20.5));
      expect(fourth.mealTimes, ['08:00:00', '18:00:00']);
    });
  });
}
