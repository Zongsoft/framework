# Application manifest

[English](application-manifest.md) | [简体中文](application-manifest.zh-Hans.md)

[`Zongsoft.Services.ApplicationManifest`](../src/Services/ApplicationManifest.cs) reads and writes application names, versions, named editions and the current edition in an application directory's `.edition` file.

## Model and file format

`Name` is the application name. A manifest contains either a top-level `Version` or a nonempty, ordered `Editions` collection; these representations are mutually exclusive. `Version` is `null` when editions are present. An empty model is allowed while editing, but saving requires a version or at least one edition.

The root entry selects the representation and optionally the current edition:

| Root entry | Meaning |
| --- | --- |
| `MyApplicationName@1.0.1` | A single version; `Editions` is empty. |
| `MyApplicationName` | Named editions without a current selection. |
| `MyApplicationName=` | The same unset selection; saving omits the equals sign. |
| `MyApplicationName=Professional` | Named editions with `Professional` selected. |

A manifest with named editions looks like this:

```ini
MyApplicationName=Professional

[Community]
1.0.1

[Professional]
1.1.0

[Enterprise]
1.1.2
```

Versions use the two-, three- or four-part numeric format supported by `System.Version`. Each edition section contains exactly one bare version number; `Version=1.0.1` entries, nested sections and inline comments are not supported. Edition names are unique and matched without regard to case. A specified current edition must exist; an invalid reference throws `FormatException` at the root entry.

Loading reuses [Profile parsing](profiles.md#loading-and-imports), including whitespace, blank lines, whole-line comments and BOM handling. Edition names follow [Profile section name rules](profiles.md#section-names), with spaces and tabs denoting hierarchy; manifests reject nested sections. Application and edition constructors require nonblank names and trim surrounding whitespace without additional reserved-character validation. Callers choose names that the Profile format can represent; `@` and `=` are separators in the application header.

Manifests use `ImportBehavior = ProfileDirectiveBehavior.Suppressed`. Both `#@import` and `;@import` directives, including empty arguments, throw `FormatException` before any imported file is opened.

## Editions and current selection

`ApplicationManifest.EditionCollection` derives from `KeyedCollection<string, ApplicationManifest.Edition>` and uses `OrdinalIgnoreCase` keys. It preserves collection order and supports insertion, replacement, lookup and removal through the base collection API. `IsEmpty` reports whether the collection has no items. `Add(string name, Version version)` returns the added edition.

`Editions.Current` has type `ApplicationManifest.Edition`:

- `default(ApplicationManifest.Edition)` means no selection; no edition is selected automatically.
- Assign a string to select an existing edition by name. Surrounding whitespace is trimmed and matching ignores case; the getter returns the stored edition, including its version.
- Assigning a complete `Edition` requires both its name and version to match. An invalid selection throws `ArgumentException` with parameter `value` and leaves the previous selection unchanged.
- `Current = default`, `Current = null` and `Current = ""` clear the selection. Whitespace-only names are invalid. String conversion creates a name-only reference that cannot be added as a collection item.
- Removing the current item or clearing the collection clears the selection. Replacing it with the same name follows the new version; replacing it with another name clears the selection. Failed modifications retain the collection and selection.

`Edition` equality, hash codes and equality operators compare names without regard to case and versions by value. The manifest and its collection are not thread safe.

```csharp
using System;
using Zongsoft.Services;

var manifest = new ApplicationManifest("MyApplicationName");
manifest.Editions.Add("Community", new Version(1, 0, 1));
manifest.Editions.Add("Professional", new Version(1, 1, 0));
manifest.Editions.Current = "Professional";
manifest.Save(AppContext.BaseDirectory);
```

## Loading and saving

`Load` and `Save` accept a directory, an explicit file path, a stream or a text reader/writer. Null or empty paths use `AppContext.BaseDirectory`; existing directories use the `.edition` file within them.

`Load(string path)` returns `null` when the file is missing. `Save(string path)` can create `.edition` within an existing directory; an explicit path that is neither an existing directory nor an existing file does not write anything. Parent directories are not created.

Saving to an existing file first reads that destination's blank lines and comments. Empty or new destinations, streams and text outputs use the loaded layout. Saving preserves blank lines and comment content by default, writes editions in collection order, omits an unset root entry value and uses the selected section's actual name. Comment markers are normalized to `#` by Profile. Notes and blank lines remain after sections are added, removed or reordered, though their positions may change; newly added sections are separated by a blank line.

File and stream output uses UTF-8 without BOM and the platform default newline. Text writers use their configured `NewLine` and encoding. Saving validates version completeness and Profile output syntax before writing; output ends with a newline.

Caller-provided streams and text readers/writers remain open on success or failure. Loading reads from the current position to the end; saving writes at the current position without resetting or truncating a stream. Streams need not support seeking. See the [regression tests](../test/Services/ApplicationManifestTest.cs) for format, collection and layout preservation coverage.

This API provides the manifest model and its file format. Host application contexts and upgrading components still use `ApplicationIdentifier` and do not automatically consume `.edition`.
