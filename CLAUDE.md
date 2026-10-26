# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

HealthyPaws is a dog health/feeding app tied to a smart device (identified by MAC address). Monorepo with two independent projects: a .NET Web API (`HealthyPaws.Backend/`) and a Flutter client (`HealthyPaws.UI/healthy_paws/`). There are no automated tests in either project. User-facing strings and exception messages are in Bosnian (e.g. `"Nije pronađeno!"`).

## Backend (`HealthyPaws.Backend/`, .NET 10, SQL Server, EF Core)

```bash
docker compose up -d healthy-paws-db          # SQL Server 2022 on host port 1434
dotnet build HealthyPaws.Backend.slnx
dotnet run --project HealthyPaws.WebApi       # http://localhost:5116, https://localhost:7151
dotnet ef migrations add <Name> --project HealthyPaws.Database --startup-project HealthyPaws.WebApi
dotnet ef database update --project HealthyPaws.Database --startup-project HealthyPaws.WebApi
```

- `HealthyPaws.WebApi/.env` (loaded by DotNetEnv in `Program.cs`) must define `ConnectionStrings__Default` and the MSSQL SA password used by docker-compose. JWT `Issuer`/`Key` come from `appsettings.json` (`Jwt` section).
- `MigrateDatabaseAsync()` exists in `HealthyPaws.Database/DatabaseConfiguration.cs` but is commented out in `Program.cs`; migrations are applied manually.
- OpenAPI is mapped only in Development.

### Architecture

Layered projects: `WebApi` (controllers, `ExceptionFilter`) → `Services` → `Database` (EF entities, `HealthyPawsDbContext`, Migrations, Seed) ; `HealhtyPaws.Models` (sic: the typo is in the folder and namespace) holds Requests/Responses/SearchObjects shared across layers.

- **Generic CRUD base classes** drive most endpoints: `BaseController`/`BaseGetController`/`BaseCrudController` and `BaseService`/`BaseCrudService<TEntity, TInsert, TUpdate, TResponse, TSearch>`. A new resource = entity + request/response/search models + service (override hooks such as `ApplyFilter`, `BeforeInsert`, `AfterInsert`, `BeforeUpdate`, `PrepareForResponse`) + controller + registration in `ServicesConfiguration.ConfigureServices`. `BaseCrudService` wraps every insert/update/delete in a transaction.
- **Mapping** uses Mapster; configs are discovered by assembly scan of `MapsterConfiguration`'s assembly, and null values are ignored globally on map (so updates are partial).
- **Auth**: every controller requires an authenticated user by default (global `AuthorizeFilter`, see `AuthorizationConfiguration`); opt out with `[AllowAnonymous]`. JWT bearer + refresh tokens (`TokenManager`, `RefreshToken`, `ResetPasswordToken` entities). Claims are not remapped (`sub`, `role`). Services get the current user via `AuthorizationService.GetUserId()`.
- **Ownership model**: a user owns dogs *through devices* (`UserDevice` → `Device.DogId`). Service-level access checks and filters (e.g. `DogService`) query `UserDevices` rather than a direct user→dog FK.
- **Errors**: throw `NotFoundException` / `BadRequestException` / `UnauthorizedException`; `ExceptionFilter` converts them to HTTP responses.
- **Calorie budget** (`DogService.GetCalorieBudgetAsync`): `70 * weight^0.75 * ActivityStatus` (latest `DogWeight`) minus calories from `TreatLog`s in the period.

## Flutter client (`HealthyPaws.UI/healthy_paws/`)

```bash
flutter pub get
flutter run --dart-define-from-file=config/dev.json   # same args as .vscode/launch.json
flutter analyze
```

- All config (API URL, keys) is compile-time via `String.fromEnvironment` in `lib/common/env.dart`; running without `--dart-define-from-file=config/dev.json` leaves `API_URL` empty. Some `Environment` entries (Stripe, Firebase, OpenRoute, hubs) are unused leftovers.
- **Bootstrap** (`lib/main.dart`): GetIt registers `HttpProvider` (factory), `SharedPrefsProvider`, `UiProvider`; the stored access-token payload seeds `AuthProvider`; `DioClient.init` installs `AuthInterceptor` (token attach/refresh); the app is wrapped in a `MultiProvider` of `ChangeNotifierProvider`s.
- **Data layer**: one provider per backend resource in `lib/providers/` (extend `common/providers/base_provider.dart`, which wraps Dio calls). Wizard state for the add-device/dog flow lives in `providers/wizard_providers/` (`first_step` … `fourth_step`; the last step is the daily feeding schedule, matching `DogMeal` on the backend).
- UI is organized as `pages/` (screens, `main_shell.dart` hosts the bottom navigation), `components/` (feature composites such as dog form, BCS field, weight), and `widgets/` (generic reusable pieces). UI imports `material_ui/material_ui.dart` rather than `flutter/material.dart`.
- Global error handling goes through `common/error_handler.dart` (`runZonedGuarded`), with `UserException` for messages meant for the user.
