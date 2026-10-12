# XML configuration and processing instructions

[English](xml-configuration.md) | [简体中文](xml-configuration.zh-Hans.md)

## Reading configuration and instructions

`Zongsoft.Configuration.Xml` reads XML configuration with a `configuration` or `options` root and `option path="..."` children. Ordinary attributes become configuration keys; `element.name` / `element.key` select named collection entries. Text and CDATA retain the existing scalar-collection mapping.

Processing instructions (PIs) are collected separately from configuration keys. Reading does not execute imports, change configuration, or extract bindings. File and stream providers collect all PIs by default.

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

`XmlStreamConfigurationProvider` also exposes `Instructions`. Each collection belongs to its provider's current data and preserves document order and repeated declarations. This layer does not merge instructions across files, remove duplicates, or establish precedence.

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

PIs are collected before the root, inside any element, and after the root. Outside-root paths are empty. Repeated sibling elements may share a path; line and column distinguish their locations. XML declarations and PI-like text inside comments or CDATA are excluded.

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

The existing `Read(Stream)` remains available. The new overload's third parameter, `filePath`, supplies an explicit source identifier. `null` infers `FileStream.Name`, or an empty string for other streams. An explicit empty string denotes anonymous input. This parameter neither opens a file nor establishes a base directory for configuration values.

Builders support `AddOptionStream(stream, filePath)`. Direct source construction exposes `XmlStreamConfigurationSource.FilePath`. File providers record the physical file stream path, or the source `Path` for virtual inputs. Direct reads and stream providers leave caller-owned streams open. File providers retain their base class's stream lifecycle.

Each successful complete read replaces configuration data and its instructions together. Previously captured instruction collections and objects remain unchanged. Missing-file and error behavior follows `FileConfigurationProvider`: when the base clears data, instructions are empty; when it preserves old data, their instructions remain. Failed reads never publish partial instructions. Stream providers retain the base class restriction that parameterless `Load()` can run only once.

## Validation

Existing mapping regressions are in [OptionConfigurationTest.cs](../test/Configuration/Xml/OptionConfigurationTest.cs). PI contract tests in the same directory cover lexical rules, positions, preserved data, optional attributes, stream ownership, and file reloads.

This layer does not register handlers, execute instructions, import files recursively, or define `binding` semantics. Subsequent implementations can consume the definitions and establish their own semantics.
