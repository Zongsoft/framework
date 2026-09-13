# Local file searching

[English](searcher.md) | [简体中文](searcher.zh-Hans.md)

`Zongsoft.IO.Searcher` extends `System.IO.DirectoryInfo` with a single `Search` method. It operates on the local filesystem without using `IFileSystem`, `IDirectory`, or the existing regular-expression `Pattern` type. No extra package is required.

## Usage and results

`Searcher.Target` selects `Files`, `Directories`, or `Both` (the default). `Both = 0`, so the default enum value searches both types. The caller is responsible for passing a valid target; Search does not validate it. To search both types with cancellation, use `directory.Search(pattern, cancellation: cancellation)`.

```csharp
var directory = new System.IO.DirectoryInfo(source);
foreach(var match in Zongsoft.IO.Searcher.Search(directory, "plugins/**/*.json", Zongsoft.IO.Searcher.Target.Files, cancellation))
{
	var name = System.IO.Path.GetRelativePath(match.Origin.FullName, match.Path);
	if(match.IsFile(out var file))
		System.Console.WriteLine($"{name}: {file.FullName}");
}
```

`Match.Path` is the absolute logical path, preserving link names. `Match.ToString()` returns this logical path, or an empty string for a default match; it performs no IO. `Result` is the actual file or directory after resolving links, including ancestor directory links. `Origin` is the logical fixed prefix preceding the first wildcard segment; an exact path uses its parent directory. `Captures` is a read-only list containing the complete match of each wildcard segment, in pattern order; `**` can capture an empty string. `IsFile(out FileInfo)` and `IsDirectory(out DirectoryInfo)` only inspect the result type, perform no IO, and return false/null for a default match.

For `plugins/*/assets/**/*.json` matching `plugins/orders/assets/config/site.json`, the base is `plugins`, and captures are `orders`, `config`, `site.json`. Use the logical Path for output names and Result for reading content.

The internal `Enumerate` method keeps `directory` (the caller-selected traversal start) separate from `origin` (the fixed prefix exposed as `Match.Origin`). Starting at the fixed prefix would bypass the rule against traversing directory links inside a pattern.

## Patterns and execution

- Patterns are nonempty relative paths anchored to the supplied directory. Absolute patterns and null inputs throw argument exceptions.
- `*` and `?` match within one segment; only a standalone `**` crosses zero or more ordinary directories. `ab**cd` remains a single-segment pattern.
- `assets/**` includes assets itself when directories are requested; `assets/**/*` only matches descendants. Exact directory paths do not implicitly enumerate their contents.
- Fixed prefixes allow `.` and `..`; a parent segment after a wildcard is rejected. The supplied directory is a resolution base, not a containment boundary.
- Windows matching and logical-path deduplication are ordinal and case-insensitive; other platforms are case-sensitive. There is no case option. Unix backslashes are not converted to separators.
- Each enumeration has independent traversal state and no global cache. Earlier `**` segments consume fewer levels when several matches are possible. Different logical aliases of the same physical target remain separate results.
- Results are collected and sorted by slash-normalized logical relative path using Ordinal order before they are returned. This is an enumerable, not an unbuffered traversal. Callers retain ordering across multiple input patterns.
- Validation and path capture happen at invocation; filesystem access happens during enumeration. Cancellation is observed during traversal and result delivery. Enumerators are disposed on every exit.
- Missing ordinary entries produce no match. Selected broken/cyclic links and access or other IO errors fail the search; they are not silently converted to empty results. Searching does not provide an input snapshot or prevent subsequent filesystem changes.

## Link contract

Matching uses the link name. A selected file link returns its target while retaining its logical name. A selected directory link can be returned, but searching never crosses a directory link occurring inside the pattern to match further segments. Even a fixed intermediate link cannot be hidden by starting traversal at the fixed prefix. An explicitly supplied DirectoryInfo search root may itself be a link.

Terminal `**` may select a directory link itself; it never descends into that link. Links which are unrelated or whose directories are skipped need not be resolved. Hard links are ordinary files and are not deduplicated by physical identity.

Packager and Deployer materialize ordinary files/directories under the selected logical names. When a directory itself is selected as a payload root, its target is expanded; nested ordinary directories recurse, nested directory links are skipped, and selected file links contribute target bytes under their logical names. They do not create symlinks in the output. Linked INI and `.deploy` files keep relative references anchored to the logical configuration location.

## Validation

See [implementation checklist](../LOCAL-SEARCHER-TASKS.md). Windows checks do not substitute for native Linux/macOS permissions, links, case behavior, or UNC/share validation where no test share is available.
