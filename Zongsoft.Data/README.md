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
ClickHouse | [/drivers/clickhouse](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/clickhouse) | _**A**vailable_ |
InfluxDB | [/drivers/influx](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/influx) | _**A**vailable_ |
TDengine | [/drivers/tdengine](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/tdengine) | _**A**vailable_ |

> Tip: If you need a driver that is not listed here or commercial support, please contact us ([zongsoft@qq.com](mailto:zongsoft@qq.com)).


<a name="environment"></a>
### Environment

- .NET 8.0
- .NET 9.0
- .NET 10.0

<a name="download"></a>
## Download

### Source code compilation

For source builds, we recommend creating a **_Zongsoft_** directory outside the system partition and cloning [Guidelines](https://github.com/Zongsoft/guidelines), [Zongsoft.Core](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core), and [Zongsoft.Data](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data) into it.

<a name="schema"></a>
## The data schema

The data **schema** is a DSL(**D**omain **S**pecific **L**anguage) that describes which fields are queried or written _(**D**elete/**I**nsert/**U**pdate/**U**psert)_. It looks similar to [GraphQL](https://graphql.org/), but it does not need a server-side GraphQL definition. It is used to choose fields, include navigation properties, and control cascade scopes.

The `schema` argument in a data access method is the schema text. The [ISchema](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/ISchema.cs) interface represents the parsed schema expression.

<a name="schema-syntax"></a>
### Schema Syntax

```
schema ::=
{
    * |
    ! |
    !identifier |
    identifier[paging][sorting]["{"schema [,...n]"}"]
} [,...n]

identifier ::= [_A-Za-z][_A-Za-z0-9]*
number ::= [0-9]+
pageIndex ::= number
pageSize ::= number

paging ::= ":"{
    *|
    pageIndex[/pageSize]
}

sorting ::=
"("
    {
        [~|!]identifier
    }[,...n]
")"
```

<a name="schema-overview"></a>
#### Schema Overview

- Asterisk(`*`): includes all scalar properties. Navigation properties are not included unless you name them explicitly.

- Exclamation(`!`): excludes fields. A single `!` excludes the previous definition; `!Name` excludes the named property.

<a name="schema-sample"></a>
### Sample description

```graphql
*, !CreatorId, !CreatedTime
```
> **Note:** All scalar properties except `CreatorId` and `CreatedTime`.

```graphql
*, Creator{*}
```
> **Note:** All scalar properties plus the `Creator` navigation property, including all scalar properties of `Creator`.

```graphql
*, Creator{Name,FullName}
```
> **Note:** All scalar properties plus the `Creator` navigation property, but only `Name` and `FullName` are loaded for `Creator`.

```graphql
*, Users{*}
```
> **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, without sorting or paging.

```graphql
*, Users:1{*}
```
> **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, paged as page 1 with page size 1. Use `Users:1/?{*}` for page 1 with the default page size.

```graphql
*, Users:1/20{*}
```
> **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, paged as page 1 with 20 rows per page.

```graphql
*, Users:1/20(Grade,~CreatedTime){*}
```
> **Note:** All scalar properties plus the `Users` collection navigation property _(one-to-many)_, sorted by `Grade` ascending and `CreatedTime` descending, then paged as page 1 with 20 rows per page.


<a name="mapping"></a>
## Mapping file

A data mapping file is an XML file with the `.mapping` extension. It defines the metadata for entities, tables, fields, keys, and navigation relationships. **Do not** put all metadata for a large application into one file; each business module should have its own mapping file.

The [Zongsoft.Data.xsd](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.xsd) XML Schema file provides IntelliSense and validation for hand-written mapping files.

The mapping file root is `schema`, and each `container` represents one metadata namespace. Most business modules have one container whose `name` matches the module name.

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
            <property name="GroupId" type="ushort" nullable="false" sortable="true" />
            <property name="Name" type="string" length="50" nullable="false" />
            <complexProperty name="Users" port="ForumUser" multiplicity="*" immutable="false">
                <link port="SiteId" />
                <link port="ForumId" />
            </complexProperty>
        </entity>
    </container>
</schema>
```

Common mapping elements:

- `entity` maps an application entity to a table. `table` is the physical table name, `inherits` points to a parent entity, `driver` limits the entity to a specific data driver, and `immutable="true"` makes the entity read-only except for insert operations.
- `property` maps a scalar member to a field. Important attributes include `type`, `field`, `nullable`, `length`, `precision`, `scale`, `default`, `sequence`, `sortable`, and `immutable`.
- `sequence="*"` means the database built-in identity/sequence is used. `sequence="#"` means the Zongsoft default external sequencer is used. `sequence="#Name"` means a named external sequencer. `sequence="#(ParentId)"` means an external sequencer grouped by the specified reference property.
- `complexProperty` defines a navigation property. Its `port` points to the target entity, or to a target entity's navigation property such as `ForumUser:User`. `multiplicity` supports `?`, `!`, and `*`; `link` maps foreign key properties to the current entity, and `constraints` add fixed navigation filters.
- `command` defines a named SQL command or stored procedure. Commands are executed through `Execute`, `Execute<T>`, or `ExecuteScalar`.

```xml
<command name="Forum.GetStatistics" type="Text" mutability="None">
    <parameter name="SiteId" type="uint" />
    <parameter name="ForumId" type="ushort" />
    <script driver="MySql"><![CDATA[
        SELECT TotalThreads, TotalPosts
        FROM Discussions_Forum
        WHERE SiteId=@SiteId AND ForumId=@ForumId
    ]]></script>
</command>
```


> **Enable XML IntelliSense for mapping files:**
>
> **Method 1：** Add an XML file named "`{module}.mapping`" to the business module project(for example: [`Zongsoft.Security.mapping`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Security/src/Zongsoft.Security.mapping) or [`Zongsoft.Discussions.mapping`](https://github.com/Zongsoft/discussions/blob/main/src/Zongsoft.Discussions.mapping)). Open the mapping file in **V**isual **S**tudio, choose "XML" -> "Schemas", click "Add", and select [Zongsoft.Data.xsd](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.xsd).
>
> **Method 2：** Copy [Zongsoft.Data.xsd](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.xsd) to the XML Schemas template directory in Visual Studio, for example:
> - **V**isual **S**tudio 2019 _(Enterprise Edition)_ <br />
> 	`C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Xml\Schemas`


> Although some developers like generating mapping files, we recommend writing them by hand:
>
> - Data structures and relationships are the foundation of a system. Database tables are their physical form, and mapping files describe how application entities match those tables.
> - Mapping files should be maintained by the system architect or module owner. Settings such as `inherits`, `immutable`, `sortable`, `sequence`, and navigation properties directly affect application code.


<a name="connection"></a>
## Connection Settings

The connection setting name must match the `DataAccess` name. One `DataAccess` can have multiple data sources. Use `#` to separate the `DataAccess` name from the data source name; for example, `Discussions#master` and `Discussions#slave_1`. This is mainly used for read/write splitting.

> - **MySQL** connection string reference: https://mysqlconnector.net/connection-options/
> - **ADO.NET** connection string reference: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/connection-string-syntax

- Configuration for a single data source:
```xml
<configuration>
    <option path="/Data">
        <connectionSettings default="Discussions">
            <connectionSetting connectionSetting.name="Discussions" driver="MySql"
                               value="server=127.0.0.1;userName=MyName;password=xxxxxx;database=MyDatabase;charset=utf8mb4" />
        </connectionSettings>
    </option>
</configuration>
```

- Configuration of multiple data sources(read-write separation mode):
```xml
<configuration>
    <option path="/Data">
        <connectionSettings>
            <connectionSetting connectionSetting.name="Discussions#master" driver="MySql" mode="WriteOnly"
                               value="server=192.168.0.10;userName=MyName;password=xxxxxx;database=MyDatabase;charset=utf8mb4" />
            <connectionSetting connectionSetting.name="Discussions#slave_1" driver="MySql" mode="ReadOnly"
                               value="server=192.168.0.11;userName=MyName;password=xxxxxx;database=MyDatabase;charset=utf8mb4" />
            <connectionSetting connectionSetting.name="Discussions#slave_2" driver="MySql" mode="ReadOnly"
                               value="server=192.168.0.12;userName=MyName;password=xxxxxx;database=MyDatabase;charset=utf8mb4" />
            <connectionSetting connectionSetting.name="Discussions#slave_3" driver="MySql" mode="ReadOnly"
                               value="server=192.168.0.13;userName=MyName;password=xxxxxx;database=MyDatabase;charset=utf8mb4" />
        </connectionSettings>
    </option>
</configuration>
```


<a name="usage"></a>
## Usages

All data operations go through the [`Zongsoft.Data.IDataAccess`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) interface in [Zongsoft.Core](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core). It supports these operations:

- `int Count(...)`
- `TValue? Aggregate(...)`
- `bool Exists(...)`
- `IEnumerable<T> Execute<T>(...)` `object ExecuteScalar(...)`
- `int Import(...)`
- `int Delete(...)`
- `int Insert(...)` `int InsertMany(...)`
- `int Update(...)` `int UpdateMany(...)`
- `int Upsert(...)` `int UpsertMany(...)`
- `IEnumerable<T> Select<T>(...)`

**Reminder:**
> The following examples are based on the [Zongsoft.Discussions](https://github.com/Zongsoft/discussions) open source project, a complete .NET backend for a community forum. Before reading the examples, it is helpful to read its [database table design document](https://github.com/Zongsoft/discussions/blob/main/database/Zongsoft.Discussions.md).


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

- Field reference:
```csharp
var forums = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("MostRecentThreadAuthorId", Operand.Field("MostRecentPostAuthorId"))
);
```

- Constant value:
```csharp
/* The following two calls are equivalent. */
this.DataAccess.Update<OrderDetail>(
    new {
        Discount = Operand.Constant(10)
    },
    Condition.Between("Quantity", Range.Create(100, 200))
);

this.DataAccess.Update<OrderDetail>(
    new {
        Discount = 10
    },
    Condition.Between("Quantity", 100, 200)
);
```

- Unary operators:
```csharp
this.DataAccess.Update<OrderDetail>(
    new {
        Discount = -Operand.Field("Discount")
    },
    Condition.LessThan("Discount", 0)
);

this.DataAccess.Update<Thread>(
    new {
        Visible = !Operand.Field("Visible")
    },
    Condition.Equal("ForumId", 404)
);
```

- Binary operators:
```csharp
/* Increment */
this.DataAccess.Update<Thread>(
    new {
        TotalReplies = Operand.Field("TotalReplies") + 1
    },
    Condition.Equal("ThreadId", 404)
);

/* Arithmetic */
this.DataAccess.Update<OrderDetail>(
    new {
        Amount = Operand.Field("UnitPrice") * Operand.Field("Quantity") - Operand.Field("Discount")
    },
    Condition.Equal("OrderId", 404)
);

/* Bitwise AND */
this.DataAccess.Select<User>(
    Condition.Equal(Operand.Field("Flags") & 0x74, 0x74)
);
```

- Function call:
```csharp
this.DataAccess.Update<OrderDetail>(
    new {
        Quantity = Operand.Function("Abs", Operand.Field("Quantity")),
        UnitPrice = Operand.Function("Abs", Operand.Field("UnitPrice"))
    },
    Condition.Equal("OrderId", 404)
);
```

- Aggregate value:
```csharp
/* The following two calls are equivalent. */
this.DataAccess.Update<Order>(
    new {
        Amount = Operand.Aggregate(DataAggregateFunction.Sum, "Details.Amount")
    },
    Condition.Equal("OrderId", 404)
);

this.DataAccess.Update<Order>(
    new {
        Amount = Operand.Sum("Details.Amount")
    },
    Condition.Equal("OrderId", 404)
);
```

```csharp
/* The following three calls are equivalent. */
this.DataAccess.Update<Order>(
    new {
        Amount = Operand.Function("COALESCE",
            Operand.Aggregate(DataAggregateFunction.Sum, "Details.Amount"), Operand.Constant(0))
            + Operand.Field("Surcharge")
            + Operand.Field("Taxes")
            - Operand.Field("Discount")
    },
    Condition.Equal("OrderId", 404)
);

this.DataAccess.Update<Order>(
    new {
        Amount = Operand.IsNull(Operand.Sum("Details.Amount"), 0)
            + Operand.Field("Surcharge")
            + Operand.Field("Taxes")
            - Operand.Field("Discount")
    },
    Condition.Equal("OrderId", 404)
);

this.DataAccess.Update<Order>(
    new {
        Amount = Operand.Sum("Details.Amount", 0)
            + Operand.Field("Surcharge")
            + Operand.Field("Taxes")
            - Operand.Field("Discount")
    },
    Condition.Equal("OrderId", 404)
);
```

<a name="condition"></a>
### Condition

`Condition` is the common expression object used by `Select`, `Exists`, `Aggregate`, `Delete`, `Update`, and `Upsert`. It supports equality, comparison, `Like`, `Between`, `In`, `NotIn`, `Exists`, and `NotExists`. Use `&` and `|` to combine conditions.

```csharp
var criteria =
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Like("Title", "%Zongsoft%") &
    Condition.Between("CreatedTime", Range.Create(DateTime.Today.AddDays(-7), DateTime.Today)) &
    (
        Condition.Equal("IsPinned", true) |
        Condition.Equal("IsValued", true)
    );

var threads = this.DataAccess.Select<Thread>(criteria, "ThreadId,Title,CreatedTime");
```

For search DTOs, use `Criteria.Transform(...)` to turn changed model members into conditions. `ConditionAttribute` can rename the target member, force an operator, ignore empty values, or plug in a custom converter.

```csharp
public abstract class ThreadCriteria : CriteriaBase
{
    public abstract uint? SiteId { get; set; }

    [Condition(ConditionOperator.Like)]
    public abstract string Title { get; set; }

    [Condition(ConditionOperator.Between, nameof(Thread.CreatedTime))]
    public abstract Range<DateTime>? CreatedTime { get; set; }
}

var criteria = Criteria.Transform<ThreadCriteria>(
    "siteId:1+title:%Zongsoft%+createdTime:(2026-01-01,2026-12-31)"
);

var threads = this.DataAccess.Select<Thread>(criteria);
```

<a name="usage-query"></a>
### Query operation

<a name="usage-query-1"></a>
#### Basic query

- By default, all scalar fields are returned. Use the `schema` argument to choose a smaller field set.
- Query results are lazy. Actual data access starts when you enumerate the result or call LINQ methods such as `ToList()` or `First()`.

**Note:** Queries are not paged by default. Avoid calling `ToList()` or `ToArray()` on a large result set unless you really need all rows in memory.

```csharp
// Query all scalar fields that match the condition(lazy loading).
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("Visible", true));

// Query one entity and load only selected fields.
var forum = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("ForumId", 100),
    "SiteId,ForumId,Name,Description,CoverPicturePath").FirstOrDefault();
```

<a name="usage-query-exists"></a>
#### Exists and aggregate query

Use `Exists` when you only need to know whether a row exists. Use `Count`, `Sum`, `Average`, `Maximum`, `Minimum`, `Median`, `Deviation`, or `Variance` extension methods when you only need aggregate values.

```csharp
var exists = this.DataAccess.Exists<Thread>(
    Condition.Equal(nameof(Thread.ThreadId), threadId) &
    Condition.Equal(nameof(Thread.Visible), true));

var totalThreads = this.DataAccess.Count<Thread>(
    Condition.Equal(nameof(Thread.ForumId), forumId));

var totalViews = this.DataAccess.Sum<Thread, long>(
    nameof(Thread.TotalViews),
    Condition.Equal(nameof(Thread.ForumId), forumId));
```

<a name="usage-query-2"></a>
#### Scalar value query

Scalar queries return a single field value. They avoid loading unused fields and avoid the cost of populating a full entity.

**Call description:**

1. Set the generic type to the field type, or to a type that the field can be converted to;
2. Specify the entity name with the method's `name` argument;
3. Specify exactly one property name with the method's `schema` argument.

```csharp
var email = this.DataAccess.Select<string>("UserProfile",
    Condition.Equal("UserId", this.User.UserId),
    "Email" // Load only the Email field, which is a string.
).FirstOrDefault();

/* Return a scalar value set(IEnumerable<uint>) */
var counts = this.DataAccess.Select<uint>("History",
    Condition.Equal("UserId", this.User.UserId),
    "ViewedCount" // Load only the ViewedCount field.
);
```

<a name="usage-query-3"></a>
#### Multi-field query

Multi-field queries load several fields and can return many target shapes: class, interface, struct, dynamic object(`ExpandoObject`), or dictionary.

```csharp
struct UserToken
{
    public uint UserId;
    public string Name;
}

/*
 * Note: The schema argument can be omitted or left empty here.
 * The engine uses the intersection between the entity metadata and the target type members.
 */
var tokens = this.DataAccess.Select<UserToken>(
    "UserProfile",
    Condition.Equal("SiteId", this.User.SiteId),
    "UserId, Name"
);
```

```csharp
/*
 * When the target type name differs from the entity name,
 * use ModelAttribute to specify the mapped entity name.
 */
[Zongsoft.Data.Model("UserProfile")]
struct UserToken
{
    public uint UserId;
    public string Name;
}

// Because the target type declares the mapped entity name, the name argument can be omitted.
var tokens = this.DataAccess.Select<UserToken>(
    Condition.Equal("SiteId", this.User.SiteId)
);
```

```csharp
/*
 * 1) The generic type specifies that each row is returned as a dictionary.
 * 2) The schema argument selects the returned fields. If omitted or set to *, all fields are returned.
 */
var items = this.DataAccess.Select<IDictionary<string, object>>(
    "UserProfile",
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.GreaterThan("TotalThreads", 0),
    "UserId,Name,TotalThreads,TotalPosts");

foreach(var item in items)
{
    item.TryGetValue("UserId", out var userId); // true
    item.TryGetValue("Name", out var name);     // true
    item.TryGetValue("Avatar", out var avatar); // false
    item.TryGetValue("TotalThreads", out var totalThreads); // true
}
```

```csharp
/*
 * The generic type specifies ExpandoObject, so each row can be accessed dynamically.
 */
var items = this.DataAccess.Select<System.Dynamic.ExpandoObject>("UserProfile");

foreach(dynamic item in items)
{
    Console.WriteLine(item.UserId); // OK
    Console.WriteLine(item.Name);   // OK
    Console.WriteLine(item.Fake);   // Compiled successfully, but runtime error
}
```

<a name="usage-query-4"></a>
#### Paging query

Pass a `paging` argument to [`Select`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) to run a paged query. See [`Paging`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/Paging.cs) for the available options.

```csharp
// Page 2, 25 rows per page.
var paging = Paging.Page(2, 25);

var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
    Condition.Equal(nameof(Thread.ForumId), 100),
    paging
);

/*
 * After the query returns, the paging object contains the result summary:
 * paging.Count is the total page count.
 * paging.Total is the total row count.
 */
```

<a name="usage-query-5"></a>
#### Sorting query

Pass `Sorting` values to [`Select`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) to order the result. See [Sorting](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/Sorting.cs) for details.

```csharp
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
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
var thread = this.DataAccess.Select<Thread>(
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
var groups = this.DataAccess.Select<ForumGroup>(
    Condition.Equal("SiteId", this.User.SiteId),
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
	<property name="Name" type="string" length="50" nullable="false" />

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
> Compare the `Users` and `Moderators` property types in the [Forum](https://github.com/Zongsoft/discussions/blob/main/src/Models/Forum.cs) class:

```csharp
public abstract class Forum
{
    public abstract uint SiteId { get; set; }
    public abstract ushort ForumId { get; set; }
    public abstract ushort GroupId { get; set; }
    public abstract string Name { get; set; }

    public abstract IEnumerable<ForumUser> Users { get; set; }
    public abstract IEnumerable<UserProfile> Moderators { get; set; }
}

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
```

```csharp
var forum = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("ForumId", 100),
    "*, Users{*}, Moderators{*, User{*}}"
).FirstOrDefault();

// moderator is UserProfile.
foreach(var moderator in forum.Moderators)
{
    Console.Write(moderator.Name);
    Console.Write(moderator.Email);
    Console.Write(moderator.Avatar);
}

// member is ForumUser.
foreach(var member in forum.Users)
{
    Console.Write(member.Permission);

    Console.Write(member.User.Name);
    Console.Write(member.User.Email);
    Console.Write(member.User.Avatar);
}
```

<a name="usage-query-11"></a>
#### Group query

Grouping queries support aggregate functions for relational databases.

```csharp
struct ForumStatistic
{
    public uint SiteId;
    public ushort ForumId;
    public int TotalThreads;
    public int TotalViews;
    public int TotalPosts;
    public Forum Forum;
}

var statistics = this.DataAccess.Select<ForumStatistic>(
    "Thread",
    Grouping
        .Group("SiteId", "ForumId")
        .Count("*", "TotalThreads")
        .Sum("TotalViews")
        .Sum("TotalPosts"),
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("Visible", true),
    "Forum{Name}"
);
```

The query above roughly generates SQL like this:

```sql
SELECT
    tt.*,
    f.Name AS 'Forum.Name'
FROM
(
    SELECT
        t.SiteId,
        t.ForumId,
        COUNT(*) AS 'TotalThreads',
        SUM(t.TotalViews) AS 'TotalViews',
        SUM(t.TotalPosts) AS 'TotalPosts'
    FROM Thread AS t
    WHERE t.SiteId = @p1 AND
          t.Visible = @p3
    GROUP BY t.SiteId, t.ForumId
) AS tt
    LEFT JOIN Forum f ON
        tt.SiteId = f.SiteId AND
        tt.ForumId = f.ForumId;
```

<a name="usage-query-12"></a>
### Navigation condition

Navigation conditions filter by fields on associated entities.

```csharp
/*
 * Query history records whose related thread is valued,
 * and whose first or most recent view time is within the last 30 days.
 */
var histories = this.DataAccess.Select<History>(
    Condition.Equal("Thread.IsValued", true) & /* Navigation condition */
    (
        Condition.Between("FirstViewedTime", DateTime.Today.AddDays(-30), DateTime.Now) |
        Condition.Between("LastViewedTime", DateTime.Today.AddDays(-30), DateTime.Now)
    )
);

/* Same query, using Range.Timing to build the time range. */
var histories = this.DataAccess.Select<History>(
    Condition.Equal("Thread.IsValued", true) & /* Navigation condition */
    (
        Condition.Between("FirstViewedTime", Range.Timing.Last(30, 'D')) |
        Condition.Between("LastViewedTime", Range.Timing.Last(30, 'D'))
    )
);
```

The query above roughly generates SQL like this:

```sql
SELECT h.*
FROM History h
    LEFT JOIN Thread t ON
        t.ThreadId = h.ThreadId
WHERE t.IsValued = @p1 AND
    (
        h.FirstViewedTime BETWEEN @p2 AND @p3 OR
        h.LastViewedTime BETWEEN @p4 AND @p5
    );
```

<a name="usage-query-13"></a>
#### Subquery filtering

Filtering a one-to-many navigation property is expressed with the `Exists` operator and becomes a SQL subquery.

> The following query gets forums in the current user's site. `Internal` and `All` forums are included directly. For `Specified` forums, the current user must be a moderator or have member permissions.

```csharp
var forums = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.In("Visibility", Visibility.Internal, Visibility.All) |
    (
        Condition.Equal("Visibility", Visibility.Specified) &
        Condition.Exists("Users",
                          Condition.Equal("UserId", this.User.UserId) &
                          (
                              Condition.Equal("IsModerator", true) |
                              Condition.NotEqual("Permission", Permission.None)
                          )
                        )
    )
);
```

The query above roughly generates SQL like this:

```sql
SELECT t.*
FROM Forum t
WHERE
    t.SiteId = @p1 AND
    t.Visibility IN (@p2, @p3) OR
    (
        t.Visibility = @p4 AND
        EXISTS
        (
                SELECT u.SiteId, u.ForumId, u.UserId
                FROM ForumUser u
                WHERE u.SiteId = t.SiteId AND
                      u.ForumId = t.ForumId AND
                      u.UserId = @p5 AND
                      (
                          u.IsModerator = @p6 OR
                          u.Permission != @p7
                      )
        )
    );
```

<a name="usage-query-14"></a>
#### Type conversion

Use a type converter when a database field type cannot be directly converted to the entity property type.

For example, the `Tags` field in the `Thread` table is `nvarchar`, but the `Tags` property of the [Thread](https://github.com/Zongsoft/discussions/blob/main/src/Models/Thread.cs) model is a **string array**. Reading and writing this property requires custom conversion. See [TagsConverter](https://github.com/Zongsoft/discussions/blob/main/src/Models/TagsConverter.cs) and the `Tags` property on [Thread](https://github.com/Zongsoft/discussions/blob/main/src/Models/Thread.cs).


<a name="usage-execute"></a>
### Execute operation

`Execute` runs a named `command` defined in a mapping file. Use it for SQL statements, stored procedures, and commands that do not naturally map to one entity operation. `mutability="None"` is treated as read-only, while `Insert`, `Update`, `Delete`, and `Upsert` are treated as write commands and participate in read/write source selection.

```csharp
public sealed class ForumStatistics
{
    public int TotalThreads { get; set; }
    public int TotalPosts { get; set; }
}

var rows = this.DataAccess.Execute<ForumStatistics>(
    "Forum.GetStatistics",
    new []
    {
        new Parameter("SiteId", this.User.SiteId),
        new Parameter("ForumId", forumId),
    });

var statistics = rows.FirstOrDefault();
```

For stored procedures, define `type="Procedure"` in the mapping file. Output and return parameters are written back to the supplied `Parameter` objects after execution.

```xml
<command name="Forum.RefreshStatistics" alias="Discussions_Forum_RefreshStatistics" type="Procedure" mutability="Update">
    <parameter name="SiteId" type="uint" />
    <parameter name="ForumId" type="uint" />
    <parameter name="Total" type="int" direction="out" />
</command>
```

```csharp
var total = Parameter.Output("Total");

this.DataAccess.Execute(
    "Forum.RefreshStatistics",
    new []
    {
        new Parameter("SiteId", this.User.SiteId),
        new Parameter("ForumId", forumId),
        total,
    });

var totalValue = total.Value;
```

<a name="usage-delete"></a>
### Delete operation

```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("Visible", false) &
    Condition.Equal("Creator.Email", "zongsoft@qq.com")
);
```

The delete above roughly generates SQL like this:

```sql
DELETE t
FROM Post AS t
    LEFT JOIN UserProfile AS u ON
        t.CreatorId = u.UserId
WHERE t.Visible=0 AND
        u.Email='zongsoft@qq.com';
```

<a name="usage-delete-cascade"></a>
#### Cascade deletion

Cascade deletion can delete child records associated through zero-or-one, one-to-one, or one-to-many navigation properties.

```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("PostId", 100023),
    "Votes"
);
```

The delete above roughly generates SQL like this(_SQL Server_):

```sql
CREATE TABLE #TMP
(
    PostId bigint
);

/* Delete the master row and write the associated key values to a temporary table. */
DELETE FROM Post
OUTPUT DELETED.PostId INTO #TMP
WHERE PostId=@p1;

/* Delete child rows by using the keys captured from the deleted master row. */
DELETE FROM PostVoting
WHERE PostId IN
(
    SELECT PostId FROM #TMP
);
```

<a name="usage-insert"></a>
### Insert operation

```csharp
this.DataAccess.Insert("Forum", new {
    SiteId = this.User.SiteId,
    GroupId = 100,
    Name = "xxxx"
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
var count = this.DataAccess.Insert<ForumUser>(
    new {
        SiteId = this.User.SiteId,
        ForumId = 100,
        UserId = 100,
        Permission = Permission.Read,
    },
    DataInsertOptions.IgnoreConstraint());
```

The insert above roughly generates SQL like this:

```sql
/* MySQL, ClickHouse */
INSERT IGNORE INTO ForumUser (SiteId,ForumId,UserId,Permission) VALUES (@p1,@p2,@p3,@p4);

/* PostgreSQL, SQLite */
INSERT INTO ForumUser (SiteId,ForumId,UserId,Permission) VALUES (@p1,@p2,@p3,@p4)
ON CONFLICT DO NOTHING;
```

If you also provide a value for a sequence field, chain the sequence option:

```csharp
var options = DataInsertOptions
    .Sequence(DataSequenceBehavior.Never)
    .IgnoreConstraint();

this.DataAccess.Insert<Forum>(new {
    SiteId = this.User.SiteId,
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

forum.SiteId = this.User.SiteId;
forum.GroupId = 100;
forum.Name = "xxxx";

forum.Users = new ForumUser[]
{
    new ForumUser { UserId = 100, IsModerator = true },
    new ForumUser { UserId = 101, Permission = Permission.Read },
    new ForumUser { UserId = 102, Permission = Permission.Write }
};

this.DataAccess.Insert(forum, "*, Users{*}");
```

The insert above roughly generates SQL like this(_MySQL_):

```sql
/* Insert the master row once. */
INSERT INTO Forum (SiteId,ForumId,GroupId,Name,...) VALUES (@p1,@p2,@p3,@p4,...);

/* Insert child rows multiple times. */
INSERT INTO ForumUser (SiteId,ForumId,UserId,Permission,IsModerator) VALUES (@p1,@p2,@p3,@p4,@p5);
```

<a name="usage-import"></a>
### Import operation

`Import` is used for bulk data import. Pass the target entity name, the data collection, and the member list that should be imported.

```csharp
var users = new []
{
    new { SiteId = this.User.SiteId, ForumId = 100, UserId = 100, Permission = Permission.Read },
    new { SiteId = this.User.SiteId, ForumId = 100, UserId = 101, Permission = Permission.Write },
};

var count = this.DataAccess.Import(
    "ForumUser",
    users,
    "SiteId,ForumId,UserId,Permission".Split(','));
```

Use `DataImportOptions.IgnoreConstraint()` when duplicated rows should be skipped during import. It can be chained with `Parameter(...)` when filters or services need operation-level flags:

```csharp
var options = DataImportOptions
    .Parameter("SkipSynchronization")
    .IgnoreConstraint();

var count = this.DataAccess.Import(
    "ForumUser",
    users,
    "SiteId,ForumId,UserId,Permission".Split(','),
    options);
```

<a name="usage-update"></a>
### Update operation

```csharp
var user = Model.Build<UserProfile>();

user.UserId = 100;
user.Name = "Popeye";
user.FullName = "Popeye Zhong";
user.Gender = Gender.Male;

this.DataAccess.Update(user);
```

The update above roughly generates SQL like this:

```sql
/* Unmodified properties are not generated into the SET clause. */

UPDATE UserProfile SET
Name=@p1, FullName=@p2, Gender=@p3
WHERE UserId=@p4;
```

<a name="usage-update-dynamic"></a>
#### Anonymous class

The value to write can be an anonymous object, dynamic object _(`ExpandoObject`)_, or dictionary _(`IDictionary`, `IDictionary<string, object>`)_.

```csharp
this.DataAccess.Update<UserProfile>(
    new {
        Name="Popeye",
        FullName="Popeye Zhong",
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
this.DataAccess.Update<UserProfile>(
    user,
    "Name, Gender"
);

/*
 * * allows all fields, but CreatorId and CreatedTime are excluded.
 * Even if user contains values for these two properties, no SET clauses are generated for them.
 */
this.DataAccess.Update<UserProfile>(
    user,
    "*, !CreatorId, !CreatedTime"
);
```

<a name="usage-update-complex"></a>
#### Associated update

Related one-to-one and one-to-many navigation values can be written **together**. For one-to-many collections, child rows are written with **UPSERT** semantics.

```csharp
public bool Approve(ulong threadId)
{
    var criteria =
        Condition.Equal(nameof(Thread.ThreadId), threadId) &
        Condition.Equal(nameof(Thread.Approved), false) &
        Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
        Condition.Exists("Forum.Users",
            Condition.Equal(nameof(Forum.ForumUser.UserId), this.User.UserId) &
            Condition.Equal(nameof(Forum.ForumUser.IsModerator), true));

    return this.DataAccess.Update<Thread>(new
    {
        Approved = true,
        ApprovedTime = DateTime.Now,
        Post = new
        {
            Approved = true,
        }
    }, criteria, "*,Post{Approved}") > 0;
}
```

The update above roughly generates SQL like this(_SQL Server_):

```sql
CREATE TABLE #TMP
(
    PostId bigint NOT NULL
);

UPDATE T SET
    T.[Approved]=@p1,
    T.[ApprovedTime]=@p2
OUTPUT DELETED.PostId INTO #TMP
FROM [Discussions_Thread] AS T
    LEFT JOIN [Discussions_Forum] AS T1 ON /* Forum */
        T1.[SiteId]=T.[SiteId] AND
        T1.[ForumId]=T.[ForumId]
WHERE
    T.[ThreadId]=@p3 AND
    T.[Approved]=@p4 AND
    T.[SiteId]=@p5 AND EXISTS (
        SELECT [SiteId],[ForumId] FROM [Discussions_ForumUser]
        WHERE [SiteId]=T1.[SiteId] AND [ForumId]=T1.[ForumId] AND [UserId]=@p6 AND [IsModerator]=@p7
    );

UPDATE T SET
    T.[Approved]=@p1
FROM [Discussions_Post] AS T
WHERE EXISTS (
    SELECT [PostId]
    FROM #TMP
    WHERE [PostId]=T.[PostId]);
```

<a name="usage-upsert"></a>
### Upsert operation

**Upsert** inserts a row when it does not exist, or updates it when it already exists. It maps to the database provider's native upsert support where possible.

> In this example, if a `History` row with `UserId=100` and `ThreadId=2001` exists, `ViewedCount` is incremented. Otherwise, a new row is inserted with `ViewedCount` set to `1`.

```csharp
this.DataAccess.Upsert<History>(
    new {
        UserId = 100,
        ThreadId = 2001,
        ViewedCount = Operand.Field(nameof(History.ViewedCount)) + 1,
        LastViewedTime = DateTime.Now,
    }
);
```

The upsert above roughly generates SQL like this:

```sql
/* MySQL syntax */
INSERT INTO History (UserId,ThreadId,ViewedCount,LastViewedTime) VALUES (@p1,@p2,@p3,@p4)
ON DUPLICATE KEY UPDATE ViewedCount=ViewedCount + @p3, LastViewedTime=@p4;

/* SQL syntax for SQL Server or PostgreSQL support for MERGE statement */
MERGE History AS target
USING (SELECT @p1,@p2,@p3,@p4) AS source (UserId,ThreadId,ViewedCount,LastViewedTime)
ON (target.UserId=source.UserId AND target.ThreadId=source.ThreadId)
WHEN MATCHED THEN
    UPDATE SET target.ViewedCount=target.ViewedCount+@p3, LastViewedTime=@p4
WHEN NOT MATCHED THEN
    INSERT (UserId,ThreadId,ViewedCount,LastViewedTime) VALUES (@p1,@p2,@p3,@p4);
```

<a name="usage-returning"></a>
### Returning values

Delete, insert, update, and upsert options can request returned values from the database provider. This is useful for deleted file paths, identity values, generated values, and updated counters. The feature is available only when the current driver supports returning values.

```csharp
var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(Thread.TotalViews));

this.DataAccess.Update<Thread>(
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

For simple counters, the `Increase` and `Decrease` helpers wrap this pattern:

```csharp
var totalViews = this.DataAccess.Increase<Thread>(
    nameof(Thread.TotalViews),
    Condition.Equal(nameof(Thread.ThreadId), threadId));
```

The delete option returns older values:

```csharp
var options = DataDeleteOptions.Return(nameof(PostAttachment.AttachmentId));

this.DataAccess.Delete<PostAttachment>(
    Condition.Equal(nameof(PostAttachment.PostId), postId),
    options);

foreach(var row in options.Returning.Rows)
{
    if(row.TryGetValue(nameof(PostAttachment.AttachmentId), out var value))
        DeletePhysicalAttachment(Convert.ToUInt64(value));
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

`Parameter(...)` is commonly used to pass transient state to validators, filters, or service overrides. For example, a synchronization filter can skip its side effect when the caller sets a flag:

```csharp
var options = DataUpdateOptions
    .Parameter("SkipSynchronization")
    .SuppressValidator();

this.DataAccess.Update<Thread>(
    new { TotalViews = Operand.Field(nameof(Thread.TotalViews)) + 1 },
    Condition.Equal(nameof(Thread.ThreadId), threadId),
    options);
```

The filter or service hook reads the flag from the operation context:

```csharp
if(context.Options.Parameters.Contains("SkipSynchronization"))
    return;
```

`Parameter(...)` can also carry an object needed by a service hook:

```csharp
var options = DataInsertOptions.Parameter("Thread", thread);

this.DataAccess.Insert<Post>(
    new {
        ThreadId = thread.ThreadId,
        CreatorId = this.User.UserId,
        Content = content,
    },
    options);
```

For mapped commands, keep SQL or stored procedure parameters in the `Execute` argument. Put only operation-context flags in `DataExecuteOptions`:

```csharp
var options = DataExecuteOptions.Parameter("SkipAudit");

this.DataAccess.Execute(
    "Forum.RefreshStatistics",
    new []
    {
        new Parameter("SiteId", this.User.SiteId),
        new Parameter("ForumId", forumId),
    },
    options);
```

Use `Distinct()` for scalar lookups:

```csharp
var creatorIds = this.DataAccess.Select<uint>(
    nameof(Thread),
    Condition.Equal(nameof(Thread.ForumId), forumId),
    nameof(Thread.CreatorId),
    DataSelectOptions.Distinct());
```

Use `SuppressValidator()` when an internal operation intentionally bypasses normal validator rules:

```csharp
var exists = this.DataAccess.Exists<UserProfile>(
    Condition.Equal(nameof(UserProfile.UserId), userId),
    DataExistsOptions.SuppressValidator());
```

Use `UpdateBehaviors.PrimaryKey` only when a primary key must be repaired:

```csharp
this.DataAccess.Update<UserProfile>(
    new { UserId = newUserId },
    Condition.Equal(nameof(UserProfile.UserId), oldUserId),
    nameof(UserProfile.UserId),
    new DataUpdateOptions(UpdateBehaviors.PrimaryKey));
```

Every operation also has before/after callbacks and matching `IDataAccess` events, such as `Selecting`/`Selected`, `Inserting`/`Inserted`, and `Executing`/`Executed`. Filters registered in `IDataAccess.Filters` run between the before event and the provider execution, which is the common place for cross-cutting behaviors.

Use `Transaction` when several operations must share one ambient data session and commit or rollback together.

```csharp
using var transaction = Transaction.ReadCommitted();

this.DataAccess.Update<Thread>(
    new { Approved = true, ApprovedTime = DateTime.Now },
    Condition.Equal(nameof(Thread.ThreadId), threadId));

this.DataAccess.Upsert<History>(new {
    UserId = this.User.UserId,
    ThreadId = threadId,
    ViewedCount = Operand.Field(nameof(History.ViewedCount)) + 1,
    FirstViewedTime = DateTime.Now,
    LastViewedTime = DateTime.Now,
});

transaction.Commit();
```

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

The implementation uses **emitting** and dynamic compilation to prepare model population and parameter binding paths ahead of time. See [ModelEmitter](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/src/Common/ModelMemberEmitter.cs) and related classes for details.
