# Profile configuration: loading, declarations and saving

[English](profiles.md) | [简体中文](profiles.zh-Hans.md)

This document describes Profile loading and imports, declarations and source ownership, and the rules for saving and committing source files.

- [Loading and imports](#loading-and-imports)
- [Declarations and saving](#declarations-and-saving)

## Loading and imports

Profile.Load reads INI files; #@import is built into the reader. Core exposes no general directive interfaces, registry, handlers or reading/writing operation contexts, and depends on no deployment, NuGet, hashing or lock-file types.

### Responsibilities and lifetime

- Profile owns declarations and the effective configuration, with public Load/Save entry points. Its internal Import method merges effective references and records direct imports.
- Each root load creates an internal sealed ProfileReader shared throughout recursive imports. It opens files, parses input, recognizes imports, runs notifications and guards cycles/depth. Its private nested Context contains only the current Profile, line number and section.
- ReadCore scopes streams and active paths; Parse uses ProfileUtility for line/import syntax. No static state or loading cache is used. Every exit cleans activity state, allowing internal retries on the same reader after failure.
- ProfileWriter serializes declarations and coordinates source commits. It does not execute import text or expose write callbacks. See [declarations and saving](#declarations-and-saving).

```text
Profile.Load -> ProfileReader.Read -> ReadFile/ReadCore -> Parse
                                    ^                      |
                                    +--- built-in import ---+
Parsed child -> parent Profile.Import(child) -> Imported callback -> activity cleanup
```

Public Load/Save signatures remain unchanged. Internal read entry points have no general callbacks or external inheritance hooks. Loaded models retain source relationships, not their reader.

### Import notification options

```csharp
using Zongsoft.Configuration.Profiles;

var options = new ProfileOptions
{
	Importing = context => Console.WriteLine($"Reading {context.FilePath} at depth {context.Depth}"),
	Imported = context => Console.WriteLine($"Merged {context.Profile.FilePath} into {context.Referer.FilePath}"),
};

var profile = Profile.Load("settings.ini", options);
```

`ProfileOptions(bool preserveBlanks = true)` retains the blank-line constructor parameter and exposes settable PreserveBlanks, RequireImports, MaximumDepth, Importing and Imported properties. Both callbacks are `Action<ProfileContext>` and default to null. Omitted load options discard blanks; explicit new ProfileOptions() records them.

Reader executes imports automatically, with no enable switch. MaximumDepth defaults to 64 and accepts only positive integers; invalid assignments throw ArgumentOutOfRangeException and retain the previous value. The root counts as level one: 1 permits only the root, while a higher value such as 128 allows deeper chains. Set it with `new ProfileOptions { MaximumDepth = 128 }`. Throw from either callback to abort the entire load; there is no return value for silently skipping a file.

Reader shallow-clones ProfileOptions at the start of a root load to capture blank handling, RequireImports, MaximumDepth and both delegate references. Recursive reads share that snapshot; subsequently replacing properties on the original options does not affect the current load. Callers own concurrency of mutable callback captures. Writer does not execute import callbacks; save scope depends only on recorded source relationships.

`ProfileContext` is public sealed, constructed internally by Reader, with get-only properties:

| Property | Meaning |
| --- | --- |
| FilePath | Normalized absolute loading path of the imported file. |
| Depth | Active loading depth of this file; root is 1, a direct import is 2. |
| Referer | The direct referring Profile containing this import declaration. |
| Profile | Null during Importing; the parsed child after recursive imports and merging during Imported. |

Before and after notifications receive separate context instances. Retaining a before context never causes its Profile to be filled later. Get-only properties fix references, while referenced Profile models remain editable. The context exposes no reader, input stream or recursive entry point and is not used for saving.

### Syntax, paths and recursion

#@import defaults.ini and ;@import defaults.ini load a file. The keyword is case insensitive and must end at a space, tab or end of line. Empty arguments do nothing; other names such as @imported remain ordinary comments. Import text is recorded as an ordinary ProfileComment declaration, without a public directive model.

Paths are separated by spaces, tabs or |. Quoted escaping, variable expansion and globs are not added. Relative paths use the containing file's loading directory; absolute paths are allowed. Cycle identity resolves file and ancestor-directory links independently, retaining the original relative-path base. Windows ignores path case; other platforms use ordinal comparison. Depth limits cover unrecognized aliases such as hard links.

Only active-chain repetition is a cycle. Diamond and sequential repeated imports are read again without caching. Cycle/depth errors identify the reason, import chain, referring file and one-based line number. The default MaximumDepth permits 64 active files; a 65th fails before notification. A custom limit changes this boundary but does not disable cycle detection. RequireImports defaults to false: imports suppress only file/directory-not-found errors while opening, not permissions or other failures. Set RequireImports = true to require every direct and recursive import. A missing file then raises ProfileException with the target path, referring file, one-based line and original IO exception. Root files are always required; failed opens do not invoke callbacks.

FileStream roots have paths and participate in identity checks. Anonymous streams accept absolute imports and reject relative ones. Profile.Load closes the supplied stream. Explicit root encoding applies only to the root; imports default to UTF-8 with BOM detection.

### Import notifications

| Callback | Timing |
| --- | --- |
| Importing | After opening and cycle/depth checks, before parsing; the context Profile is null. |
| Imported | After parsing the child and its imports and merging into its parent; the context identifies the child Profile and direct Referer while the child is still active. |

Roots do not notify. A importing B importing C produces Importing(B), Importing(C), Imported(C), Imported(B). Repeated successful imports notify separately. Missing or rejected files do not produce their corresponding notifications.

Importing, parse or merge failures suppress that file's Imported callback. Either callback throwing aborts loading and cleans streams/activity without rolling back prior notifications or merges. Discard collected results from failed loads.

### Merging, saving and consumers

Imports merge at the current Profile root, even when written inside sections. Sections merge recursively; later declarations replace effective references while retaining each declaration's original value and Profile source. Duplicate keys within one file remain errors. Local/import precedence follows read order. Successful merges record direct imports so shadowed or ineffective children still participate in saves.

Reader records original declaration baselines; edits in Imported remain dirty. Save() writes only changed sources in the receiver's import subtree. Explicit path/stream/text outputs emit only local declarations. Comments may be edited into import text, but text edits do not load new files or update relationships; reload to build a new import graph.

Deployer uses Importing to hash child files and records the root separately. Packager uses before/after notifications to validate each source. Hashing by reopening a path still does not guarantee a digest of exactly the parsed bytes; that snapshot concern belongs downstream.

See the [implementation checklist](../PROFILE-BUILTIN-IMPORT-TASKS.md) for validation and platform limits.

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

Each save creates an internal sealed ProfileWriter. Profile retains public convenience methods; Reader owns parsing and Writer owns serialization and commits. No public writer, singleton, context factory or shared reader/writer base is introduced. Writer clones ProfileOptions to freeze blank handling; it does not execute import callbacks. ProfileContext describes import notifications only; callback settings and MaximumDepth do not alter the save scope of loaded models.

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

Writer owns path resources. Supplied streams retain the existing closing behavior; supplied TextWriter instances remain open. Stream/text outputs may contain partial output after failure and cannot be rolled back.

See [PROFILE-WRITER-TASKS.md](../PROFILE-WRITER-TASKS.md) for the checklist and validation results. Tests cover source writes, scope isolation, round trips, duplicate instances and preparation/commit failures with retry. Linux/macOS permission and link checks require native execution; Windows results do not substitute for them. Strict hashing of the parsed bytes remains a downstream snapshot concern; Core has no deployment, NuGet or hashing dependency.
