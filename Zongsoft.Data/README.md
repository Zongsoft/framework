# Zongsoft.Data ORM Framework

![license](https://img.shields.io/github/license/Zongsoft/Zongsoft.Data) ![download](https://img.shields.io/nuget/dt/Zongsoft.Data) ![version](https://img.shields.io/github/v/release/Zongsoft/Zongsoft.Data?include_prereleases) ![github stars](https://img.shields.io/github/stars/Zongsoft/Zongsoft.Data?style=social)

README: [English](https://github.com/Zongsoft/Zongsoft.Data/blob/master/README.md) | [简体中文](https://github.com/Zongsoft/Zongsoft.Data/blob/master/README-zh_CN.md)

-----

The [Zongsoft.Data](https://github.com/Zongsoft/Zongsoft.Data) is a [GraphQL](https://graphql.cn)-style **ORM**(**O**bject/**R**elational **M**apping) data access framework.

Its design philosophy is to represent the data structure relationship in a declarative way and de-scripting _(i.e. data access and navigation without writing any SQL or SQL-like syntax structure)_, making access to data easier, application code cleaner, and providing the best comprehensive price/performance ratio.

<a name="feature"></a>
## Features

- Support for strict POCO/POJO objects without any annotation(Attribute) dependency;
- Support for read and write separate data access;
- Support various data operations of table inheritance;
- Support mapping isolation of business modules and complete extension mechanism;
- Data navigation, filtering, paging, grouping, aggregation operations, etc. without SQL scripts;
- Universal intuition for object-oriented developers, easy to understand, out of the box;
- Provide excellent overall cost performance and complete solutions;
- Minimal implementation dependencies, usually only require ADO.NET and native ADO.NET drivers or less.

<a name="driver"></a>
## Drivers

| **Deriver** | **Project Path** | **State** |
| --- | --- | :---: |
MySQL | [/drivers/mysql](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/mysql) | **Available** |
SQL Server | [/drivers/mssql](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/mssql) | **Available** |
PostgreSQL | [/drivers/postgres](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/postgres) | _Unimplemented_ |
Oracle | [/drivers/oracle](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/oracle) | _Unimplemented_ |
InfluxDB | [/drivers/influx](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/influx) | **Planning** |
Elasticsearch | [/drives/elastics](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/elastics) | **Planning** |

> Tip: If you need unimplemented drivers or commercial technical support, please contact us.([zongsoft@qq.com](mailto:zongsoft@qq.com))。


<a name="environment"></a>
### Environment

- .NET Framework 4.6+
- .NET Standard 2.0+

<a name="download"></a>
## Download

### Source code compilation

It is recommended to create a **_Zongsoft_** directory in the non-system partition of the hard disk and clone the items such as [Guidelines](https://github.com/Zongsoft/Guidelines), [Zongsoft.CoreLibrary](https://github.com/Zongsoft/Zongsoft.CoreLibrary) and [Zongsoft.Data](https://github.com/Zongsoft/Zongsoft.Data), etc. into this directory.

<a name="schema"></a>
## The data schema

The data **schema** is a DSL(**D**omain **S**pecific **L**anguage) that describes the shape of the data to be query or write _(**D**elete/**I**nsert/**U**pdate/**U**psert)_, The representation is somewhat like [GraphQL](https://graphql.cn) but does not require to predefined. It is used to define the data fields to be fetched and written, scopes for cascading deletes, etc.

The `schema` argumment in the data access method is the data schema, and the [ISchema](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/ISchema.cs) interface is the parsed schema expression.

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

- Asterisk(`*`): Indicates that all simplex/scalar properties (without navigation/complex properties) are included, and must be explicitly specified if you want to include navigation properties.

- Exclamation(`!`): for exclusion, a single exclamation mark indicates the exclusion of the previous definition, and `Exclamation + Property` indicate a property that excludes the specified name.

> **Note:** The data schema will be further grammatically enhanced later, such as the qualification of navigation properties, the type specification of non-deterministic navigation properties, and so on.

<a name="schema-sample"></a>
### Sample description

```graphql
*, !CreatorId, !CreatedTime
```
> **Note:** All simplex/_scalar_ properties without `CreatorId` and `CreatedTime` properties.

```graphql
*, Creator{*}
```
> **Note:** All simplex/_scalar_ properties and `Creator` complex property(all simplex properties of this complex property).

```graphql
*, Creator{Name,FullName}
```
> **Note:** All simplex/_scalar_ properties and `Creator` complex property(Include only the `Name` and `FullName` properties of the navigation property).

```graphql
*, Users{*}
```
> **Note:** All simplex/_scalar_ properties and `Users` complex property _(one-to-many)_, The collection property has no sorting, no paging.

```graphql
*, Users:1{*}
```
> **Note:** All simplex/_scalar_ properties and `Users` complex property _(one-to-many)_, Paginate the results of this collection property (page 1 / page size is the default).

```graphql
*, Users:1/20{*}
```
> **Note:** All simplex/_scalar_ properties and `Users` complex property _(one-to-many)_, Paginate the results of this collection property (page 1 / 20 per page).

```graphql
*, Users:1/20(Grade,~CreatedTime){*}
```
> **Note:** All simplex/_scalar_ properties and `Users` complex property _(one-to-many)_, Sorting and paginate the results of this collection property (`Grade` ascending and `CreatedTime` descending, page 1 / 20 per page).


<a name="mapping"></a>
## Mapping file

A data map file is an XML file with a `.mapping` extension that is metadata that defines the relationship of the entity structure. **Do not** write metadata in a large application in a mapping file. A mapping file should be defined separately for each business module to ensure the isolation of the module.

We provide the [Zongsoft.Data.xsd](https://github.com/Zongsoft/Zongsoft.Data/blob/master/Zongsoft.Data.xsd) XML Schema file, It makes it easy for you to handwrite mapping files and eliminate the chance of errors.


> **Enable XML IntelliSense for mapping files:**
> 
> **Method 1：** Add new an XML file called "`{module}.mapping`" to the business module project(for example: [`Zongsoft.Security.mapping`](https://github.com/Zongsoft/Zongsoft.Security/blob/master/src/Zongsoft.Security.mapping) or [`Zongsoft.Community.mapping`](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Zongsoft.Community.mapping)), open the mapping file and click "XML" -> "Architecture" menu item in the **V**isual **S**tudio, in the pop-up dialog box, click the "Add" button in the upper right corner to find the [Zongsoft.Data.xsd](https://github.com/Zongsoft/Zongsoft.Data/blob/master/Zongsoft.Data.xsd) file.
> 
> **Method 2：** Copy [Zongsoft.Data.xsd](https://github.com/Zongsoft/Zongsoft.Data/blob/master/Zongsoft.Data.xsd) to the XML Shemas template directory in Visual Studio, for example:
> - **V**isual **S**tudio 2019 _(Enterprise Edition)_ <br />
> 	`C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Xml\Schemas`

-----

> Although some programmers are used to using tools to generate mapping files, we still recommend handwriting:
> 
> - Data structure and relationship are undoubtedly the lowest level of infrastructure for any system. The database table structure is the concrete manifestation of this structure relationship. The mapping file is the "treasure map" about definition of the structural relationship between the upper layer entities and the lower tables.
> - The mapping file should be uniformly updated by the system architect or the module development leader. The settings of `inherits`, `immutable`, `sortable`, `sequence` and navigation properties in the mapping have a crucial impact on the development of the application layer. so care must be taken carefully.


<a name="usage"></a>
## Usages

All data operations are performed through the data access interface (located on the [`Zongsoft.Data.IDataAccess`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/IDataAccess.cs) interface in the [Zongsoft.CoreLibrary](https://github.com/Zongsoft/Zongsoft.CoreLibrary)) and support the following data access operations:

- `int Count(...)` 
- `bool Exists(...)` 
- `long Increment(...)` `long Decrement(...)` 
- `IEnumerable<T> Execute<T>(...)` `object ExecuteScalar(...)` 
- `int Delete(...)` 
- `int Insert(...)` `int InsertMany(...)` 
- `int Update(...)` `int UpdateMany(...)` 
- `int Upsert(...)` `int UpsertMany(...)` 
- `IEnumerable<T> Select<T>(...)` 

**Remind:**
> The following examples are based on the [Zongsoft.Community](https://github.com/Zongsoft/Zongsoft.Community) open source project, which is a complete community forum .NET backend project. It is recommended that you read [the database table structure design document](https://github.com/Zongsoft/Zongsoft.Community/blob/master/database/Zongsoft.Community-Tables.md) of the project to understand the relevant data structure relationship before reading following samples.

<a name="usage-query"></a>
### Query operation

<a name="usage-query-1"></a>
#### Basic query

- Returns all scalar fields by default, which can be explicitly specified by the `schema` argument.
- The result of the query is lazy loading, traversing the result set or calling Linq's `ToList()`, `First()` extension methods to trigger actual data access.

**Note:** Because the query is not paged by default, you should avoid using Linq's `ToList()`, `ToArray()` extension methods to load the result set into memory, so as to avoid unnecessary data access and wasted memory space.

```csharp
// Gets the entities of all scalar fields of the specified criteria(lazy loading)
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("Visible", true));

// Get a single entity with the specified criteria(only specific fields)
var forum = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("ForumId", 100),
    "SiteId,ForumId,Name,Description,CoverPicturePath").FirstOrDefault();
```

<a name="usage-query-2"></a>
#### Scalar value query

Querying the value of a scalar avoids returning unwanted fields and avoids the performance penalty of populate the entity, while also making the business code more concise.

**Call description:**

1. A generic parameter is specified as a type that returns a scalar value or a convertible type of a field;
2. Must explicitly specify the entity name of the query(by the method's `name` argument);
3. Must explicitly specify the property name of the returned(by the method's `schema` argument).

```csharp
var email = this.DataAccess.Select<string>("UserProfile",
    Condition.Equal("UserId", this.User.UserId),
    "Email" //Explicitly specify only the value of the "Email" field by the schema argument, which is a string type
).FirstOrDefault();

/* Return a scalar value set(IEnumerable<int>) */
var counts = this.DataAccess.Select<int>("History",
    Condition.Equal("UserId", this.User.UserId),
    "Count" //Explicitly specify only the value of the "Count" field by the schema argument, which is an integer type
);
```

<a name="usage-query-3"></a>
#### Multi-field query

Query the values of multiple fields, and support returning any entity type, including class, interface, structure, dynamic class(`ExpandoObject`), and dictionary.

```csharp
struct UserToken
{
    public uint UserId;
    public string Name;
}

/*
 * Note: The schema argument of this method can be missing or empty, and the actual effect is the same.
 * Because the return fields of the query method defaults to the intersection of schema and the properties and fields of the returned entity type.
 */
var tokens = this.DataAccess.Select<UserToken>(
    "UserProfile",
    Condition.Equal("SiteId", this.User.SiteId),
    "UserId, Name"
);
```

```csharp
/*
 * When the entity to be accessed is different from the generic parameter type,
 * The entity class(structure, interface) can be annotated with ModelAttribute to determine its mapped entity name.
 */
[Zongsoft.Data.Model("UserProfile")]
struct UserToken
{
    public uint UserId;
    public string Name;
}

// Because the returned entity class(structure, interface) is annotated with the mapped entity name, the name argument is missing, and the code can be simplified as follows:
var tokens = this.DataAccess.Select<UserToken>(
    Condition.Equal("SiteId", this.User.SiteId)
);
```

```csharp
/*
 * 1)The return result type is specified as a dictionary by a generic parameter.
 * 2)Explicitly specify the returned fields via the schema argument. If this argument is missing or an asterisk(*), all fields are returned by default.
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
 * The result type specified by the generic parameter is ExpandoObject, which is then accessed dynamically.
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

Specify the `paging` argument in the [`Select`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/IDataAccess.cs) method for paging queries. For details, see the [`Paging`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/Paging.cs) settings class.

```csharp
// Define the paging settings for the query(page 2, 25 per page)
var paging = Paging.Page(2, 25);

var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
    Condition.Equal(nameof(Thread.ForumId), 100),
    paging
);

/*
 * After the query method is called, the paging variable is the paging result:
 * paging.PageCount indicates the total number of pages that satisfy the condition
 * paging.TotalCount indicates the total number of records that satisfy the condition
 */
```

<a name="usage-query-5"></a>
#### Sorting query

Specify the `sortings` argument in the [`Select`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/IDataAccess.cs) method to sort the query. For details, please refer to the [Sorting](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/Sorting.cs) settings class.

```csharp
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
    Condition.Equal(nameof(Thread.ForumId), 100),
    Paging.Disable, /* It is explicitly specified here to disable paging(you can also specify a paging setting) */
    Sorting.Descending("TotalViews"),   // 1.Descending for TotalViews
    Sorting.Descending("TotalReplies"), // 2.Descending for TotalReplies
    Sorting.Ascending("CreatedTime")    // 3.Ascending for CreatedTime
);
```

<a name="usage-query-6"></a>
#### Navigation properties

The navigation(complex) property is explicitly specified by the `schema` argument, which supports one-to-one(zero), one-to-many navigation relationships, and supports nesting at any level. See the syntax description of the **Schema** for more details.

<a name="usage-query-7"></a>
##### One-to-One

```csharp
/*
 * 1) The structural relationship of the Thread entity's Post navigation property(associated to the Post entity) is one-to-one,
 *    that is, multiplicity="!" in the mapping file(.mapping), so its corresponding SQL association is INNER JOIN.
 *
 * 2) The structure relationship of the Thread entity's MostRecentPost navigation property(associated to the Post entity) is one-to-one/zero(the default value),
 *    that is, multiplicity="?" in the mapping file(.mapping), so its corresponding SQL association is LEFT JOIN.
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
 * 1) The forum group(ForumGroup) Forums navigation property structure is one-to-many,
 *    that is, multiplicity="*" in the mapping file(.mapping), the navigation property will correspond to a new SQL query statement.
 *
 * 2) Whether it's a "one-on-one" or "one-to-many" navigation property, they all support arbitrary nesting.
 *
 * Note: The asterisk(*) indicates all scalar(simplex) properties without any navigation properties,
 *       so the navigation properties must be explicitly specified.
 */
var groups = this.DataAccess.Select<ForumGroup>(
    Condition.Equal("SiteId", this.User.SiteId),
    "*,Forums{*, Moderators{*}, MostRecentThread{*, Creator{*}}}"
);
```

<a name="usage-query-9"></a>
##### Navigation constraint

Especially in a one-to-many relationship, it is often necessary to conditionally constrain the result set of the navigation property, which is the navigation constraint.

> There is a one-to-many relationship between the forum(`Forum`) and the forum members(`ForumUser`). The moderators(`Moderator`) is a subset of the forum members(`ForumUser`), then the structural relationship is Expressed by `complexProperty/constraints` in the data mapping file.
> 
> As shown in the following code, the `Users` navigation property of the [Forum](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Forum.cs) entity represents the full set of forum members, and the `Moderators` navigation property is a subset of the forum members, which are all associated with the `ForumUser` entity.

```xml
<entity name="Forum" table="Community_Forum">
	<key>
		<member name="SiteId" />
		<member name="ForumId" />
	</key>

	<property name="SiteId" type="uint" nullable="false" />
	<property name="ForumId" type="ushort" nullable="false" sequence="#SiteId" />
	<property name="GroupId" type="ushort" nullable="false" />
	<property name="Name" type="string" length="50" nullable="false" />

	<complexProperty name="Users" role="ForumUser" multiplicity="*" immutable="false">
		<link name="SiteId" role="SiteId" />
		<link name="ForumId" role="ForumId" />
	</complexProperty>

	<complexProperty name="Moderators" role="ForumUser:User" multiplicity="*">
		<link name="SiteId" role="SiteId" />
		<link name="ForumId" role="ForumId" />

		<!-- Constraints of navigation property -->
		<constraints>
			<constraint actor="Foreign" name="IsModerator" value="true" />
		</constraints>
	</complexProperty>
</entity>

<entity name="ForumUser" table="Community_ForumUser">
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

	<complexProperty name="User" role="UserProfile" multiplicity="!">
		<link name="UserId" role="UserId" />
	</complexProperty>
</entity>
```

<a name="usage-query-10"></a>
##### Navigation springboard

Point to another navigation property in the associated entity, which usually needs to be filtered with the use of navigation constraints. Take the `Moderators` navigation(complex) property of the `Forum` entity in the above mapping file as an example:

1. Specify the colon syntax of the `role` attribute of the navigation(complex) property: the left side of the colon is the associated entity name, and the right side of the colon is the corresponding target navigation property.

2. Define the `constraint` constraint for this navigation(complex) property.

> Note: Since the moderator is not restricted by the forum member's `Permission` field, the definition of the moderator's entity type is [`UserProfile`](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/UserProfile.cs) will be more concise and easy to use(avoid the jump navigation through `ForumUser.User`), so set `role` attribute of the `Moderators` navigation property is _`"ForumUser:User"`_ to express this requirement.
> 
> Take the above data mapping fragment as an example, and feel the difference between the `Users` and `Moderators` property types of the [Forum](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Forum.cs) class.

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

// The type of moderator variable is UserProfile.
foreach(var moderator in forum.Moderators)
{
    Console.Write(moderator.Name);
    Console.Write(moderator.Email);
    Console.Write(moderator.Avatar);
}

// The type of member variable is ForumUser.
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

Grouping queries support aggregate functions for relational databases, and in the future it will add more statistical functions to the time series database.

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

The above query method call will be roughly generated as the following SQL script:

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

Conditional filtering of entities associated with navigation properties.

```csharp
/*
 * The query condition description:
 * 1) The most valuable thread associated with the History table(Thread.IsValued=true), and
 * 2) The viewing time(first or last) is within the last 30 days.
 */
var histories = this.DataAccess.Select<History>(
    Condition.Equal("Thread.IsValued", true) & /* The navigation condition */
    (
        Condition.Between("FirstViewedTime", DateTime.Today.AddDays(-30), DateTime.Now) |
        Condition.Between("MostRecentViewedTime", DateTime.Today.AddDays(-30), DateTime.Now)
    )
);
```

The above query method call will be roughly generated as the following SQL script:

```sql
SELECT h.*
FROM History h
    LEFT JOIN Thread t ON
        t.ThreadId = h.ThreadId
WHERE t.IsValued = @p1 AND
    (
        h.FirstViewedTime BETWEEN @p2 AND @p3 OR
        h.MostRecentViewedTime BETWEEN @p4 AND @p5
    );
```

<a name="usage-query-13"></a>
#### Subquery filtering

The conditional filtering of a one-to-many navigation property corresponds to a subquery of SQL, expressed using the `Exists` operator.

> The following code indicates that the forum **visibility** is "`Internal`" or "`Public`" under the site to which the current user belongs. If the forum **visibility** is "`Specified`", then it is determined whether the current user is a moderator or has forum member permissions.

```csharp
var forums = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.In("Visibility", Visibility.Internal, Visibility.Public) |
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

The above query method call will be roughly generated as the following SQL script:

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

When the database field type does not match the corresponding entity property type _(cannot be converted directly)_, you need to introduce a type converter for custom conversion logic.

For example, the `Tags` field type of the `Thread` table is `nvarchar`, but the type of the `Tags` property of the [Thread](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Thread.cs) model class is a **string array**, so data read and write operations require custom conversion of these two types. For specific implementations, please refer to the [TagsConverter](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/TagsConverter.cs) class, and the `Tags` property definition of the [Thread](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Thread.cs) model class.


<a name="usage-delete"></a>
### Delete operation

```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("Visible", false) &
    Condition.Equal("Creator.Email", "zongsoft@qq.com")
);
```

The above delete method call will be roughly generated as the following SQL script:

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

Support for deleting sub-table records associated with "one-to-one(zero)" or "one-to-many" navigation properties.

```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("PostId", 100023),
    "Votes"
);
```

The above delete method call will be roughly generated as the following SQL script(_SQL Server_):

```sql
CREATE TABLE #TMP
(
    PostId bigint
);

/* Delete the master table and export the associated field values of the one-to-many navigation property to the temporary table */
DELETE FROM Post
OUTPUT DELETED.PostId INTO #TMP
WHERE PostId=@p1;

/* Delete dependent table records, with the condition that a subset of the master table has been deleted */
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

<a name="usage-insert-complex"></a>
#### Associated insertion

Support "one-to-one" or "one-to-many" navigation properties to be inserted at the same time.

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

The above insert method call will be roughly generated as the following SQL script(_MySQL_):

```sql
/* The master table insert statement, only once */
INSERT INTO Forum (SiteId,ForumId,GroupId,Name,...) VALUES (@p1,@p2,@p3,@p4,...);

/* Subtable insert statement, multiple executions */
INSERT INTO ForumUser (SiteId,ForumId,UserId,Permission,IsModerator) VALUES (@p1,@p2,@p3,@p4,@p5);
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

The above update method call will be roughly generated as the following SQL script:

```sql
/* Note: Unmodified properties will not be generated as SET clause */

UPDATE UserProfile SET
Name=@p1, FullName=@p2, Gender=@p3
WHERE UserId=@p4;
```

<a name="usage-update-dynamic"></a>
#### Anonymous class

The data written can be an anonymous class, dynamic class _(`ExpandoObject`)_, dictionary _(`IDictionary`, `IDictionary<string, object>`)_, and the like.

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

Explicitly specify fields, or exclude some fields.

```csharp
/*
 * Explicitly specify only the Name, Gender fields by using the schema argument,
 * Other fields are not modified regardless of whether they have changed.
 */
this.DataAccess.Update<UserProfile>(
    user,
    "Name, Gender"
);

/*
 * All fields can be updated by specifying the schema argument, but the CreatorId and CreatedTime are excluded,
 * Even if the model object pointed to by the user variable contains and changes the values of these two properties, their SET clauses will not be generated.
 */
this.DataAccess.Update<UserProfile>(
    user,
    "*, !CreatorId, !CreatedTime"
);
```

<a name="usage-update-complex"></a>
#### Associated update

Supports "one-to-one" or "one-to-many" navigation properties to be written at the **same time**. For "one-to-many" navigation properties, it also ensures that the property value _(collection type)_ is written in **UPSERT** mode.

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

The above update method call will be roughly generated as the following SQL script(_SQL Server_):

```sql
CREATE TABLE #TMP
(
    PostId bigint NOT NULL
);

UPDATE T SET
    T.[Approved]=@p1,
    T.[ApprovedTime]=@p2
OUTPUT DELETED.PostId INTO #TMP
FROM [Community_Thread] AS T
    LEFT JOIN [Community_Forum] AS T1 ON /* Forum */
        T1.[SiteId]=T.[SiteId] AND
        T1.[ForumId]=T.[ForumId]
WHERE
    T.[ThreadId]=@p3 AND
    T.[Approved]=@p4 AND
    T.[SiteId]=@p5 AND EXISTS (
        SELECT [SiteId],[ForumId] FROM [Community_ForumUser]
        WHERE [SiteId]=T1.[SiteId] AND [ForumId]=T1.[ForumId] AND [UserId]=@p6 AND [IsModerator]=@p7
    );

UPDATE T SET
    T.[Approved]=@p1
FROM [Community_Post] AS T
WHERE EXISTS (
    SELECT [PostId]
    FROM #TMP
    WHERE [PostId]=T.[PostId]);
```

<a name="usage-upsert"></a>
### Upsert operation

The **Upsert** operation corresponds to a single primitive language in SQL, providing higher performance and consistency, and provides very simple syntax support for the application layer.

> Modify the `History` table, When the record specifying the primary key value(ie `UserId=100` and `ThreadId=2001`) exists, then increment the `Count` field value; otherwise, a new record is added, and the the `Count` field value is `1`.

```csharp
this.DataAccess.Upsert<History>(
    new {
        UserId = 100,
        ThreadId = 2001,
        Count = (Interval)1;
        MostRecentViewedTime = DateTime.Now,
    }
);
```

The above upsert method call will be roughly generated as the following SQL script:

```sql
/* MySQL syntax */
INSERT INTO History (UserId,ThreadId,Count,MostRecentViewedTime) VALUES (@p1,@p2,@p3,@p4)
ON DUPLICATE KEY UPDATE Count=Count + @p3, MostRecentViewedTime=@p4;

/* SQL syntax for SQL Server or other(PostgreSQL/Oracle) support for MERGE statement */
MERGE History AS target
USING (SELECT @p1,@p2,@p3,@p4) AS source (UserId,ThreadId,[Count],MostRecentViewedTime)
ON (target.UserId=source.UserId AND target.ThreadId=source.ThreadId)
WHEN MATCHED THEN
    UPDATE SET target.Count=target.Count+@p3, MostRecentViewedTime=@p4
WHEN NOT MATCHED THEN
    INSERT (UserId,ThreadId,Count,MostRecentViewedTime) VALUES (@p1,@p2,@p3,@p4);
```

<a name="usage-other"></a>
### Other

For more details(such as read-write separation, inheritance tables, data schema, mapping files, filters, validators, type conversions, data isolation), please consult the related documentation.

If you agree with our design philosophy(ideas), please pay attention to the(**W**atch & **F**ork) and **S**tar(Like) this project.

<a name="performance"></a>
## Performance

We want to provide the best **overall price/performance ratio** and not compromise our design goals for some of benchmarking. We believe that for an ORM data access engine, performance concerns are mainly(unlimited) with these elements:

1. Generate clean and efficient SQL scripts and make the best use of the latest SQL syntax of the specified database;
2. The model/entity populate process of the data query results must be efficient;
3. Avoid reflections, a valid syntax tree cache.

Thanks to the semantic design concept of “declaratively expressing data structure relationships”, compared with the imperative programming design, the program intention is more focused, and it is natural easier to convert the semantics into a syntax tree to represent SQL scripts of different data providers, and the optimization space of each step is more relaxed and free.

The implementation layer uses **emitting** dynamic compilation technology to pre-heat the model/entity populated, data parameter binding, etc., which can be understood by the [DataPopulator](https://github.com/Zongsoft/Zongsoft.Data/blob/master/src/Common/DataPopulatorProviderFactory.cs) class and related classes.

<a name="contribution"></a>
## Contributing

Please do not submit a **Q**uestion and **D**iscussion in the **I**ssues of the project. **I**ssues are used to report bugs and features. If you wish to participate in the contribution, you are welcome to submit a code merge request([PullRequest](https://github.com/Zongsoft/Zongsoft.Data/pulls)) or an [issue](https://github.com/Zongsoft/Zongsoft.Data/issues).

For new features, be sure to create a functional feedback([Issue](https://github.com/Zongsoft/Zongsoft.Data/issues)) to describe your recommendations in detail so that we can fully discuss them, which will also allow us to better coordinate our work to prevent duplication of development and help you adjust recommendations or requirements to make it successfully accepted into the project.

You are welcome to write articles for our open source projects to promote, if you need to forward your **A**rticles, **B**logs, **V**ideos, etc. on the official website([http://zongsoft.com/blog](http://zongsoft.com/blog)), you can contact us by [**email**](mailto:zongsoft@qq.com).

> If you're new to posting issues, we ask that you read ["How To Ask Questions The Smart Way"](http://www.catb.org/~esr/faqs/smart-questions.html), ["How to Ask a Question in Open Source Community"](https://github.com/seajs/seajs/issues/545) and ["How to Report Bugs Effectively"](http://www.chiark.greenend.org.uk/~sgtatham/bugs.html) prior to posting. Well written bug reports help us help you!


<a name="sponsor"></a>
## Sponsorship

We look forward to your support and sponsorship. You can provide us with the necessary financial support in the following ways:

1. Follow the **Zongsoft** WeChat public account and reward our articles;
2. Join the [**Zongsoft Knowledge Planet**](https://t.zsxq.com/2nyjqrr) to get online Q&A and technical support;
3. If your organization requires on-site technical support and coaching, or if you need some new features, instant bug fixes, etc., please contact us by [email](mailto:zongsoft@qq.com).

[![Zongsoft's WeChat](https://raw.githubusercontent.com/Zongsoft/Guidelines/master/zongsoft-qrcode%28wechat%29.png)](http://weixin.qq.com/r/zy-g_GnEWTQmrS2b93rd)

[![Zongsoft's Knowledge Planet](https://raw.githubusercontent.com/Zongsoft/Guidelines/master/zongsoft-qrcode%28zsxq%29.png)](https://t.zsxq.com/2nyjqrr)


<a name="license"></a>
## License

Licensed under the [LGPL](https://opensource.org/licenses/LGPL-2.1) license.
