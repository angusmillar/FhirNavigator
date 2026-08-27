# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

FHIR Navigator is a **non-official support library layered on top of the [Official Firely .NET SDK for HL7 FHIR](https://github.com/FirelyTeam/firely-net-sdk)**. It takes a hard dependency on `Hl7.Fhir.R4` and wraps the raw `FhirClient` to solve the recurring real-world problems: OAuth2 token renewal, basic/bearer auth, proxies, multiple named FHIR servers, automatic search pagination, an in-memory resource cache, and reference resolution across contained/cached/remote resources.

The library is published as a NuGet package, but **only CI packs it** — `GeneratePackageOnBuild` is deliberately off, so a local `dotnet build` produces no `.nupkg`. Consumers only ever see `AddFhirNavigator()`, `IFhirNavigatorFactory`, `IFhirNavigator`, `SearchInfo` and `IFhirResourceSearchCache`; everything else is internal plumbing.

## Layout & commands

`src/FhirNavigator.sln` contains two projects, both `net9.0` / `LangVersion 13` / nullable enabled:

- `src/FhirNavigator/` — the library.
- `src/ConsoleApp/` — a runnable sample/manual test harness (queries Patients, writes a CSV). There is **no automated test project**; verification is done by running ConsoleApp against a real or local FHIR server.

```powershell
dotnet build src/FhirNavigator.sln
dotnet run --project src/ConsoleApp          # runs the sample against the configured repository
dotnet pack src/FhirNavigator/FhirNavigator.csproj -c Release
```

## Releasing

Publishing to NuGet.org is automated by `.github/workflows/publish-nuget.yml` and triggers **only** on a pushed lowercase `v` version tag:

```powershell
# 1. Rewrite <PackageReleaseNotes> in FhirNavigator.csproj for this release, commit,
#    and merge to release (tag from there to match release history)
git tag v1.1.0
git push origin v1.1.0
```

`<PackageReleaseNotes>` is **not** generated — it is hand-written and currently describes the 1.0.0 breaking changes. Rewrite it every release, or the NuGet page will advertise the previous release's notes.

The tag is the single source of truth for the package version — the workflow passes `-p:Version=<tag minus the v>` to build and pack, and `<Version>` in the csproj is deliberately pinned to `0.0.0-local` so a locally built `.nupkg` can never be mistaken for a release. Do not expect to bump `<Version>` per release; bump the tag.

Notes:

- Tags must be lowercase `v` and semver (`v1.0.0`, `v1.1.0-beta.1`). The historical uppercase `V0.0.x` tags will **not** trigger a run — no run, no error.
- Authentication uses NuGet.org **Trusted Publishing** (OIDC), not a stored API key. There is no secret in the repo and nothing to rotate. The policy is owned by nuget.org user `AngusMillar` and pinned to three things: repository `angusmillar/FhirNavigator`, workflow **file name** `publish-nuget.yml`, and an empty environment. Renaming or moving the workflow file silently breaks publishing until the policy is updated to match.
- The policy is also **scoped to the `FhirNavigator` package**. Publishing a new package ID from this workflow requires widening that scope on nuget.org first, or the push is rejected even though the OIDC exchange succeeds.
- `workflow_dispatch` runs a build/pack dry run that publishes nothing and uploads the `.nupkg`/`.snupkg` as build artifacts. Its "Run workflow" button only appears if the workflow exists on the repo's **default branch** (`release`) — a copy on `development` alone is not enough. Tag-triggered publishing has no such constraint; the workflow only needs to be in the tagged commit's tree.
- The push uses `--skip-duplicate`, so re-running a release that already published is a no-op rather than a failure.
- `GeneratePackageOnBuild` is off by design. CI runs `dotnet pack` explicitly, so nothing packs locally and there is no stray `.nupkg` to confuse with a release.
- A newly pushed version is accepted immediately but takes a few minutes to appear on nuget.org while validation runs. A successful workflow with "Your package was pushed" in the log means the release worked, even if the version is not yet listed.

**Versioning contract.** 0.0.9 was the last of the `0.0.x` line, where anything could change. From 1.0.0 onward this package is under SemVer: a namespace move, a removed public member, or a new `required` member on a public settings class is a **major** bump. Note that `required` members are source-breaking for consumers who construct settings objects in C#, but invisible to consumers who bind them from `appsettings.json`.

## Architecture

### Registration is where the shape of the system is decided

`RepositoryFhirNavigatorRegistrationExtension.AddFhirNavigator()` is the single wiring point. For **each** `FhirRepositorySettings` entry it registers:

1. A named `HttpClient` keyed by `Code`, with `BaseAddress = ServiceBaseUrl` and the `User-Agent` header, and a handler pipeline of `ProxyHttpClientHandler` (primary) → `RetryDelegatingHandler` → `AuthenticationDelegatingHandler`.
2. If `UseOAuth2`, a second named `HttpClient` keyed by `OAuth2ClientCode` (`"{Code}-OAuth2"`) pointed at `TokenEndpointUrl`.
3. A **keyed** `IFhirNavigator` registration under `Code`.

So the repository `Code` string is the join key across HttpClients, tokens and navigators. Consumers get an instance via `IFhirNavigatorFactory.GetFhirNavigator(code)`, which is just `GetRequiredKeyedService<IFhirNavigator>(code)`.

Two quirks to be aware of before editing this file: the keyed `IFhirNavigator` factory lambda calls `services.BuildServiceProvider()` (building a throwaway provider per resolution), and `AuthenticationDelegatingHandler` is a transient with a **mutable `OrderRepositorySettings` property** that `AuthenticationDelegatingHandlerFactory` stamps at handler-creation time — that property is how a shared handler type learns which repository it is serving. Both are load-bearing; don't "clean them up" without tracing the consequences.

### Call layering

`IFhirNavigator` (`FhirNavigator.cs`) → `IFhirCallService` → `Api/Fhir*Api` → Firely `FhirClient`.

- **`FhirNavigator`** is the user-facing façade. It supplies `RepositorySettings.Code` to every call service method, checks the cache before remote GETs, and converts "not found on write" into `ApplicationException`.
- **`FhirCallService`** holds the only non-trivial orchestration: the **pagination loop** in `Search<T>`, which walks `next` links via `FhirSearchApi.ContinueAsync` until exhausted or `pageLimiter` is reached, adding every returned `Bundle` into the cache, and returns only `SearchInfo` metadata. It also enforces that a transaction `Bundle.Type` is `Transaction`.
- **`Api/*`** is one thin class per FHIR interaction (get/search/create/update/delete/transaction). Each obtains a fresh Firely `FhirClient` from `IFhirHttpClientFactory` (JSON, `SearchParameterHandling.Lenient`) wrapping the named `HttpClient`, then logs and rethrows on failure. `FhirGetApi` deliberately returns `null` on `404`/`410` rather than throwing.

**Search returns metadata, not resources.** `Search<T>()` gives you a `SearchInfo` record; the resources land in `fhirNavigator.Cache` and are read with `Cache.GetList<T>()` / `Cache.Get<T>(id)`. This is the most important API idiom in the codebase — see `src/ConsoleApp/Application.cs` for the canonical usage.

### Cache

`FhirResourceSearchCache` is a `Dictionary<resourceTypeName, Dictionary<resourceId, Resource>>`, registered **transient** (so its lifetime follows the resolved `IFhirNavigator`), with **no thread-safety and no eviction**. Callers are expected to `Clear()` when done. `GetResource` consults it before hitting the wire, so a stale cached resource wins over the server.

### Reference resolution

`IFhirNavigator.GetResource<T>(ResourceReference, errorLocationDisplay[, parentResource])` resolves in this order: validate the reference parses and its resource type matches `T` → if the reference is **contained** (`#id`), pull it from `parentResource.Contained` (throwing if no parent was supplied) → else cache → else a repository GET. The overload without `parentResource` throws on contained references by design.

`FhirUri`/`FhirUriFactory` is the hand-rolled parser behind this. It handles relative and absolute references, references pointing at a *different* server (`PrimaryServiceRootRemote`, discovered by scanning path segments for a known resource name), `urn:uuid:`/`urn:oid:`, contained `#`, canonical `|version`, `_history/{vid}`, `$operations` at base/type/instance scope, and compartments. It is intricate string-slicing code — change it only with concrete reference examples in hand.

### Auth & resilience

- `AuthenticationDelegatingHandler` applies, per request, whichever of OAuth2 / basic / bearer / `x-api-key` the repository settings enable (they are independent flags, and multiple can fire — later ones overwrite `Authorization`). For OAuth2 it fetches a token when none is stored or `ApiToken.WillExpireSoon()` (5-minute threshold), and retries once after a `401`/`403` with a freshly minted token.
- `ApiTokenStore` is a **singleton** cache of `ApiToken` keyed by repository `Code`, guarded by a `Semaphore` on write specifically because concurrent first-use of the same repository was throwing duplicate-key exceptions.
- `OAuthTokenApi` (via `HttpClientBase`) does the `client_credentials` POST and returns `Result<ApiToken>`.
- `RetryDelegatingHandler` retries up to 10 times on `408`/`429`/`500`/`503`/`504` and on `HttpRequestException`/`TimeoutException`, honouring `Retry-After` when present, otherwise using jittered backoff from `IJitter`. Note there are effectively **two independent retry mechanisms** — this handler on the FHIR pipeline, and `HttpClientBase.RetryEnabledSendAsync` on the token path.

### Result type

`Infrastructure/Result.cs` provides `Result` / `Result<T>` with `Success`/`Failure`/`Retryable`/`ErrorMessage`. It is used **only** on the OAuth token path; the FHIR API path throws instead. Follow whichever convention the surrounding layer already uses.

## Configuration

Bound from the `FhirNavigator` section (`FhirNavigatorSettings.SectionName`); repositories are the nested `FhirRepositories` array, plus an optional `Proxy` block. `FhirRepositorySettings.SectionName` (`"FhirNavigatorRepositories"`) is vestigial and not used by the current binding path.

`Code`, `DisplayName`, `ServiceBaseUrl`, `UseOAuth2`, `UseBasicAuth` and `UseBearerToken` are declared `required` on the settings class but are satisfied by `IConfiguration` binding, so omitting the boolean flags in JSON is fine (they default to `false`). `ServiceBaseUrl` must not have a trailing `/`.

`ConsoleApp` reads config in `Program.cs`, binds `FhirNavigatorSettings` manually, and passes the values through the `AddFhirNavigator` action. Note that `ConsoleApp.csproj` copies **only** `appsettings.json` to the output directory — `appsettings.Development.json` is not copied, so settings placed there won't be picked up by a normal `dotnet run` unless that is fixed. Both appsettings files are tracked in git; keep real client secrets, passwords and bearer tokens out of them and use the project's User Secrets (`UserSecretsId` is already configured) instead.

## Conventions

- Named arguments are used liberally at call sites (`repositoryCode: …, resource: …`) — match that style.
- Primary constructors with injected dependencies are the norm for services.
- `using Task = System.Threading.Tasks.Task;` is required in files that reference both `System.Threading.Tasks.Task` and `Hl7.Fhir.Model.Task` — the FHIR `Task` resource collides with the CLR type.
- Structured logging with named placeholders throughout; API classes log the equivalent FHIR request line (e.g. `GET [base]/Patient/123`) for diagnosis.
- Branch `development` is the working branch; `release` is the main/PR target branch.
