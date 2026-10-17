# Plugin Refactoring Regression Baseline

[English](README.md) | [简体中文](README.zh-Hans.md)

This project verifies observable plugin composition behavior before refactoring. It references the Plugins project and the local Core assembly through `HintPath`; it has no Web dependency. Core runtime packages `Microsoft.Extensions.ObjectPool` and `Microsoft.Extensions.Caching.Memory` are declared explicitly because assembly references do not carry NuGet dependencies. Web bridge and host construction tests belong to [Plugins.Web tests](../../Zongsoft.Plugins.Web/test/README.md). Tests follow the [framework instructions](../../AGENTS.md) and [Zongsoft guidelines](../../../guidelines/zongsoft.csharp.guidelines.md). They do not widen production visibility or assert localized framework messages.

## Research Basis

The 2026-10-11 review covered Plugins, Plugins.Web, Core service/configuration/command/application infrastructure, and Web controller discovery and activation. The manifest survey read 595 `.plugin` files under `D:\Zongsoft` (including this framework) and `D:\Automao\server`, with 234 distinct SHA-256 contents. All parsed as XML. These counts include existing source, build, and deployment copies; they do not represent 595 independent plugins or successful XSD validation.

Minimal fixtures reproduce manifest patterns found in that corpus: dependencies, directory hierarchy, hidden manifests, overrides at the same path, object/expose builders, explicit constructor arguments, property behaviors, member extensions, relative member paths, static fields and nested types, options, and module services. Business hosts were not started. Tests need no database, broker, credentials, business deployment, or access to the surveyed directories.

The shared fixture loads the shipped registrations from [Main.plugin](../plugins/Main.plugin), removing its extension nodes. A separate test loads the complete Main and [Terminal.plugin](../plugins/Terminal.plugin) manifests without constructing their components.

## Running

Run from the framework root with the .NET 10 SDK and .NET 8, 9, and 10 runtimes. The project uses central package versions and VSTest with xUnit v3.

On a clean checkout, build these projects in order first. Production Debug projects use `HintPath` references to adjacent build outputs. This baseline tests current Debug source; production Release configurations reference published packages and are not equivalent source validation.

```powershell
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins/Zongsoft.Plugins.slnx -p:GeneratePackageOnBuild=false

dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net8.0 --no-build --no-restore
dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net9.0 --no-build --no-restore
dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net10.0 --no-build --no-restore
```

Host subprocess tests carry `Category=Integration` and remain entirely local. Add `--filter 'Category!=Integration'` for unit tests or `--filter 'Category=Integration'` for integration tests. Subprocesses invoke the current target framework's test assembly with `dotnet` from PATH. They open no interactive terminal or HTTP listener. A 30-second timeout terminates only the test's own process.

Read-only style checks:

```powershell
$testFrameworkBeforeFormat = $env:TargetFramework
try {
	$env:TargetFramework = 'net10.0'
	dotnet format style ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj --no-restore --verify-no-changes --diagnostics IDE0049
}
finally {
	$env:TargetFramework = $testFrameworkBeforeFormat
}
git -c core.whitespace=cr-at-eol diff --check -- Zongsoft.Plugins
```

Builds enable `ZongsoftCodeStyleStrict` by default. Formatting temporarily selects one framework to avoid workspace warnings about a missing Compile target in outer cross-targeting projects with the current SDK. Builds and tests still cover all three frameworks. New source uses tabs, CRLF, and UTF-8 without BOM; existing license headers are preserved. The temporary Git option recognizes CRLF during whitespace checks without changing repository configuration.

## Coverage Map

| Tests | Contracts |
| --- | --- |
| [PluginLoaderTests](PluginLoaderTests.cs) | Events, idempotence, dependency ordering, relationships, directory/hidden plugins, duplicates, optional assemblies, invalid declarations, overlays, shipped manifests |
| [PluginSchemaTests](PluginSchemaTests.cs) | Shipped XSD and manifests, plugins without extensions, optional unique manifest/builders/parsers sections, nonempty builder/parser registration sections, manifest → builders → parsers → extension order, 1,365 top-level section combinations, nested object/expose, dynamic attributes, unnamed typed and text parameters, custom schemas, warning boundaries and invalid known structures |
| [PluginTreeTests](PluginTreeTests.cs) | Paths, casing, sibling order, mount events, unmount behavior, read-only collections, invalid inputs |
| [ObjectBuilderTests](ObjectBuilderTests.cs) | Context/assembly/resource/module injection, constructors, property assignment, inferred types, caching, concurrency, failure recovery, disposal |
| [BuilderExtensionTests](BuilderExtensionTests.cs), [ExposeBuilderTests](ExposeBuilderTests.cs), [BuildPipelineTests](BuildPipelineTests.cs) | Owner appenders, dynamic Build decisions retained across cache reuse, collection conversion/expansion, expose suppression and child reception, property back-references, sibling deduplication, deferred module assembly, cycles/failures and settings reuse |
| [BuilderLifecycleTests](BuilderLifecycleTests.cs) | Optional lifecycle interface, direct implementations, callback order, property behaviors, cached/deferred/module construction, explicit reconstruction, null results and callback failures |
| [ParserTests](ParserTests.cs), [PredicateParserTests](PredicateParserTests.cs), [ResourceParserTests](ResourceParserTests.cs) | Path/type/static/object/service/option/predicate/resource parsing, aliases, asynchronous predication, invalid input, resources containing only test-owned data |
| [CommandTests](CommandTests.cs) | List/Tree/Find, explicit construction modes, deferred commands, parameters and cancellation, invalid arguments |
| [PluginConfigurationTests](PluginConfigurationTests.cs), [HostingConfigurationTests](HostingConfigurationTests.cs) | Exact plugin entry selection by manifest basename then logical name, no suffix scanning or fallback on parse failure, explicit imports in document order, one provider per plugin entry, host exact-name loading with physical/nonphysical file providers, descendant reloads, repeated notifications, entry provenance, failure/retry and subscription disposal |
| [PluginModelTests](PluginModelTests.cs) | Options identity, property cache/conversion, behaviors, foundation inheritance/overrides and dependencies |
| [PluginUnloadTests](PluginUnloadTests.cs) | Cancellation, resource disposal, removal of nodes/configuration, unloaded entries and their imports do not transfer to prefix-named plugins, unused foundations remain uninstantiated |
| [ApplicationLifecycleTests](ApplicationLifecycleTests.cs), [TestProgram](TestProgram.cs) | Initialization, workbench states/events/retry, startup worker selection, process exit, Daemon/Terminal construction, one assembly registration across host/plugin manifests with complete descendant configuration |

`PluginTestScope` creates a unique temporary directory per test and disposes configuration providers and service containers. Assembly-level parallelism is disabled because ApplicationContext, BuilderManager, and plugin tree caches contain process-wide state. The concurrency test still makes concurrent calls explicitly. Global test event handlers are removed. PluginTree has no public disposal API; tests do not clear its private static cache, which lives until the test process exits.

Unloading is currently internal. One adapter in `PluginUnloadTests` invokes that entry through reflection, then asserts observable results. No production visibility or private fields are changed. Adjust only this adapter if the entry changes during refactoring.

## Coverage Snapshot

On 2026-10-11, all 151 cases passed on each target framework, with no skips. The net10.0 Coverlet report includes generated resource accessors and unused classes; no classes were removed to improve the figures.

| Assembly | Lines | Branches |
| --- | ---: | ---: |
| Zongsoft.Plugins | 79.67% | 70.47% |

Selected Plugins line coverage: PluginResolver 92.35%, PluginLoader 84.40%, BuilderManager 93.90%, ObjectBuilder 83.76%, PluginConfigurationProvider 98.43%, PluginApplicationContext 95.65%, WorkbenchBase 98.80%. Execution coverage is not a proof of correctness or refactoring safety.

Regenerate the report:

```powershell
dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net10.0 --no-build --no-restore --collect "XPlat Code Coverage" --settings ./Zongsoft.Plugins/test/coverage.runsettings --results-directory ./Zongsoft.Plugins/test/TestResults
```

[coverage.runsettings](coverage.runsettings) selects only Zongsoft.Plugins. Reports are written under Git-ignored `TestResults`. Related Core code is exercised but excluded from these assembly totals.

## Refactoring Follow-ups

Tests record the current composition contracts, including descendant configuration reloads, repeatable notifications and assembly registration deduplication. The following table distinguishes retained behavior from source-review risks requiring separate regression work.

| Issue | Evidence and next validation |
| --- | --- |
| Cached expose child appending | The manager checks the decision saved during Build before invoking the owner's IAppender. Neither initial nor cached traversal appends expose itself; its children still populate the exposed collection. BuildPipelineTests verifies dynamic conditions and property back-references; BuilderLifecycleTests verifies deferred completion order. See [Architecture and implementation](../implementation.md). |
| Exit and cleanup | Workbench.Close calls ApplicationContext.Exit. PluginApplicationContext.OnStopping closes the workbench before calling the base implementation that stops workers. Add subprocess traces for worker start/stop/disposal and verify cleanup before exit. |
| Dependency cycles | PluginLoader recursively advances dependencies without cycle detection. Validate self-dependencies and A→B→A in isolated, bounded processes before adding diagnostics; this suite does not trigger a possible stack overflow. |
| Nested unloading | PluginLoader removes children while iterating their collection; PluginTree.UnmountItem can likewise remove nodes while iterating. Existing disposal tests cover leaf components. Add nested plugins/components and cancelled child unload cases. |
| Multiple parent segments | PluginTreeNode.Find resolves `..` through the original node's parent. Add expected results for `../..` and `Child/..`. |
| Equality/hash agreement | Builtin equality ignores case but hashing does not. Empty tree-node equality omits tree identity while hashing includes it. Define identity before testing hash collections. |
| Expression/resource arguments | ParserExpression includes the opening brace and colon in parsed values; Parser.Parse uses another implementation. ResourceParser copies CaptureCollection directly into string[]. Add valid expression and formatted-resource regressions; current cases cover malformed expressions and argument-free resource lookup. |

PluginSchemaTests copies the shipped XSD into the fixtures and enables `ReportValidationWarnings` to detect missing standard declarations as well as errors. Positive cases also exercise loading and construction where appropriate. Dynamic member names remain permitted by `lax`; their missing-schema warnings are tested separately from structural errors. Custom schemas can declare a builder element with `BuiltinElementType`. Known malformed descendants must still fail validation. These tests do not access the external manifest survey directories.

Documentation differences remain: Plugin.GetParser's comments describe recursive dependency lookup, but foundations are found through direct dependencies, slaves, and parents; transitive dependencies alone do not expose Main registrations. Tests preserve runtime compatibility while this contract awaits reconciliation.

Real HTTP requests, interactive terminals, business startup, assembly replacement/unloading, operating-system file notifications, and every failure rollback combination are outside this baseline. Daemon lifecycle success does not establish those behaviors.
