# Profile configuration: loading, directives and saving

[English](profiles.md) | [简体中文](profiles.zh-Hans.md)

Profile reads INI declarations, processes directives, merges imported sources and saves changes back to their declaring files.

- [Loading and imports](#loading-and-imports)
- [Declarations and saving](#declarations-and-saving)
- [Variable views](#variable-views)

## Loading and imports

`Profile.Load` accepts paths, Stream and TextReader. Each root load creates a ProfileReader.Session, an internal type nested in ProfileReader, that captures settings and the global directive registry, dispatches directives, manages file notifications and guards the active loading chain. ProfileReader parses one source; ImportDirective interprets import paths and requests nested reads through the same session. Profile retains declarations, effective references and source relationships. ProfileWriter serializes declarations and coordinates source commits; it does not execute directives or callbacks.

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

`ProfileOptions(bool preserveBlanks = true)` exposes PreserveBlanks, a get-only Directives collection, Loading/Loaded (`Action<ProfileContext>`). The collection exposes Processing/Processed (`Action<ProfileDirectiveContext>`) and a get-only Options property pointing to its owning ProfileOptions. Callbacks default to null. Omitted load options discard blanks; explicit new ProfileOptions() records them. Profile does not expand variables.

`ProfileDirectiveOptions` has an immutable Name and a settable Behavior. Names start with a letter or underscore and contain only letters, digits, underscores, hyphens or dots; they contain no comment marker or `@` prefix. `ProfileDirectiveOptionsCollection` uses case-insensitive name keys, rejects null and duplicate items, and supports keyed lookup. Its enumeration order does not control execution order. Missing settings use the directive's built-in defaults; removing settings restores those defaults. Adding settings does not register an implementation.

`ProfileDirectiveOptions.Import(behavior = None, maximumDepth = 0)` constructs the public nested ImportOptions type, whose name is fixed to `import`. MaximumDepth is nonnegative: zero selects the built-in limit of 64; positive values specify the limit; negative assignments throw ArgumentOutOfRangeException without replacing the current value. The root counts as one active level: 1 allows only the root. Each directive copies the captured settings and reads the limit from its context Options when executing. Processing may adjust MaximumDepth for that directive without changing the original options or other directives. Cycle detection remains enabled independently.

`ProfileDirectiveBehavior` is a general policy enum. None selects the directive's built-in behavior; Strict requests its strict rules; Ignore preserves the declaration as a comment without entering directive processing; Suppress throws ProfileException before directive callbacks or execution.

| Behavior | Import directive | Other directive without an implementation |
| --- | --- | --- |
| None | Load files; missing imported files/directories are allowed. | Offer the directive to callbacks and preserve an unhandled declaration as a comment. |
| Strict | Require every direct and recursive import to exist. | Throw unless the processing callback handles it. |
| Ignore | No directive callbacks or imported-file access. | No directive callbacks or execution. |
| Suppress | Reject the directive, including empty arguments. | Reject the directive, including empty arguments. |

These policies apply after the containing document's Loading notification. They do not suppress root file notifications. Names match without regard to case; invalid enum values are rejected when assigned.

At root entry, the session copies the options collection and calls each directive option's virtual Clone method. The copied collection points back to the copied ProfileOptions, and includes both directive callbacks. Recursive reads share the resulting settings. The default Clone preserves the runtime type and shallow-copies fields; derived options with mutable reference members must override it to copy those members. Changing the original collection, option properties or callback properties affects subsequent root loads. Callers manage mutable state captured by callback delegates.

### Global directive registry

`Profile.Directives` is the process-wide ProfileDirectiveCollection, initially containing `ImportDirective.Instance` from the `Zongsoft.Configuration.Profiles.Directives` namespace. It accepts public ProfileDirectiveBase implementations with an immutable Name and a Process(ProfileDirectiveContext) method. The collection supports Add, Count, name lookup, Contains, TryGetValue and snapshot enumeration. Names are case-insensitive; null instances and duplicate names are rejected. Registration is additive: removal and replacement are not exposed.

```csharp
// Register once during application initialization.
Profile.Directives.Add(new NoteDirective());

var options = new ProfileOptions();
options.Directives.Processing = context => Console.WriteLine(context.Name);
options.Directives.Add(new ProfileDirectiveOptions("note", ProfileDirectiveBehavior.Strict));
using var input = new StringReader("#@note Hello");
var profile = Profile.Load(input, options);

public sealed class NoteDirective() : ProfileDirectiveBase("note")
{
	public override void Process(ProfileDirectiveContext context)
	{
		context.Profile.Entries.Add("note", context.Argument);
	}
}
```

Each root load copies the registry before cloning options or invoking callbacks. Recursive imports use that same name-to-instance snapshot. Registrations during a load apply only to later root loads. The registry uses SynchronizedDictionary to synchronize modifications and snapshot creation; handlers execute outside the lock. Instances are shared across concurrent loads and must keep invocation state in locals or the supplied context, with thread-safe dependencies. A snapshot copies references, not handler instances.

Processing order is Ignore/Suppress checks, Processing callback, a registered implementation if not Handled, and Processed after success. Known implementations interpret None and Strict using their own rules. Unknown Strict directives must be handled by Processing; unknown None directives may remain unhandled. A callback can bypass a registered implementation by setting Handled=true.

ImportDirective treats the complete argument as a single file path, resolves it relative to the declaring source, handles optional versus required missing files, and selects the configured depth limit (default 64). An internal context operation passes the opened stream to the current ProfileReader.Session, which owns and releases it, checks the shared active chain, parses, merges and notifies Loaded. ImportDirective does not reference ProfileReader or ProfileWriter. Calling public Profile.Load inside a custom handler starts an independent root load; it is not the built-in recursive import path.

### File loading callbacks

Loading and Loaded run for roots and imports. Loading runs after opening and cycle/depth checks, before parsing. Loaded runs after successful parsing and recursive imports; an imported Profile has already been merged into its direct referer. Each notification receives a separate public ProfileContext with get-only properties:

| Property | Meaning |
| --- | --- |
| FilePath | Absolute loading path, or an empty string for anonymous input. |
| Depth | Active loading depth: root 1, direct import 2. |
| Referer | Direct referring Profile; null for roots. |
| Profile | Null in Loading; the parsed Profile in Loaded. |

Missing optional imports and rejected files do not receive file notifications. Repeated successful reads notify separately. A failure in Loading, parsing or merging prevents that file's Loaded callback. Callback exceptions propagate, streams/activity are cleaned, and prior notifications or merges are not rolled back. Notifications run while the file is still in the active chain. Retained Loading contexts are not filled later; referenced Profile objects remain editable.

### Directive callbacks

A directive is a comment beginning immediately with `@name`, such as `#@import a.ini` or `;@custom value`. A space or tab separates the name from its argument. Whitespace between the comment marker and `@` produces an ordinary comment. Original directive text remains a ProfileComment declaration.

Directives.Processing runs after the name and original argument have been identified, before execution. Directives.Processed runs after the whole directive succeeds, including its nested file reads and merges. Each import directive identifies one file and receives one pair of directive notifications; each successfully loaded file receives one pair of file notifications. Two import directives declared in sequence produce:

```text
Loading(root)
  Directives.Processing(import)
    Loading(a.ini)
    Loaded(a.ini)
  Directives.Processed(import)
  Directives.Processing(import)
    Loading(b.ini)
    Loaded(b.ini)
  Directives.Processed(import)
Loaded(root)
```

Recursive directives nest inside their containing file's notifications. An empty import or an optional missing import still completes directive processing. Processing, execution or nested callback failure prevents Directives.Processed. Ignore and Suppress take effect before either directive callback.

Both callbacks receive the same public sealed ProfileDirectiveContext, derived from ProfileContext. The inherited Profile is the currently parsed source; its Referer is the direct parent of that source. For A importing B and B importing C, the directive in B has Profile=B, Referer=A and Depth=2:

| Property | Meaning |
| --- | --- |
| Name | Directive name with its declared casing. |
| Argument | Settable directive argument with surrounding whitespace removed; assigning null preserves null. Each directive defines its meaning. |
| Handled | Set true in Directives.Processing to take over execution. The dispatcher also sets it true when a registered implementation returns successfully. |
| Behavior | The captured policy for this directive. |
| Options | An independent copy of this directive's settings, including derived properties; implementations read this copy directly, so Processing edits can affect directive-specific settings such as import MaximumDepth, without changing session settings, other directives or the captured Behavior. Without explicit settings, this is a plain ProfileDirectiveOptions; configured derived settings retain their type. |
| Profile / Section | Declaring Profile and current section; Section is null at the root section. |
| FilePath / LineNumber / Depth | Declaring source path, one-based line and active file depth. |

Argument and Handled changes made in Directives.Processing guide execution. Edits in Directives.Processed do not re-execute the directive. Arguments are never written back into the original declaration. For example, a caller can redirect an import and implement a custom directive:

```csharp
options.Directives.Processing = context =>
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

Each import directive treats its complete argument, with surrounding whitespace removed, as a single file path. Internal spaces, tabs and `|` remain part of the path; character validity depends on the operating system and filesystem. Null or empty arguments read no files. Paths do not interpret or remove quotes, expand variables or process globs; write paths containing spaces directly without quotes. Relative paths use the declaring file's loading directory; absolute paths are allowed. Imports merge at the current Profile root even when declared inside a section.

To import several files, declare one directive per file in reading order:

```ini
#@import ../shared files/base.env
#@import ../.shared/product.env
#@import ../.shared/production.env
```

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

Each save creates an internal sealed ProfileWriter. Profile retains public convenience methods; Reader owns parsing and Writer owns serialization and commits. No public writer, singleton, context factory or shared reader/writer base is introduced. Writer captures PreserveBlanks only; it does not clone directive options or execute callbacks. ProfileContext describes the current source and configuration; callback settings and directive options do not alter the save scope of loaded models.

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

## Variable views

`ProfileExtension.ToVariables()` returns `Zongsoft.Common.IVariables`, adapting configuration objects into live read-only variable views:

| Conversion | Lookup scope |
| --- | --- |
| `profile.ToVariables()` | Current effective root entries and all sections, including merged imports. |
| `section.ToVariables()` | The selected section and all descendants, retaining the full namespace from the root. |
| `entry.ToVariables()` | Only the selected entry, retaining its section's full namespace. |

Conversion does not copy a dictionary. Value changes and additions, removals or replacements within the selected profile or section appear in subsequent queries. Section and entry views retain the selected object; removing or replacing that object in its original collection does not retarget the view. Views do not search other profiles, read ahead, expand templates or add synchronization for concurrent access.

### Name mapping

Root entries belong to the default namespace. Section levels are joined with `.`, retaining dots within section names. Each dot-separated segment must match the ASCII identifier rule `[A-Za-z_][A-Za-z0-9_]*`. Invalid segments, including empty segments, leading digits or other characters, exclude that section and its descendants from variable mapping.

Every `.` and `-` in an entry name becomes `_` before validating the complete identifier. For example, `db-name` and `db.name` both map to `db_name`, while `-name` maps to `_name`. Other invalid entries provide no variable. Conversion never changes original names, values or saved declarations.

```ini
product=erp

[mysql]
db-name=zongsoft

[io rustfs]
database=attachments

[app.runtime]
worker-count=4
```

The variables are `product`, `mysql:db_name`, `io.rustfs:database` and `app.runtime:worker_count`. Section views do not move entries into the default namespace: `profile.Sections["mysql"].ToVariables()` still requires the `mysql` namespace.

### Lookup and conflicts

`TryGetValue(name, out value)`, `TryGetValue(null, name, out value)` and `TryGetValue("", name, out value)` are equivalent and query only the default namespace. Names and namespaces use OrdinalIgnoreCase. Overloads without a Boolean parameter do not fall back; `TryGetValue("A.B.C", "key", true, out value)` checks A.B.C, A.B, A and global. Fallback does not broaden view scope: sections supply only their subtrees, entries only themselves. Null or empty stops lookup. Profile has no additional defaults.

Query parameters are not trimmed or normalized. Pass the mapped name `db_name`; querying `db-name` or `db.name` directly returns false. A null query name throws ArgumentNullException; other invalid queries or missing variables return false. A null conversion source also throws ArgumentNullException.

Values remain raw strings or null. An existing entry with a null value still returns true. The source performs no formatting, recursion, member navigation or type conversion.

Mapping can produce duplicate names, such as `db-name` and `db.name` in one section, or identical entry names under `[io rustfs]` and `[io.rustfs]`. Only querying the conflicting variable throws ProfileException. View creation and other queries remain valid. Sections sharing a namespace may provide different variable names without conflict.

Conflicts are limited to the selected view's scope; a single-entry view does not inspect other entries. Ordinary import overrides of the same original name in the same section are already resolved by Profile and are not mapping conflicts. Removing a conflicting entry restores successful lookup immediately.

### Template evaluation

```csharp
using Zongsoft.Configuration.Profiles;
using Zongsoft.Common;
using Zongsoft.Text.Templating;

var profile = Profile.Load("settings.ini");
IVariables variables = profile.ToVariables();

variables.TryGetValue("mysql", "db_name", out var database);

var evaluator = new TemplateEvaluator();
evaluator.Providers.Add(variables);
var text = evaluator.Evaluate("Database=${mysql:db_name};Storage=${io.rustfs:database}");
```

When a template queries a conflicting variable, TemplateEvaluator throws TemplateEvaluationException with Code=ProviderFailed and preserves ProfileException as InnerException. TemplateEvaluatorOptions.Recursive controls recursive evaluation of string values.

These extensions only supply variables. Profile loading, import arguments and Save do not evaluate templates automatically; ProfileEntry.Value retains the original text. The caller explicitly composes variable sources and invokes template evaluation.

See [ProfileExtension](../src/Configuration/Profiles/ProfileExtension.cs), the [IVariables contract](variables.md#variable-contract), and [ProfileVariablesTest](../test/Configuration/Profiles/ProfileVariablesTest.cs).
