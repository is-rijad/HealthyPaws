import 'package:healthy_paws/common/components/snackbars/success_snackbar.dart';
import 'package:healthy_paws/common/constants.dart';
import 'package:healthy_paws/common/providers/base_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/first_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/fourth_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/second_step_provider.dart';
import 'package:healthy_paws/providers/wizard_providers/third_step_provider.dart';
import 'package:healthy_paws/resources/requests/device/device_insert_request.dart';
import 'package:healthy_paws/resources/requests/device/device_update_request.dart';
import 'package:healthy_paws/resources/requests/dog_weight/dog_weight_insert_request.dart';
import 'package:healthy_paws/resources/responses/device/device_response.dart';
import 'package:healthy_paws/resources/search_objects/device/device_search_object.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

class DevicesProvider
    extends
        BaseProvider<
          DeviceResponse,
          DeviceSearchObject,
          DeviceInsertRequest,
          DeviceUpdateRequest
        > {
  new() : super(providerName: "devices");

  Future completeWizard(BuildContext context) async {
    final firstStepRequest = context.read<FirstStepProvider>().request;
    final secondStepRequest = context.read<SecondStepProvider>().request;
    final thirdStepRequest = context.read<ThirdStepProvider>().request;
    final mealTimes = context.read<FourthStepProvider>().mealTimes;
    final request = DeviceInsertRequest(
      macAddress: firstStepRequest!.macAddress,
      dog: secondStepRequest,
      dogWeight: thirdStepRequest,
      mealTimes: mealTimes,
    );
    await insert(request);
    if (context.mounted) {
      Constants.navigatorKey.currentState?.pop(true);
      Constants.messengerKey.currentState?.showSnackBar(
        SuccessSnackbar(message: "Uspješno ste dodali uređaj!"),
      );
    }
  }

  Future<DeviceResponse> addWeight(
    int deviceId,
    DogWeightInsertRequest request,
  ) async {
    final updated = await httpProvider
        .post<DogWeightInsertRequest, DeviceResponse>(
          "$providerName/$deviceId/weights",
          request,
        );
    updateLocally(updated);
    return updated;
  }
}
