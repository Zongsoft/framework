# Zongsoft.Data ORM Framework

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

<a name="abstract"></a>
## Abstract

The [Zongsoft.Data](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data) is a [GraphQL](https://graphql.org/)-style **ORM**(**O**bject/**R**elational **M**apping) data access framework.

Its core idea is simple: describe the data shape and entity relationships declaratively, and let the engine generate the SQL. Most application code can query, write, and navigate data without hand-written SQL or SQL-like strings.

<a name="feature"></a>
## Features

- Supports strict POCO objects without attribute or annotation dependencies;
- Supports read/write splitting;
- Supports data operations on inherited tables;
- Keeps mappings isolated by business module and provides extension points;
- Supports navigation, filtering, paging, grouping, and aggregation without hand-written SQL;
- Fits common object-oriented programming habits and is easy to start with;
- Focuses on balanced performance, maintainability, and ease of use;
- Keeps dependencies small, usually only ADO.NET plus the native ADO.NET driver.

<a name="driver"></a>
## Drivers

| **Driver** | **Project Path** | **State** |
| --- | --- | :---: |
MySQL | [/drivers/mysql](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/mysql) | _**A**vailable_ |
SQL Server | [/drivers/mssql](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/mssql) | _**A**vailable_ |
PostgreSQL | [/drivers/postgres](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/postgres) | _**A**vailable_ |
SQLite | [/drivers/sqlite](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/sqlite) | _**A**vailable_ |
DuckDB | [/drivers/duckdb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/duckdb) | _**A**vailable_ |
ClickHouse | [/drivers/clickhouse](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/clickhouse) | Incomplete |
InfluxDB | [/drivers/influx](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/influx) | Incomplete |
TDengine | [/drivers/tdengine](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/tdengine) | _**A**vailable_ |

> 💡 Tip: If you need a driver that is not listed here or commercial support, please contact us ([zongsoft@qq.com](mailto:zongsoft@qq.com)).

<a name="plugin-quickstart"></a>
## A Real Plugin Workflow: Discussions

[Discussions](../../discussions/README.md) is a forum module using the Data engine. Deploy its package together with the engine and a database driver; application services use the public `IDataAccess` and `IDataService<T>` contracts.

### 1. Deploy the Module, Engine, and Driver

Add the following packages to an existing host deployment manifest; retain its Main, Web/Terminal and Security dependencies. See [plugin composition](../Zongsoft.Plugins/README.md).
```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data mysql]
nuget:Zongsoft.Data.MySql

[plugins zongsoft discussions]
nuget:Zongsoft.Discussions
```

### 2. Prepare the Actual Data and Configuration

Follow the [Discussions database documentation](../../discussions/database/Zongsoft.Discussions.md) and [module deployment manifest](../../discussions/src/Zongsoft.Discussions.deploy). Deploy the complete mapping and plugin, including validators and filters. Configure a named `Discussions` connection under `/Data/ConnectionSettings`, and configure the site and file storage using [the module option file](../../discussions/src/Zongsoft.Discussions.option). Mapping files describe existing structures; loading one does not initialize a database.

### 3. Access Data through Services

Resolve the module services from the configured host and await their data operations. Consume queries with `await foreach`, pass cancellation to the asynchronous methods, and keep the configured site and identity restrictions. An accessor obtained from `IDataAccessProvider.GetAccessor("Discussions")` is owned by its provider; do not dispose it at the end of each request.

Initialize the database, site, users, and identity before using the module. Loading mapping files does not create tables. See the [Discussions usage documentation](../../discussions/README.md) for the module configuration.

<a name="schema"></a>
## The data schema

The data **schema** is a DSL(**D**omain **S**pecific **L**anguage) that describes which fields are queried or written _(**D**elete/**I**nsert/**U**pdate/**U**psert)_. It looks similar to [GraphQL](https://graphql.org/), but it does not need a server-side GraphQL definition. It is used to choose fields, include navigation properties, and control cascade scopes.

The `schema` argument in a data access method is the schema text.

<a name="schema-syntax"></a>
### Schema Syntax

```
schema ::=
{
    * |
    ! |
    exclusion |
    inclusion
} [,...n]

identifier ::= [_A-Za-z][_A-Za-z0-9]*
number ::= [0-9]+

path ::= identifier {"." identifier}
exclusion ::= "!" path
inclusion ::= path [limit] [sorting] ["{" schema [,...n] "}"] | path ".*"

limit ::= ":"(number | "*")

sorting ::=
"("
    {
        ["~"|"-"|"+"]identifier
    } [,...n]
")"
```

💡 The `Department.Manager` / `Secret` grammar examples below come from the actual [SchemaParserTest](test/SchemaParserTest.cs) fixture and [Core SchemaTest](../Zongsoft.Core/test/unit/Data/SchemaTest.cs). They test parsing, not a Discussions organization module.

<a name="schema-overview"></a>
#### Schema Overview

- Asterisk(`*`): includes all scalar properties. Navigation properties are not included unless you name them explicitly.
- Exclamation(`!`): excludes fields. A bare `!` clears all members at the current level; `!Name` excludes the named property.
- A dot(`.`) joins nested member names. `Department.Manager.Name` is equivalent to `Department{Manager{Name}}`, and dotted paths may be mixed with braces. Whitespace is allowed on both sides of a dot.
- A terminal mapped navigation property automatically includes its scalar properties. Therefore `Department`, `Department.*`, and `Department{*}` produce the same member tree. The `.*` form is terminal and cannot be followed by another path segment.
- A dotted exclusion such as `!Department.Manager.Secret` validates every segment and removes only the target member. `!Department` removes the whole navigation member and is equivalent to `Department{!}`. The `!*` form and `!path.*` are invalid.
- Identifiers are composed of letters, digits, and underscores, must not start with a digit, and are case-insensitive.
- Multiple members are separated by commas(`,`); the sub-schema _（inside the braces）_ uses the same syntax, so nesting is allowed at any depth. Empty comma segments and leading/trailing commas are ignored.
- Whitespace is not allowed inside an identifier, but is allowed between members, after the asterisk, and between an identifier and the sub-schema/sorting/limit tokens, e.g. `Users {*}`、`Users :20`、`* , Users`.
- Whitespace is not allowed inside a limit or right after the colon(`:`); it is not allowed inside the sorting parentheses or a sorting field, but is allowed between sorting fields _（after a comma）_.
- A limit or sorting expression finishes the dotted path. For example, `Departments:10(~Name)` includes the navigation's scalar properties, while `Departments:10.Manager` and `Departments:10(~Name).Manager` are invalid.

<a name="schema-paging"></a>
#### Limit and Sorting

A limit is written after the member name and starts with a colon(`:`). It controls the maximum number of records loaded for a one-to-many member:

| Syntax | Meaning |
| --- | --- |
| omitted | unlimited _（the default）_ |
| `:N` | at most `N` records when `N > 0` |
| `:0` | unlimited |
| `:*` | unlimited |

The parsed limit is an integer. Any value less than or equal to zero means unlimited; canonical schema text omits an unlimited limit. Schema expressions accept only unsigned numbers or `*` after the colon.

Sorting is written after the optional limit and wrapped in parentheses; sorting fields are separated by commas. A `~` or `-` prefix means descending, while a `+` prefix or no prefix means ascending:

```
Users:20(-IsModerator,+UserId){*}
```

Every sorting field may carry its own prefix. `+CreatedTime` is exactly equivalent to `CreatedTime`. If a field is declared more than once, the last declaration determines its direction and position.

<a name="schema-sample"></a>
### Sample description

- **Note:** All scalar properties except `CreatorId` and `CreatedTime`.

	> ```graphql
	> *, !CreatorId, !CreatedTime
	> ```

- **Note:** All scalar properties plus the `Creator` navigation property, including all scalar properties of `Creator`.

	> ```graphql
	> *, Creator{*}
	> ```

	The following forms are equivalent:
	> ```graphql
	> *, Creator
	> *, Creator.*
	> *, Creator{*}
	> ```

- **Note:** All scalar properties plus the `Creator` navigation property, but only `Name` and `Nickname` are loaded for `Creator`.

	> ```graphql
	> *, Creator{Name,Nickname}
	> ```

- **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, without sorting or a record limit.

	> ```graphql
	> *, Users{*}
	> ```

- **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, limited to at most 1 record.

	> ```graphql
	> *, Users:1{*}
	> ```

- **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, limited to at most 20 records.

	> ```graphql
	> *, Users:20{*}
	> ```

- **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_ with an explicitly unlimited record count.

	> ```graphql
	> *, Users:*{*}
	> ```

- **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, sorted by `IsModerator` descending and `UserId` ascending, then limited to at most 20 records.

	> ```graphql
	> *, Users:20(-IsModerator,+UserId){*}
	> ```

-----

💡 Nested braces can be replaced or mixed with dotted paths. The following expressions produce the same member tree:

```graphql
*, User,
Department.*,
Department.Manager.Name,
Department.Manager.Gender,
Department.Manager.FullName,
!Department.Manager.Secret
```

```graphql
*, User{*}, Department{*, Manager{Name,Gender,FullName,!Secret}}
```

<a name="schema-computed"></a>
### Computed Members

An explicitly named member that is not mapped may still be used when the corresponding model defines a public instance property or field with that name. Such a computed member participates in the returned model shape but does not generate a database field. An unknown name that exists in neither the mapping nor the model is invalid. The `*` wildcard includes mapped scalar properties only and never adds computed members automatically.

💡 **Note:** Any mapped inputs required by a computed property must be included explicitly.


<a name="mapping"></a>
## Mapping file

A data mapping file is an XML file with the `.mapping` extension. It defines the metadata for entities, tables, fields, keys, and navigation relationships. **Do not** put all metadata for a large application into one file; each business module should have its own mapping file.

The [Zongsoft.Data.xsd](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.xsd) XML Schema file provides IntelliSense and validation for hand-written mapping files.

The mapping file root is `schema`, and each `container` represents one metadata namespace. Most business modules have one container whose `name` matches the module name.

The following is a partial excerpt of the [Discussions mapping](../../discussions/src/Zongsoft.Discussions.mapping); deploy the original complete file, not this shortened fragment.

```xml
<schema xmlns="http://schemas.zongsoft.com/data">
	<container name="Discussions">
		<entity name="Forum" table="Discussions_Forum">
			<key>
				<member name="SiteId" />
				<member name="ForumId" />
			</key>
			<property name="SiteId" type="uint" nullable="false" />
			<property name="ForumId" type="ushort" nullable="false" sequence="#(SiteId)" />
			<property name="GroupId" type="ushort" nullable="false" />
			<property name="Name" type="nvarchar" length="50" nullable="false" />
			<complexProperty name="Users" port="ForumUser" multiplicity="*" immutable="false">
				<link port="SiteId" />
				<link port="ForumId" />
			</complexProperty>
		</entity>
	</container>
</schema>
```

Common mapping elements:

- `entity` maps an application entity to a table. `table` is the physical table name（when omitted it defaults to `namespace_entity`, or the entity name when the namespace is empty; `alias` is also accepted as an alternative to `table`）, `inherits` points to a parent entity, `driver` limits the entity to a specific data driver, and `immutable="true"` makes the entity read-only except for insert operations.
- `property` maps a scalar member to a field. Important attributes include `type`, `field`, `nullable`, `length`, `precision`, `scale`, `default`, `sequence`, `sortable`, and `immutable`.
- `sequence="*"` means the database built-in identity/sequence is used. `sequence="#"` means the Zongsoft default external sequencer is used. `sequence="#Name"` means a named external sequencer; `sequence="#Name@seed/interval"` also specifies the seed and interval; `sequence="#(ParentId)"` means an external sequencer grouped by the specified reference property; `sequence="Entity:Property"` references the sequencer of another entity's property.
- `complexProperty` defines a navigation property. Its `port` points to the target entity, or to a target entity's navigation property such as `ForumUser:User`. `multiplicity` supports `?`（zero-or-one，default）、`!`（exactly one）、`*`（one-to-many）; `link` maps foreign key properties to the current entity（`anchor` specifies the anchor on the current side and defaults to the `port` name when omitted）, and `constraints` add fixed navigation filters（when `actor` is omitted it is inferred from the multiplicity: `Foreign` for one-to-many, otherwise `Principal`）.
- `command` defines a named SQL command or stored procedure. Commands are executed through `ExecuteAsync`, `ExecuteAsync<T>`, or `ExecuteScalarAsync`. `type` supports `text`（default）and `procedure`; `mutability` declares whether the command reads or writes data. It is not merely descriptive metadata: when read/write splitting is configured, the data source selector uses it as the routing basis—`none` selects a readable source, while `delete`、`insert`、`update`、`upsert` select a writable source. The selector does not infer mutability by analyzing the SQL text. The loader parses these enum values case-insensitively, but use lowercase to pass XSD validation.

[Zongsoft.Data.xsd](Zongsoft.Data.xsd) defines the mapping XML. Use `type="procedure"` and an alias naming an existing stored procedure. Declare output-parameter directions and read the supplied `Parameter` objects after completing the query or ending enumeration and awaiting enumerator disposal.


> **Enable XML IntelliSense for mapping files:**
>
> Copy [Zongsoft.Data.xsd](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.xsd) and [Zongsoft.Data.catalog.xml](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.catalog.xml) to the XML Schemas template directory of **V**isual **S**tudio, for example:
> - **V**isual **S**tudio 2026 _(Enterprise Edition)_ <br />
> 	`C:\Program Files\Microsoft Visual Studio\18\Enterprise\Xml\Schemas`


> Although some developers like generating mapping files, we recommend writing them by hand:
>
> - Data structures and relationships are the foundation of a system. Database tables are their physical form, and mapping files describe how application entities match those tables.
> - Mapping files should be maintained by the system architect or module owner. Settings such as `inherits`, `immutable`, `sortable`, `sequence`, and navigation properties directly affect application code.


<a name="connection"></a>
## Connection Settings

Connection settings follow the [Core settings contract](../Zongsoft.Core/docs/settings.md). `Value` contains explicit driver parameters and extension properties such as `CircuitBreaker.Duration`. Database drivers create provider options through `GetOptions()`; pass those native options or their connection string to the provider, rather than the complete framework `Value`.

The connection name is resolved by [DataAccessProviderBase](../Zongsoft.Core/src/Data/DataAccessProviderBase.cs). Discussions requests its module name; configure that name explicitly instead of accidentally falling back to another module's default connection.

Use the following configuration shape with deployment-supplied values. `REPLACE_WITH_*` marks required environment values, not a shipped account or database.
```xml
<options>
	<option path="/Data">
		<connectionSettings>
			<connectionSetting connectionSetting.name="Discussions" driver="MySql"
				value="Server=REPLACE_WITH_HOST;Database=REPLACE_WITH_DATABASE;UserName=REPLACE_WITH_USER;Password=REPLACE_WITH_PASSWORD" />
		</connectionSettings>
	</option>
</options>
```

For read/write separation, use `accessor#source` names and `ReadOnly`/`WriteOnly` modes. This is an engine capability, not a claim that Discussions ships a replicated database topology. See [DataSourceSelector](src/Common/DataSourceSelector.cs) and the [MySQL driver](drivers/mysql/README.md). Replication, consistency and credentials remain deployment responsibilities.

<a name="usage"></a>
## Usages

All data operations go through the [`Zongsoft.Data.IDataAccess`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) interface in [Zongsoft.Core](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core). It supports these operations:

- `ValueTask<int> CountAsync(...)`
- `ValueTask<TValue?> AggregateAsync(...)`
- `ValueTask<bool> ExistsAsync(...)`
- `IAsyncEnumerable<T> ExecuteAsync<T>(...)` `ValueTask<object> ExecuteScalarAsync(...)`
- `ValueTask<int> ImportAsync(...)`
- `ValueTask<int> DeleteAsync(...)`
- `ValueTask<int> InsertAsync(...)` `ValueTask<int> InsertManyAsync(...)`
- `ValueTask<int> UpdateAsync(...)` `ValueTask<int> UpdateManyAsync(...)`
- `ValueTask<int> UpsertAsync(...)` `ValueTask<int> UpsertManyAsync(...)`
- `IAsyncEnumerable<T> SelectAsync<T>(...)`

💡 Examples use the [Discussions models](../../discussions/src/Models/Thread.cs) and [mapping](../../discussions/src/Zongsoft.Discussions.mapping) to illustrate the current asynchronous API. They run inside a composed module service; siteId and userId are validated identity values. Import Zongsoft.Collections for the awaitable FirstOrDefault extension. Await scalar and write operations, consume query results with await foreach, and pass cancellation tokens as the last positional argument.

<a name="operand"></a>
### Operand

Operands can be used in conditions (`Condition`) and in values written to fields. The main operand types are:
- Constant operand `ConstantOperand<T>`
- Field operand `FieldOperand`
- Function operand `FunctionOperand`
- Aggregation operand `AggregateOperand`
- Unary operand `UnaryOperand`, including:
> - `!` logical `NOT`
> - `~` bitwise `NOT`
> - `-` arithmetic negation
- Binary operand `BinaryOperand`, including:
> - `+` addition
> - `-` subtraction
> - `*` multiplication
> - `/` division
> - `%` remainder
> - `&` bitwise or logical `AND`
> - `|` bitwise or logical `OR`
> - `^` bitwise or logical `XOR`

#### Examples

[ThreadService.OnGet](../../discussions/src/Services/ThreadService.cs) increments the persisted view count after its access checks. The following example expresses that operation with the asynchronous API:
```csharp
await this.DataAccess.UpdateAsync<Thread>(new
{
	TotalViews = Operand.Field(nameof(Thread.TotalViews)) + 1,
	ViewedTime = DateTime.Now,
}, Condition.Equal(nameof(Thread.ThreadId), thread.ThreadId));
```

`Operand.Field` refers to the stored value; `+ 1` is evaluated by the database rather than by a client-side read/modify/write loop. Keep the surrounding authorization and history handling when reading this service.

For operator precedence without a database, [OperandTest.Test1](../Zongsoft.Core/test/unit/Data/OperandTest.cs) constructs the expression below and checks its tree. These constants are test inputs, not an order-processing application:
```csharp
Operand a = Operand.Constant(1);
Operand b = Operand.Constant(2);
Operand c = Operand.Constant(3);
Operand d = Operand.Constant(4);
Operand e = Operand.Constant(5);

var expression = (a + b) * (c - d) / e;
```

Function and aggregate operands are defined by [Operand](../Zongsoft.Core/src/Data/Operand.cs); database support depends on the selected driver.

<a name="condition"></a>
### Condition

`Condition` represents predicates for queries and writes. Combine conditions with `&` and `|`; keep authorization constraints outside any business `OR` group.

The actual [ForumService.GetThreads](../../discussions/src/Services/ForumService.cs) starts with:
```csharp
var criteria =
	Condition.Equal(nameof(Thread.ForumId), forumId) &
	Condition.Equal(nameof(Thread.Visible), true);
```

The method then excludes the separately loaded topmost threads on the first page before calling `SelectAsync<Thread>`. See the complete method for paging behavior.

Search DTOs are also real models: [ThreadCriteria in Thread.cs](../../discussions/src/Models/Thread.cs) declares nullable fields and a `Range<DateTime>?` time range; its `Title` property uses `[Condition(ConditionOperator.Like)]`. [ThreadService](../../discussions/src/Services/ThreadService.cs) selects that type with `[DataService(typeof(ThreadCriteria))]`. Do not redefine a different DTO with the same name. The Core [Criteria](../Zongsoft.Core/src/Data/Criteria.cs) API supplies parsing and transformation.

<a name="usage-query"></a>
### Query operation

<a name="usage-query-1"></a>
#### Basic query

- By default, all scalar fields are returned. Use the `schema` argument to choose a smaller field set.
- Queries return IAsyncEnumerable<T>. Preparation starts on invocation; database rows are read during await foreach. Use await query.FirstOrDefault(cancellation) to obtain the first row.

**Note:** Queries are not paged by default. Prefer streaming or explicit paging and limit the number of records materialized in memory.

```csharp
// Query all scalar fields that match the condition(lazy loading).
var threads = this.DataAccess.SelectAsync<Thread>(
	Condition.Equal("SiteId", siteId) &
	Condition.Equal("Visible", true));

// Query one entity and load only selected fields.
var forum = await this.DataAccess.SelectAsync<Forum>(
	Condition.Equal("SiteId", siteId) &
	Condition.Equal("ForumId", 100),
	"SiteId,ForumId,Name,Description,CoverPicturePath").FirstOrDefault();
```

<a name="usage-query-exists"></a>
#### Exists and aggregate query

Use `ExistsAsync` when you only need to know whether a row exists. Use `CountAsync`, `SumAsync`, `AverageAsync`, `MaximumAsync`, `MinimumAsync`, `MedianAsync`, `DeviationAsync`, or `VarianceAsync` extension methods when you only need aggregate values.

```csharp
var exists = await this.DataAccess.ExistsAsync<Thread>(
	Condition.Equal(nameof(Thread.ThreadId), threadId) &
	Condition.Equal(nameof(Thread.Visible), true));

var totalThreads = await this.DataAccess.CountAsync<Thread>(
	Condition.Equal(nameof(Thread.ForumId), forumId));

var totalViews = await this.DataAccess.SumAsync<Thread, long>(
	nameof(Thread.TotalViews),
	Condition.Equal(nameof(Thread.ForumId), forumId));
```

<a name="usage-query-2"></a>
#### Scalar value query

Scalar queries return a single field value. They avoid loading unused fields and avoid the cost of populating a full entity.

**Call description:**

1. Set the generic type to the field type, or to a type that the field can be converted to;
2. Specify the entity name with the method's `name` argument _（it must be the qualified name registered in the mapping file, i.e. `container.entity`; when the container name is empty, the bare entity name can be used — `Discussions.UserProfile` below is the qualified name from the forum module's mapping file）_;
3. Specify exactly one property name with the method's `schema` argument.

```csharp
var email = await this.DataAccess.SelectAsync<string>("Discussions.UserProfile",
	Condition.Equal("UserId", userId),
	"Email" // Load only the Email field, which is a string.
).FirstOrDefault();

/* Return a scalar value set(IAsyncEnumerable<uint>) */
var counts = this.DataAccess.SelectAsync<uint>("Discussions.History",
	Condition.Equal("UserId", userId),
	"ViewedCount" // Load only the ViewedCount field.
);
```

<a name="usage-query-3"></a>
#### Multi-field Query

[ThreadService.SetMostRecentThreadAsync](../../discussions/src/Services/ThreadService.cs) loads only the author fields needed to update the forum's latest-thread summary. This asynchronous example uses the `UserProfile` model:
```csharp
var userId = data.GetValue(p => p.CreatorId, this.Principal.Identity.GetIdentifier<uint>());
var user = await this.DataAccess.SelectAsync<UserProfile>(
	Condition.Equal(nameof(UserProfile.UserId), userId),
	$"{nameof(UserProfile.UserId)}," +
	$"{nameof(UserProfile.Name)}," +
	$"{nameof(UserProfile.Nickname)}," +
	$"{nameof(UserProfile.Avatar)}").FirstOrDefault();
```

`data` is the method's `IDataDictionary<Thread>` argument. The example shows the field selection; the complete service also performs authorization and related updates. Dictionaries and `ExpandoObject` are also supported for scalar projections; see [DictionaryPopulatorProvider](src/Common/DictionaryPopulatorProvider.cs). Navigation population requires a model-shaped target; do not expect nested navigation objects in dictionary projections.

<a name="usage-query-4"></a>
#### Paging query

Pass a `paging` argument to [`SelectAsync`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) to run a paged query. See [`Paging`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/Paging.cs) for the available options.

```csharp
// Page 2, 25 rows per page.
var paging = Paging.Page(2, 25);

var threads = this.DataAccess.SelectAsync<Thread>(
	Condition.Equal(nameof(Thread.SiteId), siteId) &
	Condition.Equal(nameof(Thread.ForumId), 100),
	paging
);

/*
 * After enumeration starts and Paginated completes, the paging object contains the result summary:
 * paging.Count is the total page count.
 * paging.Total is the total row count.
 */
```

<a name="usage-query-5"></a>
#### Sorting query

Pass `Sorting` values to [`SelectAsync`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) to order the result. See [Sorting](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/Sorting.cs) for details.

```csharp
var threads = this.DataAccess.SelectAsync<Thread>(
	Condition.Equal(nameof(Thread.SiteId), siteId) &
	Condition.Equal(nameof(Thread.ForumId), 100),
	Paging.Disabled, /* Disable paging for this query. You can pass a Paging object instead. */
	Sorting.Descending("TotalViews"),   // 1.Descending for TotalViews
	Sorting.Descending("TotalReplies"), // 2.Descending for TotalReplies
	Sorting.Ascending("CreatedTime")    // 3.Ascending for CreatedTime
);
```

<a name="usage-query-6"></a>
#### Navigation properties

Navigation(complex) properties are included through the `schema` argument. They support one-to-one, one-to-zero-or-one, and one-to-many relationships, and can be nested.

<a name="usage-query-7"></a>
##### One-to-One

```csharp
/*
 * 1) Thread.Post is a one-to-one navigation property associated with Post.
 *    In the mapping file this is multiplicity="!", so the generated SQL uses INNER JOIN.
 *
 * 2) Thread.MostRecentPost is a zero-or-one navigation property associated with Post.
 *    In the mapping file this is multiplicity="?", so the generated SQL uses LEFT JOIN.
 */
var thread = await this.DataAccess.SelectAsync<Thread>(
	Condition.Equal("ThreadId", 100001),
	"*,Post{*},MostRecentPost{*}"
).FirstOrDefault();
```

<a name="usage-query-8"></a>
##### One-to-Many

```csharp
/*
 * 1) ForumGroup.Forums is a one-to-many navigation property.
 *    In the mapping file this is multiplicity="*", so the navigation property is loaded by a separate SQL query.
 *
 * 2) Both one-to-one and one-to-many navigation properties can be nested.
 *
 * Note: * means all scalar properties only. Navigation properties must be named explicitly.
 */
var groups = this.DataAccess.SelectAsync<ForumGroup>(
	Condition.Equal("SiteId", siteId),
	"*,Forums{*, Moderators{*}, MostRecentThread{*, Creator{*}}}"
);
```

<a name="usage-query-9"></a>
##### Navigation constraint

For one-to-many navigation properties, you often need to filter the child collection. This is called a navigation constraint.

> A forum(`Forum`) has many forum members(`ForumUser`). Moderators are a subset of those forum members, and this subset is defined with `complexProperty/constraints` in the mapping file.
>
> In the example below, the `Users` navigation property of [Forum](https://github.com/Zongsoft/discussions/blob/main/src/Models/Forum.cs) represents all forum members, while `Moderators` represents only the members whose `IsModerator` field is `true`.

```xml
<entity name="Forum" table="Discussions_Forum">
	<key>
		<member name="SiteId" />
		<member name="ForumId" />
	</key>

	<property name="SiteId" type="uint" nullable="false" />
	<property name="ForumId" type="ushort" nullable="false" sequence="#(SiteId)" />
	<property name="GroupId" type="ushort" nullable="false" />
	<property name="Name" type="nvarchar" length="50" nullable="false" />

	<complexProperty name="Users" port="ForumUser" multiplicity="*" immutable="false">
		<link port="SiteId" />
		<link port="ForumId" />
	</complexProperty>

	<complexProperty name="Moderators" port="ForumUser:User" multiplicity="*">
		<link port="SiteId" />
		<link port="ForumId" />

		<!-- Constraints of navigation property -->
		<constraints>
			<constraint actor="Foreign" name="IsModerator" value="true" />
		</constraints>
	</complexProperty>
</entity>

<entity name="ForumUser" table="Discussions_ForumUser">
	<key>
		<member name="SiteId" />
		<member name="ForumId" />
		<member name="UserId" />
	</key>

	<property name="SiteId" type="uint" nullable="false" />
	<property name="ForumId" type="ushort" nullable="false" />
	<property name="UserId" type="uint" nullable="false" />
	<property name="Permission" type="byte" nullable="false" />
	<property name="IsModerator" type="bool" nullable="false" />

	<complexProperty name="User" port="UserProfile" multiplicity="!">
		<link port="UserId" />
	</complexProperty>
</entity>
```

<a name="usage-query-10"></a>
##### Navigation springboard

Sometimes a navigation property should return the target of another navigation property on the associated entity. The `Moderators` property above is such a case:

1. Use the colon syntax in the `port` attribute. The left side is the associated entity name, and the right side is the navigation property to jump to.

2. Add constraints to decide which associated rows are included.

> Note: A moderator does not need to expose the forum member's `Permission` field. Returning [`UserProfile`](https://github.com/Zongsoft/discussions/blob/main/src/Models/UserProfile.cs) is simpler than returning `ForumUser` and then reading `ForumUser.User`. Therefore `Moderators` uses `port="ForumUser:User"`.
>
> Compare the `Users` and `Moderators` property types in the [Forum](https://github.com/Zongsoft/discussions/blob/main/src/Models/Forum.cs) class. This member excerpt preserves the nested `Forum.ForumUser` type; constructors, equality methods, and unrelated members are omitted:

```csharp
public abstract class Forum
{
	public abstract uint SiteId { get; set; }
	public abstract ushort ForumId { get; set; }
	public abstract ushort GroupId { get; set; }
	public abstract string Name { get; set; }

	public abstract IEnumerable<ForumUser> Users { get; set; }
	public abstract IEnumerable<UserProfile> Moderators { get; set; }

	public struct ForumUser : IEquatable<ForumUser>
	{
		public uint SiteId;
		public ushort ForumId;
		public uint UserId;
		public Permission Permission;
		public bool IsModerator;

		public Forum Forum;
		public UserProfile User;
	}
}
```

```csharp
var forum = await this.DataAccess.SelectAsync<Forum>(
	Condition.Equal("SiteId", siteId) &
	Condition.Equal("ForumId", 100),
	"*, Users{*, User{Name,Email,Avatar}}, Moderators{Name,Email,Avatar}"
).FirstOrDefault();

// moderator is UserProfile (the navigation hop returns UserProfile directly, so just list its scalar properties).
foreach(var moderator in forum.Moderators)
{
	Console.Write(moderator.Name);
	Console.Write(moderator.Email);
	Console.Write(moderator.Avatar);
}

// member is ForumUser; its User navigation property leads to UserProfile.
foreach(var member in forum.Users)
{
	Console.Write(member.Permission);

	Console.Write(member.User.Name);
	Console.Write(member.User.Email);
	Console.Write(member.User.Avatar);
}
```

<a name="usage-query-11"></a>
#### Group Queries

[Grouping](../Zongsoft.Core/src/Data/Grouping.cs) describes grouping keys, aggregate functions and result aliases. It is separate from row paging and sorting.

Discussions defines per-forum counters on [Forum](../../discussions/src/Models/Forum.cs), but it does not define a `ForumStatistic` projection or a grouped statistics service. Its real workflow updates those counters in [ThreadService.SetMostRecentThreadAsync](../../discussions/src/Services/ThreadService.cs) and [PostService](../../discussions/src/Services/PostService.cs). Read that workflow before adding a separate aggregate query: stored counters and on-demand aggregates have different cost and consistency characteristics.

For grouping support itself, follow the `IDataAccess.SelectAsync` overloads accepting `Grouping` in [the public contract](../Zongsoft.Core/src/Data/IDataAccess.cs), and the selected [database driver](#driver). Choose fields from the actual mapping and a result shape matching the keys/aliases; there is no preinstalled forum statistics command to call.

<a name="usage-query-12"></a>
### Navigation condition

Navigation conditions filter by fields on associated entities.

```csharp
/*
 * Query history records whose related thread is valued,
 * and whose first or most recent view time is within the last 30 days.
 */
var histories = this.DataAccess.SelectAsync<History>(
	Condition.Equal("Thread.IsValued", true) & /* Navigation condition */
	(
		Condition.Between("FirstViewedTime", DateTime.Today.AddDays(-30), DateTime.Now) |
		Condition.Between("LastViewedTime", DateTime.Today.AddDays(-30), DateTime.Now)
	)
);

/* Same query, using Range.Timing to build the time range. */
var histories = this.DataAccess.SelectAsync<History>(
	Condition.Equal("Thread.IsValued", true) & /* Navigation condition */
	(
		Condition.Between("FirstViewedTime", Range.Timing.Last(30, 'D')) |
		Condition.Between("LastViewedTime", Range.Timing.Last(30, 'D'))
	)
);
```


<a name="usage-query-13"></a>
#### Subquery Filtering

[ThreadService.GetIsModeratorCriteria](../../discussions/src/Services/ThreadService.cs) uses a real navigation subquery to check forum membership:
```csharp
return Condition.Exists("Forum.Users",
	Condition.Equal(nameof(Forum.ForumUser.UserId), this.Principal.Identity.GetIdentifier<uint>()) &
	Condition.Equal(nameof(Forum.ForumUser.IsModerator), true));
```

`Approve`, `Visible` and other restricted methods combine this predicate with the target thread condition. For a more complex `OR` expression, read [ForumService.OnValidateAsync](../../discussions/src/Services/ForumService.cs): it calls the base validation first, then applies the whole visibility expression with `criteria.And(...)`.

🚨 Do not rewrite this as `siteCondition & publicCondition | privateCondition`: the second branch would no longer be constrained by `siteCondition`. Retain the [module validator](../../discussions/src/Data/DataValidator.cs) and service authorization when adapting these excerpts.

<a name="usage-query-14"></a>
#### Type conversion

Use a type converter when a database field type cannot be directly converted to the entity property type.

For example, the `Tags` field in the `Thread` table is `nvarchar`, but the `Tags` property of the [Thread](https://github.com/Zongsoft/discussions/blob/main/src/Models/Thread.cs) model is a **string array**. Reading and writing this property requires custom conversion. See [TagsConverter](https://github.com/Zongsoft/discussions/blob/main/src/Models/TagsConverter.cs) and the `Tags` property on [Thread](https://github.com/Zongsoft/discussions/blob/main/src/Models/Thread.cs).


<a name="usage-execute"></a>
### Execute operation

`ExecuteAsync` runs a named `command` defined in a mapping file. Use it for SQL statements, stored procedures, and commands that do not naturally map to one entity operation. The declared `mutability` is the data source selector's basis for read/write routing: `mutability="none"` selects a readable source, while `insert`, `update`, `delete`, and `upsert` select a writable source; the selector does not inspect the SQL script to determine this. A command without an explicit `mutability` is treated as writable _（`Delete|Insert|Update`）_, so there is usually no need to declare it.

Discussions currently uses entity operations rather than mapped forum-statistics commands. There is no deployed `Forum.GetStatistics` or `Forum.RefreshStatistics` in its mapping. Do not invoke those names.

[Zongsoft.Data.xsd](Zongsoft.Data.xsd) defines the mapping XML. Use `type="procedure"` and an alias naming an existing stored procedure. Declare output-parameter directions and read the supplied `Parameter` objects after completing the query or ending enumeration and awaiting enumerator disposal.

<a name="usage-delete"></a>
### Delete operation

This fragment uses the real [Post model](../../discussions/src/Models/Post.cs), with caller-supplied `postId` and an authorized `userId`; it is not an excerpt of the Discussions deletion service. Use that service in application code so its authorization and attachment-cleanup workflow remain active.

```csharp
await this.DataAccess.DeleteAsync<Post>(
	Condition.Equal("Visible", false) &
	Condition.Equal("CreatorId", userId) &
	Condition.Equal("PostId", postId)
);
```


<a name="usage-delete-cascade"></a>
#### Cascade deletion

Cascade deletion can delete child records associated through zero-or-one, one-to-one, or one-to-many navigation properties.

```csharp
await this.DataAccess.DeleteAsync<Post>(
	Condition.Equal("PostId", 100023),
	"Votes"
);
```


<a name="usage-insert"></a>
### Insert operation

```csharp
await this.DataAccess.InsertAsync("Discussions.Forum", new {
	SiteId = siteId,
	GroupId = 100,
	Name = forumName
});
```

<a name="usage-insert-options"></a>
#### Insert options

`DataInsertOptions` controls insert-only behaviors:

- `IgnoreConstraint()` ignores database constraint conflicts, such as duplicate primary keys or unique keys. The failed row is skipped instead of throwing a conflict exception.
- `Sequence(...)` controls how sequence fields are generated. Use `DataSequenceBehavior.Never` when the value is supplied by the caller and should not be generated.
- `Return(...)` asks the database to return generated values when the current driver supports returning values.
- `SuppressValidator()` disables validators registered for this insert operation.

```csharp
var count = await this.DataAccess.InsertAsync<Forum.ForumUser>(
	new {
		SiteId = siteId,
		ForumId = 100,
		UserId = 100,
		Permission = Permission.Read,
	},
	DataInsertOptions.IgnoreConstraint());
```


If you also provide a value for a sequence field, chain the sequence option:

```csharp
var options = DataInsertOptions
	.Sequence(DataSequenceBehavior.Never)
	.IgnoreConstraint();

await this.DataAccess.InsertAsync<Forum>(new {
	SiteId = siteId,
	ForumId = 100,
	GroupId = 10,
	Name = "General",
}, options);
```

<a name="usage-insert-complex"></a>
#### Associated insertion

Related one-to-one and one-to-many navigation values can be inserted together.

```csharp
var forum = Model.Build<Forum>();

forum.SiteId = siteId;
forum.GroupId = 100;
forum.Name = forumName;

forum.Users = new Forum.ForumUser[]
{
	new Forum.ForumUser { UserId = 100, IsModerator = true },
	new Forum.ForumUser { UserId = 101, Permission = Permission.Read },
	new Forum.ForumUser { UserId = 102, Permission = Permission.Write }
};

await this.DataAccess.InsertAsync(forum, "*, Users{*}");
```


<a name="usage-import"></a>
### Import operation

`ImportAsync` is used for bulk data import. Pass the target entity name, the data collection, and the member list that should be imported.

```csharp
var users = new []
{
	new { SiteId = siteId, ForumId = 100, UserId = 100, Permission = Permission.Read },
	new { SiteId = siteId, ForumId = 100, UserId = 101, Permission = Permission.Write },
};

var count = await this.DataAccess.ImportAsync(
	"Discussions.ForumUser",
	users,
	"SiteId,ForumId,UserId,Permission".Split(','));
```

Use `DataImportOptions.IgnoreConstraint()` when duplicated rows should be skipped during import. It can be chained with `Parameter(...)` when filters or services need operation-level flags:

```csharp
var options = DataImportOptions
	.IgnoreConstraint();

var count = await this.DataAccess.ImportAsync(
	"Discussions.ForumUser",
	users,
	"SiteId,ForumId,UserId,Permission".Split(','),
	options);
```

<a name="usage-update"></a>
### Update operation

```csharp
var user = Model.Build<UserProfile>();

user.UserId = 100;
user.Name = name;
user.Nickname = nickname;
user.Gender = Gender.Male;

await this.DataAccess.UpdateAsync(user);
```


<a name="usage-update-dynamic"></a>
#### Anonymous class

The value to write can be an anonymous object, dynamic object _(`ExpandoObject`)_, or dictionary _(`IDictionary`, `IDictionary<string, object>`)_.

```csharp
await this.DataAccess.UpdateAsync<UserProfile>(
	new {
		Name=name,
		Nickname=nickname,
		Gender=Gender.Male,
	},
	Condition.Equal("UserId", 100)
);
```

<a name="usage-update-schema"></a>
#### Exclude fields

Use `schema` to choose the fields to update, or to exclude fields.

```csharp
/*
 * Only Name and Gender are updated.
 * Other fields are ignored even if their values changed.
 */
await this.DataAccess.UpdateAsync<UserProfile>(
	user,
	"Name, Gender"
);

/*
 * * allows all fields, but CreatorId and CreatedTime are excluded.
 * Even if user contains values for these two properties, no SET clauses are generated for them.
 */
await this.DataAccess.UpdateAsync<UserProfile>(
	user,
	"*, !CreatorId, !CreatedTime"
);
```

<a name="usage-update-complex"></a>
#### Associated Updates

The thread and content-post models can be updated in one asynchronous operation:
```csharp
public async ValueTask<bool> ApproveAsync(ulong threadId, CancellationToken cancellation = default)
{
	var criteria = Condition.Equal(nameof(Thread.ThreadId), threadId) &
	               Condition.Equal(nameof(Thread.Approved), false) &
	               GetIsModeratorCriteria();

	return await this.DataAccess.UpdateAsync<Thread>(new
	{
		Approved = true,
		ApprovedTime = DateTime.Now,
		Post = new
		{
			Approved = true,
		}
	}, criteria, "*,Post{Approved}", cancellation) > 0;
}
```

`GetIsModeratorCriteria()` is the private helper in the same class, shown under [subquery filtering](#usage-query-13). The schema `*,Post{Approved}` explicitly includes the post's approval field. Do not remove the moderator predicate or the module's validator. One-to-many associated writes use upsert semantics; the exact SQL belongs to the selected driver.

<a name="usage-upsert"></a>
### Upsert Operations

Upsert inserts a row when its key does not exist and updates it when it does. Browsing history can use the user/thread composite key, record `LastViewedTime`, and increment `ViewedCount`.


For the upsert contract and driver restrictions, see [IDataAccess](../Zongsoft.Core/src/Data/IDataAccess.cs), [DataUpsertOptions](../Zongsoft.Core/src/Data/DataUpsertOptions.cs) and the selected driver's README. Key matching, sequence generation, defaults on insertion and updates to existing rows must be considered separately.

<a name="usage-returning"></a>
### Returning values

Delete, insert, update, and upsert options can request returned values from the database provider. This is useful for deleted file paths, identity values, generated values, and updated counters. The feature is available only when the current driver supports returning values.

Returned rows describe the primary entity; the affected-row count can also include associated writes. Results are appended to the existing options after all statements succeed and before the completion callbacks. An owned transaction must commit first; within a caller-owned transaction, returned values remain provisional until the caller commits. Execution failures or cancellation before commit do not append rows. Row order is unspecified.

SQL Server and PostgreSQL 18 or later support both older and newer update values. SQLite and DuckDB provide newer update values and older delete values; unsupported older update/upsert columns are absent (`TryGetValue` returns `false`). Associated updates on SQLite and DuckDB cannot also assign link anchors. MySQL supports update/delete returning under the [driver's transaction and key requirements](drivers/mysql/README.md#returning-values).

```csharp
var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(Thread.TotalViews));

await this.DataAccess.UpdateAsync<Thread>(
	new {
		TotalViews = Operand.Field(nameof(Thread.TotalViews)) + 1,
	},
	Condition.Equal(nameof(Thread.ThreadId), threadId),
	options);

if(options.Returning.Rows.Count > 0 &&
	 options.Returning.Rows[0].TryGetValue(nameof(Thread.TotalViews), ReturningKind.Newer, out var value))
{
	var totalViews = Convert.ToInt64(value);
}
```

For simple counters, the `IncreaseAsync` and `DecreaseAsync` helpers wrap this pattern:

```csharp
var totalViews = await this.DataAccess.IncreaseAsync<Thread>(
	nameof(Thread.TotalViews),
	Condition.Equal(nameof(Thread.ThreadId), threadId));
```

The following adaptation uses the real PostAttachment model to inspect returned IDs only; it does not implement file deletion:

```csharp
var options = DataDeleteOptions.Return(nameof(PostAttachment.AttachmentId));

await this.DataAccess.DeleteAsync<PostAttachment>(
	Condition.Equal(nameof(PostAttachment.PostId), postId),
	options);

foreach(var row in options.Returning.Rows)
{
	if(row.TryGetValue(nameof(PostAttachment.AttachmentId), out var value))
		Console.WriteLine(value);
}
```

<a name="usage-options"></a>
### Options, events, and transactions

Every data access method accepts an option object. Options are intentionally small, but they are important because validators, filters, events, and service hooks all receive the same option instance.

Common option members:

| Option member | Applies to | Description |
| --- | --- | --- |
| `Parameter(...)` / `new DataXxxOptions(parameters)` | All option types | Adds operation-level parameters. These parameters are for validators, filters, callbacks, and service hooks. They are not SQL parameters; mapped commands use `IEnumerable<Parameter>` for SQL/stored procedure parameters. |
| `SuppressValidator()` | Select, Exists, Aggregate, Delete, Insert, Update, Upsert | Skips registered data validators for this operation. Use it only when the caller has already enforced the scope or the operation is an internal maintenance operation. |
| `Return(...)` | Delete, Insert, Update, Upsert | Requests returned values. Insert returns newer values; delete returns older values; update/upsert can request `ReturningKind.Newer` or `ReturningKind.Older`. |
| `IgnoreConstraint()` | Insert, Import, Upsert | Skips rows that conflict with database constraints when the driver supports it. |
| `Sequence(...)` | Insert, Upsert | Controls sequence generation for sequence fields. |

Read option members:

| Option | Description |
| --- | --- |
| `DataSelectOptions.Distinct()` | Generates a distinct query. It is especially useful for scalar or small projection queries. |
| `DataSelectOptions.SuppressLazy(false)` | Suppresses lazy loading of navigation collections. Pass `true` when the query should also be distinct. |
| `DataExistsOptions.SuppressValidator()` | Runs an existence check without validators. |
| `DataAggregateOptions.SuppressValidator()` | Runs a count/sum/aggregate query without validators. |

Write option members:

| Option | Description |
| --- | --- |
| `DataInsertOptions.Sequence(DataSequenceBehavior.Auto)` | Uses the provided sequence value when present; otherwise generates one. This is the default behavior. |
| `DataInsertOptions.Sequence(DataSequenceBehavior.Alway)` | Always lets the sequencer generate the value. |
| `DataInsertOptions.Sequence(DataSequenceBehavior.Never)` | Never generates the sequence value; use the value supplied by the caller. |
| `DataUpdateOptions.SuppressValidator(UpdateBehaviors.PrimaryKey)` | Allows primary key fields to be included in an update. This is for repair or migration code, not normal business updates. |
| `DataUpsertOptions.IgnoreConstraint()` | Applies the same conflict-ignore intent to upsert statements. |

A real operation parameter is `"Thread"`, read by [PostService.OnInsert](../../discussions/src/Services/PostService.cs). The service accepts either a `Thread` model or `IDataDictionary<Thread>`, resolves `ForumService` through its service provider, then decides whether the new post is approved. Read the complete method for null handling and file-content storage. These options are operation context, not SQL parameters; there is no built-in `SkipSynchronization` or `SkipAudit` switch.

Before/after callbacks, IDataAccess events and filters are awaitable. Event handlers accept (sender, args, cancellation), return ValueTask, and complete in subscription order. Use asynchronous enumeration and Pageable.FilterAsync/MapAsync for result processing while retaining pagination. IEnumerable navigation members contain loaded records; consume IAsyncEnumerable navigation members asynchronously.

`InsertManyAsync` and `UpsertManyAsync` accept in-memory collections and `IAsyncEnumerable<T>`. Each input is enumerated once in batches of up to 256 records, with smaller command batches when required by driver parameter limits. Ordinary relational inserts use multiple `VALUES` rows; constraint suppression and upserts use the database dialect. The result accumulates affected rows, including cascading writes. All batches share the operation's session. When transactions are supported and enabled, completion commits all batches, while failure or cancellation rolls back earlier batches.

Manage transactions with `await using` and await completion. For example, insert an order and its lines in one transaction:

```csharp
await using var transaction = new Transaction();
await accessor.InsertAsync("Orders", order, cancellation);
await accessor.InsertManyAsync("OrderLines", lines, cancellation);
await transaction.CommitAsync(cancellation);
```

`accessor` is an IDataAccess instance; the caller supplies the order and lines. Disposal rolls back an uncommitted transaction. Transactions do not roll back external file or object-storage writes. Keep authorization and validators enabled in normal application calls.

<a name="usage-other"></a>
### Other

For more details, see the related documentation for read/write splitting, inherited tables, schemas, mapping files, filters, validators, type conversion, and data isolation.

If this project is useful to you, please **Watch**, **Fork**, or **Star** it.

<a name="performance"></a>
## Performance

Zongsoft.Data aims for balanced performance, maintainability, and usability instead of optimizing for a single benchmark. For an ORM data access engine, performance mainly depends on:

1. Generating clean and efficient SQL, using database-specific syntax when it helps;
2. Populating models/entities efficiently from query results;
3. Avoiding reflection on hot paths and caching parsed expression trees.

Because data relationships are described declaratively, the engine can turn user intent into expression trees and then into provider-specific SQL. This keeps application code focused and leaves more room for provider-level optimization.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

The manifest mounts the Data environment and driver collection. Add a database-driver plugin, a named connection and the application's `.mapping`; installing the engine alone neither selects a database nor creates tables.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Data` | [Zongsoft.Data.plugin](src/Zongsoft.Data.plugin) |
| File copying and dependencies | [Zongsoft.Data.deploy](src/Zongsoft.Data.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Data.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
