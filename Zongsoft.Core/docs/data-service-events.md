# Data service write events

[简体中文](data-service-events.zh-Hans.md)

`DataServiceBase<TModel>.EventFilterBase` is a public abstract nested class implementing `IDataServiceFilter<TModel>`. It prepares event data for five write operations and awaits the derived publication callbacks. A service opts in through `Filters.Add(...)`; constructing a service does not install it automatically. Reads, commands, existence checks, and aggregates are outside its scope.

## Registration

The constructor is `EventFilterBase(params DataAccessMethod[] methods)` and requires at least one operation: `Insert`, `Update`, `Delete`, `Upsert`, or `Import`. Omitting arguments or passing an empty array throws `ArgumentException`; a null array throws `ArgumentNullException`; other operation values throw `ArgumentOutOfRangeException`. Each exception identifies `methods` as the parameter. Duplicate values enable an operation once. The operation set is copied during construction. Disabled operations do not prepare Returning, materialize input, or invoke callbacks.

Operations are enabled by `DataAccessMethod` kind: `Insert` covers both single and batch inserts, and `Upsert` covers both single and batch upserts. The filter does not distinguish service methods by `DataServiceMethod.Name`; callbacks can still inspect `context.Method`.

The following service exposes insert events only. The business module supplies `Item` and `ItemEvents`. Both `ItemEvents.OnCreatedAsync` overloads should publish through the registry's `RaiseAsync` using one descriptor that declares `IDataDictionary<Item>` and `IEnumerable<IDataDictionary<Item>>`. Import `System`, `System.Threading`, `System.Threading.Tasks`, `System.Collections.Generic`, and `Zongsoft.Data` for this example.

```csharp
public sealed class ItemService : DataServiceBase<Item>
{
	public ItemService(IServiceProvider services, ItemEvents events) : base(services)
	{
		this.Filters.Add(new EventFilter(events));
	}

	private sealed class EventFilter(ItemEvents events) : EventFilterBase(DataAccessMethod.Insert)
	{
		protected override async ValueTask OnInsertedAsync(IDataServiceContext<Item> context,
			IDataDictionary<Item> argument, CancellationToken cancellation) =>
			await events.OnCreatedAsync(argument, cancellation);

		protected override async ValueTask OnInsertedAsync(IDataServiceContext<Item> context,
			IEnumerable<IDataDictionary<Item>> argument, CancellationToken cancellation) =>
			await events.OnCreatedAsync(argument, cancellation);
	}
}
```

The business module owns event names, descriptors, resources, plugin paths, and publication parameters. Each callback receives the current `IDataServiceContext<TModel>` and explicitly chooses any event parameters. The base class does not automatically copy service option parameters to events. Use the existing event registry to retain filtering, local handling, transaction deferral, and remote delivery; see [event dispatch and argument contracts](event-dispatching.md).

## Callbacks and arguments

| Operation | Publication callback | Source and argument shape |
| --- | --- | --- |
| Insert | `OnInsertedAsync` | Input with values populated during the write; `IsMultiple` selects single or collection |
| Update | `OnUpdatedAsync` | Current operation Returning `Newer`; complete primary-key conditions select single or collection |
| Delete | `OnDeletedAsync` | Current operation Returning `Older`; complete primary-key conditions select single or collection |
| Upsert | `OnUpsertedAsync` | Input with values populated during the write; `IsMultiple` selects single or collection |
| Import | `OnImportedAsync` | Import input with populated values, always a collection |

The first four callbacks have separate overloads for `IDataDictionary<TModel>` and `IEnumerable<IDataDictionary<TModel>>`. Import has only the collection overload. Collection arguments are materialized arrays. Default callbacks do nothing; derived filters override the overloads needed by their enabled operations.

Updates and deletes select a single argument only when every primary-key field has a definite non-null equality constant in the condition. Keyless entities, partial keys, IN, OR, ranges, nulls, and conflicting constants use collections when uniqueness cannot be established. One returned row does not change a collection contract. The single contract requires exactly one returned row. `UpdateManyAsync` invokes updates individually, with one notification per internal operation and no aggregate notification for the loop.

Publication requires a positive affected-row count; update and delete also require current Returning rows. Zero rows, execution failures, and preparation failures do not publish. Missing update/delete results are not replaced with input data or an extra query. Preparation and publication for enabled operations check cancellation. Callback completion, failures, and cancellation propagate through the original call. A callback failure does not itself undo a database operation that has already committed.

## Returning and input lifetime

Before updates and deletes, `PrepareReturning` requests all mapped simple properties of the main entity by default, excluding related objects. Derived filters can override this method to adjust requested columns, using the operation context's `Returning`. Callbacks receive the current operation's results for the selected kind; business callbacks determine the fields they publish. The engine and driver must support and validate the request; the filter does not bypass driver limitations.

The filter does not replace the caller's `Options.Returning` or modify its columns, order, kinds, or existing rows. The engine still appends current results using the caller's projection. Events use only the current context's rows. The filter does not modify caller `Parameters`.

Before execution, batch inserts and upserts materialize input as data dictionary arrays; imports materialize as `TModel[]`. Synchronous and asynchronous inputs are consumed once, and enumerator disposal completes before the array is passed to the write. Memory use grows with batch size, which callers should bound. Insert, upsert, and import data need not contain every database default or trigger-generated value. An aggregate affected-row count cannot identify which individual inputs were persisted.

The filter stores no mutable per-operation state and can handle overlapping operations. Derived callbacks are responsible for the concurrency of their own state. Data dictionaries preserve missing fields versus explicit nulls; consumers must use the fields actually provided.

Specify the union of operations required by local and remote business consumers when constructing the filter. Handler counts and channel states do not determine this list. Changing remote scope does not disable preparation needed locally; when removing an exclusively remote operation, remove its service enablement too. Install no filter when no operation is enabled. Other explicit registry publications remain independent.
