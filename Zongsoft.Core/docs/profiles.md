# Profile configuration: loading, directives and saving

[English](profiles.md) | [简体中文](profiles.zh-Hans.md)

Profile reads INI declarations, processes directives, merges imported sources and saves changes back to their declaring files.

- [Loading and imports](#loading-and-imports)
- [Declarations and saving](#declarations-and-saving)

## Loading and imports

`Profile.Load` accepts paths, Stream and TextReader. Each root load creates an internal ProfileReader that owns parsing, directive dispatch, file notifications and cycle/depth checks. Recursive imports share the reader and its captured settings. Profile retains declarations, effective references and source relationships. ProfileWriter serializes declarations and coordinates source commits; it does not execute directives or callbacks.

### Options and directive settings

```csharp
using Zongsoft.Configuration.Profiles;

var options = new ProfileOptions
{
	Directives =
	{
		ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Strict, maximumDepth: 16),
		new ProfileDirectiveOptions("custom", ProfileDirectiveBehavior.Suppress),
	},
	Loading = context => Console.WriteLine($"Reading {context.FilePath} at depth {context.Depth}"),
	Loaded = context => Console.WriteLine($"Read {context.Profile.FilePath}"),
};

var profile = Profile.Load("settings.ini", options);
```

`ProfileOptions(bool preserveBlanks = true)` exposes PreserveBlanks, a get-only Directives collection, Loading/Loaded (`Action<ProfileContext>`) and DirectiveProcessing/DirectiveProcessed (`Action<ProfileDirectiveContext>`). Callbacks default to null. Omitted load options discard blanks; explicit new ProfileOptions() records them. Profile does not expand variables.

`ProfileDirectiveOptions` has an immutable Name and a settable Behavior. Names start with a letter or underscore and contain only letters, digits, underscores, hyphens or dots; they contain no comment marker or `@` prefix. `ProfileDirectiveOptionsCollection` uses case-insensitive name keys, rejects null and duplicate items, and supports keyed lookup. Its enumeration order does not control execution order. Missing settings use the directive's built-in defaults; removing settings restores those defaults. Adding settings does not register an implementation.

`ProfileDirectiveOptions.Import(behavior = None, maximumDepth = 0)` constructs the public nested ImportOptions type, whose name is fixed to `import`. MaximumDepth is nonnegative: zero selects the built-in limit of 64; positive values specify the limit; negative assignments throw ArgumentOutOfRangeException without replacing the current value. The root counts as one active level: 1 allows only the root. The effective limit is resolved once for the load, without modifying the supplied options. Cycle detection remains enabled independently.

`ProfileDirectiveBehavior` is a general policy enum. None selects the directive's built-in behavior; Strict requests its strict rules; Ignore preserves the declaration as a comment without entering directive processing; Suppress throws ProfileException before directive callbacks or execution.

| Behavior | Import directive | Other directive without an implementation |
| --- | --- | --- |
| None | Load files; missing imported files/directories are allowed. | Offer the directive to callbacks and preserve an unhandled declaration as a comment. |
| Strict | Require every direct and recursive import to exist. | Throw unless the processing callback handles it. |
| Ignore | No directive callbacks or imported-file access. | No directive callbacks or execution. |
| Suppress | Reject the directive, including empty arguments. | Reject the directive, including empty arguments. |

These policies apply after the containing document's Loading notification. They do not suppress root file notifications. Names match without regard to case; invalid enum values are rejected when assigned.

At root entry, Reader copies the options collection and calls each directive option's virtual Clone method. Recursive reads share the resulting settings. The default Clone preserves the runtime type and shallow-copies fields; derived options with mutable reference members must override it to copy those members. Changing the original collection, option properties or callback properties affects subsequent root loads. Callers manage mutable state captured by callback delegates.

### File loading callbacks

Loading and Loaded run for roots and imports. Loading runs after opening and cycle/depth checks, before parsing. Loaded runs after successful parsing and recursive imports; an imported Profile has already been merged into its direct referer. Each notification receives a separate public sealed ProfileContext with get-only properties:

| Property | Meaning |
| --- | --- |
| FilePath | Absolute loading path, or an empty string for anonymous input. |
| Depth | Active loading depth: root 1, direct import 2. |
| Referer | Direct referring Profile; null for roots. |
| Profile | Null in Loading; the parsed Profile in Loaded. |

Missing optional imports and rejected files do not receive file notifications. Repeated successful reads notify separately. A failure in Loading, parsing or merging prevents that file's Loaded callback. Callback exceptions propagate, streams/activity are cleaned, and prior notifications or merges are not rolled back. Notifications run while the file is still in the active chain. Retained Loading contexts are not filled later; referenced Profile objects remain editable.

### Directive callbacks

A directive is a comment beginning immediately with `@name`, such as `#@import a.ini | b.ini` or `;@custom value`. A space or tab separates the name from its argument. Whitespace between the comment marker and `@` produces an ordinary comment. Original directive text remains a ProfileComment declaration.

DirectiveProcessing runs after the name and original argument have been identified, before execution. DirectiveProcessed runs after the whole directive succeeds, including its nested file reads and merges. A directive with several imports receives one pair of directive notifications and a pair of file notifications for each successfully loaded file:

```text
Loading(root)
  DirectiveProcessing(import)
    Loading(a.ini)
    Loaded(a.ini)
    Loading(b.ini)
    Loaded(b.ini)
  DirectiveProcessed(import)
Loaded(root)
```

Recursive directives nest inside their containing file's notifications. An empty import or an optional missing import still completes directive processing. Processing, execution or nested callback failure prevents DirectiveProcessed. Ignore and Suppress take effect before either directive callback.

Both callbacks receive the same public sealed ProfileDirectiveContext:

| Property | Meaning |
| --- | --- |
| Name | Directive name with its declared casing. |
| Argument | Settable directive argument with surrounding whitespace removed; assigning null preserves null. Each directive defines its meaning. |
| Handled | Set true in DirectiveProcessing to take over execution. Successful built-in handling also sets it true. |
| Behavior | The captured policy for this directive. |
| Options | An independent copy of this directive's settings, including derived properties; edits do not change Reader settings. |
| Profile / Section | Declaring Profile and current section; Section is null at the root section. |
| FilePath / LineNumber / Depth | Declaring source path, one-based line and active file depth. |

Argument and Handled changes made in DirectiveProcessing guide execution. Edits in DirectiveProcessed do not re-execute the directive. Arguments are never written back into the original declaration. For example, a caller can redirect an import and implement a custom directive:

```csharp
options.DirectiveProcessing = context =>
{
	if(context.Name.Equals("import", StringComparison.OrdinalIgnoreCase))
		context.Argument = "shared.ini";
	else if(context.Name.Equals("note", StringComparison.OrdinalIgnoreCase))
	{
		Console.WriteLine(context.Argument);
		context.Handled = true;
	}
};
```

Saving still writes the original import comment. An unhandled unknown directive under None completes with Handled=false; Strict rejects it. The extension context exposes no Reader, input stream or recursive loading entry point.

### Syntax, paths and recursion

Import arguments are separated by spaces, tabs or `|`. Null or empty arguments read no files. Paths support neither quoting, variable expansion nor globs. Relative paths use the declaring file's loading directory; absolute paths are allowed. Imports merge at the current Profile root even when declared inside a section.

[ApplicationManifest](application-manifest.md) configures `Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress) }` and rejects import directives with FormatException before opening imported files, including both comment markers and empty arguments.

Only repetition on the active chain is a cycle. Diamond and sequential repeated imports are read again. Identity checks resolve file and ancestor-directory links while retaining the original relative-path base. Windows ignores path case; other platforms use ordinal comparison. The depth limit also bounds unrecognized aliases such as hard links. Cycle/depth errors identify the import chain, declaring file and one-based line. With the default limit, a 65th active file fails before its Loading callback.

None ignores only file/directory-not-found errors during imported-file opening; permission, parsing and callback failures propagate. Strict reports a missing import with its path, referer, line and original IO exception. Root files are always required.

FileStream roots provide paths and participate in cycle checks. Anonymous inputs permit absolute imports and reject relative imports. Profile.Load(Stream) closes the supplied stream. Explicit encoding applies only to the root; imports use UTF-8 with BOM detection. Profile.Load(TextReader, ProfileOptions) reads from the current position and leaves the reader open on success or failure. A StreamReader over a FileStream provides its source path. A BOM at the start of the first line read is ignored; numbering starts at that position.

### Section names

ProfileSection trims surrounding whitespace and rejects empty names or `/`, `\`, `|`, `*`, `?`, `=`, `%`, `^`, `&`, `<`, `>`, `{`, `}`. Section lookup ignores case. Spaces and tabs in headers separate hierarchy levels: `[network proxy]` declares a child section. Model construction uses parent/child sections; saving rejects whitespace inside a single section name. Section names have no quoting or escaping.

### Merging, saving and consumers

Sections merge recursively; later declarations replace effective references while retaining each declaration's original value and Profile source. Duplicate keys within one file are errors. Local/import precedence follows read order. Successful merges record direct imports so shadowed children still participate in saves.

Reader records declaration baselines; edits in Loaded remain dirty. Save() writes changed sources in the receiver's import subtree, while explicit outputs emit local declarations. Changing a comment into directive text does not execute it or rebuild source relationships; reload to establish a new import graph.

Deployer uses Loading for imported-file hashes and records the root separately. Packager uses Loading to collect the referer's declarations before merging and Loaded to collect each parsed source, including the root. Containerizer validates source declarations in Loading. Reopening a path for hashing does not guarantee a digest of exactly the parsed bytes; that concern belongs to consumers.

See [ProfileImportTest](../test/Configuration/Profiles/ProfileImportTest.cs) for regression coverage.

## Declarations and saving

### Save entry points

| Entry point | Output scope |
| --- | --- |
| `Save()` / `Save(options)` | The receiver and its recursive imports; write changed declarations back to their respective source files. |
| A child Profile's `Save()` | That child and its import subtree, excluding parents and siblings. |
| `Save(path, ...)` | Only the receiver's local declarations; a null path falls back to FilePath. |
| `Save(Stream, ...)` / `Save(TextWriter, ...)` | Only local declarations, without writing imported files. |

Unchanged files retain their bytes and last-write timestamps. Explicit outputs always render, allowing deliberate formatting. Save-as does not change FilePath or rewrite relative import arguments; copying a file to another directory can change the import base on the next load.

Suppose app.ini contains only `#@import defaults.ini`, and defaults.ini declares `timeout=30` in `[network]`:

```csharp
var profile = Profile.Load("app.ini");
var entry = profile.Sections["network"].Entries["timeout"];
entry.Value = "60";
profile.Save();
```

Only defaults.ini changes; app.ini is not rewritten. Calling `entry.Profile.Save()` has the same result in this example. ProfileSection.SetEntryValue and Profile.SetOptionValue also edit the effective entry's source declaration, without creating a parent override.

To create a local override, explicitly call `profile.Sections["network"].Entries.Add("timeout", "60")`. An imported key permits a local declaration; an existing local declaration still rejects duplicates. The saved app.ini keeps its import and appends `[network]` and `timeout=60` afterwards.

### Declarations and the effective view

Statements use the internal nested Profile.Statement class in Profile.Declarations.cs. Each Profile internally retains local entries, comments (including import text), section declarations and `[]` root-scope switches in input order. Repeated section headers are distinct declarations; explicit empty sections survive. Blank positions remain in Profile.Blanks and participate in snapshots and output rather than introducing another mutable blank representation.

Public collections provide the merged effective view. Writer does not serialize their grouped enumeration order. Statements and effective indexes reference entry objects instead of storing separate mutable values. ProfileItem.Profile identifies the declaration's source. Later local declarations and imports replace the effective reference without overwriting the earlier declaration's value.

Duplicate complete keys within one file still fail. Local/import and import/import precedence follows actual reading order. Imports inside sections still merge at the current Profile root. Successful merges register direct imports, including children with no effective entries or entirely shadowed content. Repeated imports are read separately and are not cached.

Collection edits update statements and name indexes together. Replacements remove stale keys; duplicate failures do not partially update collections. Removing imported declarations through the parent is rejected, as is clearing a mixed collection before any changes occur. Edit the source collection explicitly. Structural changes, removal and precedence changes in a child require reloading the parent; there is no incremental dependency evaluator.

### Writer lifetime and output

Each save creates an internal sealed ProfileWriter. Profile retains public convenience methods; Reader owns parsing and Writer owns serialization and commits. No public writer, singleton, context factory or shared reader/writer base is introduced. Writer captures PreserveBlanks only; it does not clone directive options or execute callbacks. ProfileContext describes file loading; callback settings and directive options do not alter the save scope of loaded models.

Null values render as `name`, empty strings as `name=`. Comments use `#`, including empty comments. Statement order, empty sections and necessary scope switches are preserved. PreserveBlanks enables recorded blank lines; blanks discarded at load cannot be recovered at save.

Names and values must be representable by the existing grammar. No quoting, escaping or multiline-value syntax is added. Invalid line breaks, whitespace that splits a section name, and values that cannot round-trip are rejected before output. All comments, including import text, use ProfileComment. Editing comments may change import text but does not load files or refresh registered relationships; reload to build the new graph. Comments.Add accepts CRLF- or LF-separated multiline comments.

Writer uses local section state and the supplied TextWriter; there is no public writing context or OnWrite notification. Import text is serialized like other comments. Related-file traversal follows only the relationships recorded during loading.

Involved profiles must not be modified while saving. Detected declaration changes and reentrant saves fail. Callers must avoid concurrent edits, and supplied TextWriter implementations must follow the same rule.

### Baselines, repeated sources and commits

Reader records original declarations as they are read; edits in the import-completed callback remain dirty. Save compares actual statement content, order and mutable arrays instead of relying only on setter flags. A successful source write updates its baseline; reverting edits returns to the unchanged state. Save-as to another path and stream outputs do not clear source changes.

Targets are deduplicated by normalized identity using the same file and ancestor-link resolution as Reader. Windows comparisons ignore case; other platforms do not. One modified instance wins over unchanged instances of that source. Equal modified snapshots produce one output; conflicting modified snapshots fail before output. Saving does not refresh other independently loaded instances.

Related saves first finish all same-directory temporary files, including validation, serialization and disposal. Only then do they replace targets in import postorder, children before parents. Preparation failures do not replace any original file. Unchanged read-only files need no write; modified read-only targets are rejected. Parent directories are not created automatically, and target deletion is never a replacement fallback.

Multiple file replacements are not a transaction. A commit failure stops subsequent commits and throws ProfileException with the IO cause as InnerException. Data["FailedPath"] identifies the failed target; Data["CompletedPaths"] contains completed paths. Completed sources accept their snapshots; pending sources remain dirty for retry. Completed commits are not rolled back.

Temporary files are cleaned on exit. If cleanup also fails after a primary failure, the original exception is retained and its Data associates the temporary path with the cleanup exception, allowing callers to locate leftovers.

File writes follow symbolic-link targets without replacing the links. Hard-link propagation, concurrent-write conflict detection, cross-file atomicity and power-loss durability are not guaranteed. Permission and replacement semantics remain subject to the operating system and filesystem.

### Encoding, ownership and validation

File and Stream defaults use Encoding.UTF8. An explicit encoding applies only to the explicit output; original encoding/BOM are not retained for later restoration. TextWriter controls its own encoding and newlines. Changed files are rendered in the selected format, without a byte-for-byte preservation promise.

Writer owns path resources. Supplied streams are closed; supplied TextWriter instances remain open. Stream/text outputs may contain partial output after failure and cannot be rolled back.

[ProfileWriterTest](../test/Configuration/Profiles/ProfileWriterTest.cs) covers source writes, scope isolation, round trips, duplicate instances and preparation/commit failures with retry. Linux/macOS permission and link checks require native execution; Windows results do not substitute for them. Strict hashing of the parsed bytes remains a downstream snapshot concern; Core has no deployment, NuGet or hashing dependency.
