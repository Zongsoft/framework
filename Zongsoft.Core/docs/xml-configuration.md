# XML configuration and processing instructions

[English](xml-configuration.md) | [简体中文](xml-configuration.zh-Hans.md)

## Reading configuration and instructions

`Zongsoft.Configuration.Xml` reads XML configuration with a `configuration` or `options` root and `option path="..."` children. Ordinary attributes become configuration keys; `element.name` / `element.key` select named collection entries. Text and CDATA retain the existing scalar-collection mapping.

Processing instructions (PIs) are collected as original definitions rather than mapped to configuration keys. File and stream providers execute registered instructions while loading, with `import` registered by default. Unknown instructions retain their definitions for consumers to interpret. Static `XmlStreamConfigurationProvider.Read` only parses the current stream and does not execute instructions.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<?binding path="Kestrel/Endpoints" url="Url" ?>
<configuration>
	<option path="/Kestrel">
		<endpoints>
			<endpoint endpoint.name="http" url="http://*:8069">
				<?example note="application endpoint" ?>
			</endpoint>
		</endpoints>
	</option>
</configuration>
```

Here `binding` is only a collected declaration. The reader does not interpret `path` or `url`, or generate `.bindings`.

The following example requires an application-provided `app.option` and demonstrates a single file provider:

```csharp
using System.Linq;
using Microsoft.Extensions.Configuration;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Xml;

using var configuration = (ConfigurationRoot)new ConfigurationBuilder()
	.AddOptionFile("app.option")
	.Build();

var provider = configuration.Providers.OfType<XmlConfigurationProvider>().Single();
var instructions = provider.Instructions;

foreach(var instruction in instructions)
{
	if(instruction.TryGetAttributes(out var attributes) &&
		attributes.TryGetValue("path", out var path))
	{
		//The caller decides how to use the parameter text in path.
	}
}
```

`XmlStreamConfigurationProvider` also exposes `Instructions`. Each collection belongs to its provider's current data and expands imports in depth-first order: the original `import`, effective instructions from its child file, then the remainder of the current file. Repeated declarations and original source information are retained. Instructions from separate providers are not automatically merged or deduplicated. Consumers needing final configuration values should finish building configuration before resolving these declarations against the final configuration.

## Importing configuration

```xml
<?import path="defaults.option"?>
<configuration>
	<option path="/Service">
		<?import path="shared.option" required="false"?>
		<client timeout="30" />
	</option>
</configuration>
<?import path="overrides.option"?>
```

`import` is allowed only before the root element, after the root element, or as a direct child of `option`. An import outside the root reads all options in its target. An import inside an `option` reads only target options with exactly the same `option path`, merging them at the instruction's position. Imports directly under `configuration`, or within grandchildren and deeper descendants of `option`, are invalid.

Path matching trims leading and trailing `/`, spaces, and Tabs, replaces `/` with configuration separator `:`, and compares using `OrdinalIgnoreCase`. Missing, empty, and `/` paths denote the configuration root. `/Service` therefore matches `service/`, but does not match a separately declared `/Service/Client` option. All matching option blocks are processed in order. No match is a successful empty result, regardless of `required`.

During a selected import, imports outside the target root propagate the same selection. Only selected options and file-level instructions are processed and collected. Instructions in other options do not execute, and their targets are neither opened nor watched. The entire target must still be well-formed XML.

Each file builds its own configuration dictionary and merges it into its caller only after success. Later imports or local values override earlier cross-file results, allowing local settings to override imported defaults. Duplicate keys produced by ordinary XML declarations in the same file remain errors, even when an import occurs between them. Processor and callback writes do not count as ordinary declarations.

`path` is a required, nonempty attribute. Each instruction imports one file, without wildcard or variable expansion. `required` defaults to `true` and accepts boolean values. `false` ignores only a missing file or directory when opening the target. Access errors, other IO failures, XML errors, cycles, and depth violations still fail loading.

Physical relative paths resolve against the declaring file's actual directory; absolute paths retain their meaning. A `FileStream` provides the actual base even when its diagnostic source identifier is overridden. An `IFileProvider` without a physical file path uses relative logical paths within the same provider, normalizes `.` and `..`, and rejects traversal above its root or fallback to disk. An ordinary memory stream can import an absolute physical path. Relative imports fail without a base, even with `required="false"` or a file-like `FilePath` identifier.

Cycle detection applies only to the active import chain, resolving physical file and directory links for identity. Repeated sequential imports and diamond dependencies reread their targets. The default maximum is 64 simultaneous loading levels, counting the root as level one. `MaximumDepth` must be positive.

## Options and callbacks

Both file and stream sources expose a read-only `Options` property:

```csharp
using var configuration = (ConfigurationRoot)new ConfigurationBuilder()
	.AddOptionFile(source =>
	{
		source.Path = "app.option";
		source.ReloadOnChange = true;
		source.Options.Instructions.Add(XmlConfigurationInstructionOptions.Import(
			XmlConfigurationInstructionBehavior.Strict, maximumDepth: 16));
		source.Options.Instructions.Processing = context =>
		{
			//Rewrite context.Data for this execution, or set Handled to take over.
		};
	})
	.Build();
```

`XmlConfigurationInstructionOptions` selects behavior by instruction name. Names are case-insensitive; null items and duplicate names are rejected. `XmlConfigurationInstructionOptions.Import()` creates `ImportOptions` with `MaximumDepth`. The options collection configures behavior and does not register processors.

| Behavior | Execution rule |
| --- | --- |
| `None` | Use the instruction's default rules. Import follows `required`. Unknown instructions still receive both callbacks and retain `Handled=false` unless intercepted. |
| `Strict` | Import requires its target and ignores the value of `required`. An unknown instruction must be handled by the preceding callback or loading fails. |
| `Ignore` | Retain only the original definition. Skip location and argument validation, processors, instruction callbacks, and target reads. |
| `Suppress` | Fail when encountered, before parsing arguments or invoking instruction callbacks. |

`Loading` and `Loaded` are `Action<XmlConfigurationContext>` callbacks for the root and each imported file actually read. `Loading` runs after opening and cycle/depth validation, with an empty configuration dictionary. `Loaded` runs after parsing and recursive imports, before merging into the caller; its dictionary changes participate in that merge. Missing or rejected files do not receive file callbacks.

`Instructions.Processing` and `Instructions.Processed` are `Action<XmlConfigurationInstructionContext>` callbacks. Execution proceeds through the preceding callback, processor, and following callback. Setting `Handled=true` skips the processor; a successfully returning processor is marked handled by the dispatcher. Import's following callback runs after recursive reading and merging. Processor or callback exceptions terminate loading; a failed instruction does not receive its following callback. Rewriting data in the following callback does not execute the instruction again.

| Context member | Meaning |
| --- | --- |
| `FilePath` / `Depth` / `Referer` | Current source, loading depth, and original import that initiated this file read. The root's `Referer` is `null`. |
| `Configuration` | Mutable values read so far from the current file, including completed earlier imports, excluding the caller's values. |
| `Instruction` | Immutable original PI, including name, data, and source position. |
| `Data` | Mutable execution data, without whitespace trimming; changes leave the original definition intact. |
| `GetAttributes()` / `TryGetAttributes(...)` | Parse current execution data on demand; changing `Data` invalidates parsed attributes. |
| `OptionPath` | Normalized `:` path of the containing option; empty for the configuration root and `null` for file-level instructions. |
| `Handled` | Whether a callback has taken over or a processor has completed. |
| `Behavior` | Behavior fixed before the preceding callback. |
| `Options` | Independent options copy for this instruction. Specific settings may be adjusted, but changes cannot alter the fixed `Behavior`. |

Each root load snapshots the global registry and source options. Recursive imports share that snapshot; reloads take a fresh snapshot. The virtual options `Clone()` preserves derived types. Derived options with mutable reference members should override it and copy those members. Callers manage external state captured by callback delegates.

## Registering processors

`XmlConfiguration.Instructions` is a thread-safe `XmlConfigurationInstructionProcessorCollection`, initially containing `Instructions.ImportInstructionProcessor.Instance`. `Add` rejects null instances and duplicate names. Lookup uses `OrdinalIgnoreCase`; enumeration uses snapshots. Removal and replacement are not provided. Registered instances are shared, and execution does not hold the registry lock.

Custom processors derive from `XmlConfigurationInstructionProcessor`, supply a valid XML instruction name to the constructor, and implement `Process(XmlConfigurationInstructionContext)`. The name cannot be `xml` and excludes `<?` and `?>`. Multiple loads can call the same instance concurrently, so invocation-specific mutable state belongs in the context or local variables.

```csharp
XmlConfiguration.Instructions.Add(new FeatureInstructionProcessor());

sealed class FeatureInstructionProcessor() : XmlConfigurationInstructionProcessor("feature")
{
	public override void Process(XmlConfigurationInstructionContext context)
	{
		context.Configuration["Features:Example"] = context.Data;
	}
}
```

This example consumes free-form data directly; attribute parsing is optional. Import is the only built-in instruction. Core does not define `set` or `binding` semantics.

## Instruction definitions

`XmlConfigurationInstruction` is a read-only object created by the reader.

| Property | Meaning |
| --- | --- |
| `Name` | PI name with original casing, excluding `<?` and `?>`. |
| `Data` | Text supplied by `XmlReader.Value`, without additional trimming or decoding. |
| `IsEmpty` | Whether `Data` is `null` or an empty string, without trimming whitespace or parsing attributes. |
| `Path` | Physical XML parent-element path, starting with `/`, preserving qualified names and casing, without indexes or predicates. |
| `FilePath` | Source file path or caller-supplied source identifier; empty for anonymous input. |
| `LineNumber` / `LinePosition` | One-based position of the PI name; a line starting with `<?example?>` reports column 3. |

In the example, `binding.Path` is empty and `example.Path` is `/configuration/option/endpoints/endpoint`, rather than the mapped configuration path `/Kestrel/endpoints/http`. A `path="..."` parameter in `Data` is independent of the object's `Path` property.

PIs can occur before the root, inside any element, and after the root; individual processors may restrict placement. Pure parsing collects every PI in the current stream, while selected imports collect only their effective scopes. Outside-root paths are empty. Repeated sibling elements may share a path; line and column distinguish their locations. XML declarations and PI-like text inside comments or CDATA are excluded.

`Data` is not a byte slice of the original file. The XML reader removes separator whitespace between the name and data and applies XML end-of-line normalization. Trailing data whitespace and entity spelling are retained. For example, `<?example value="a&amp;b" ?>` supplies `value="a&amp;b" `.

Both `<?example?>` and `<?example   ?>` have `IsEmpty` equal to `true`. For `<?example hello   ?>`, it is `false`, and `Data` retains the three spaces after `hello`.

## Optional attribute parsing

`GetAttributes()` returns `IReadOnlyDictionary<string, string>`. Invalid syntax throws `FormatException` with the instruction's source position. `TryGetAttributes(out ...)` returns `false` and a `null` output for the same errors, never partial results. Other exceptions are not converted to parse failures.

- Names use `OrdinalIgnoreCase` comparison and retain their spelling. `Path="a" path="b"` is a duplicate and fails.
- Values require single or double quotes. XML whitespace is allowed between attributes and around equals signs.
- Empty data or XML-whitespace-only data produces an empty collection; `value=""` produces an empty string value.
- XML entities and character references are decoded once, with XML attribute-value whitespace normalization. `a&amp;b` becomes `a&b`; `&amp;amp;` becomes `&amp;`.
- Backslashes are literal, without C# escape semantics. Values are not converted to numbers, booleans, settings, or paths.
- The entire input must be attributes. Unquoted values, duplicates, extra elements, comments, and trailing text fail.
- Attributes are parsed as those of a standalone XML element, without the parent's namespace or DTD context. Required namespace declarations must appear in `Data`. DTDs and external entities are not loaded.

Successful attribute collections are read-only and cached per instruction for concurrent reads. Failure leaves `Data` and the loaded configuration unchanged. Instructions can use other data syntaxes:

```xml
<?custom some free-form text ?>
```

This PI is collected normally. Its consumer can interpret `Data` directly; attribute parsing is optional.

## Streams, sources, and snapshots

Read configuration and instructions directly from an existing stream:

```csharp
using System.IO;
using Zongsoft.Configuration.Xml;

using var stream = File.OpenRead("app.option");
var values = XmlStreamConfigurationProvider.Read(stream, out var instructions);
```

`Read(Stream)` and `Read(Stream, out instructions, filePath)` only parse the current stream. They do not use the registry, options, or callbacks, and do not follow Import. `filePath` supplies an explicit source identifier. `null` infers `FileStream.Name`, or an empty string for other streams. An explicit empty string denotes anonymous input. The identifier neither opens a file nor establishes a base for relative imports.

Builders support `AddOptionStream(stream, filePath)` and `AddOptionStream(source => { ... })` for configuring `Stream`, `FilePath`, and `Options`. These providers execute instructions. File providers record the physical stream path or the logical path for virtual inputs. Direct reads and stream providers leave caller-owned streams open. File providers retain their base class's stream lifecycle; the loader closes streams it opens for imports.

Each successful complete read replaces configuration data and its instructions together. Previously captured instruction collections and objects remain unchanged. Missing-file and error behavior follows `FileConfigurationProvider`: when the base clears data, instructions are empty; when it preserves old data, their instructions remain. Failed reads never publish partial instructions. Stream providers retain the base class restriction that parameterless `Load()` can run only once.

## File changes and validation

With `ReloadOnChange`, changes to the root or effective import dependencies reload the entire root configuration. Optional missing targets and read targets without matching options are watched. Targets in skipped options and ignored imports are not watched. Each load refreshes dependencies, removes obsolete subscriptions, and disposal releases all subscriptions without disposing the caller's `IFileProvider`. Notifications depend on file-provider support. Stream sources do not automatically watch files.

Failed reloads follow `FileConfigurationProvider` clearing and exception handling, including `OnLoadException`, without restoring the last successful result. Dependencies already discovered remain watched so later changes can trigger another load. Reading multiple files does not provide a filesystem transaction snapshot.

Mapping regressions are in [OptionConfigurationTest.cs](../test/Configuration/Xml/OptionConfigurationTest.cs). PI, [processor](../test/Configuration/Xml/XmlConfigurationProcessorTest.cs), [import](../test/Configuration/Xml/XmlConfigurationImportTest.cs), and [reload](../test/Configuration/Xml/XmlConfigurationImportReloadTest.cs) tests in the same directory cover lexical rules, original definitions, callback snapshots, path selection, precedence, errors, stream ownership, cycles, and subscription disposal.
