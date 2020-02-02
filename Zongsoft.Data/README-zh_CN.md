# Zongsoft.Data 数据引擎

![license](https://img.shields.io/github/license/Zongsoft/Zongsoft.Data) ![download](https://img.shields.io/nuget/dt/Zongsoft.Data) ![version](https://img.shields.io/github/v/release/Zongsoft/Zongsoft.Data?include_prereleases) ![github stars](https://img.shields.io/github/stars/Zongsoft/Zongsoft.Data?style=social)

README: [English](https://github.com/Zongsoft/Zongsoft.Data/blob/master/README.md) | [简体中文](https://github.com/Zongsoft/Zongsoft.Data/blob/master/README-zh_CN.md)

-----

[Zongsoft.Data](https://github.com/Zongsoft/Zongsoft.Data) 是一个类 [GraphQL](https://graphql.cn) 风格的 **ORM**(**O**bject/**R**elational **M**apping) 数据访问框架。

它的设计理念是以声明方式来表达数据结构关系以及去SQL脚本化 _（即不需要手写任何SQL或类SQL语法结构即可完成数据访问和导航）_，使得访问数据变得更加容易、应用代码更简洁，并提供最佳的综合性价比。

<a name="feature"></a>
## 特性

- 支持严格的POCO对象，无任何注解(**A**ttribute/**A**nnotation)依赖；
- 支持读写分离的数据访问；
- 支持表继承的各种数据操作；
- 支持业务模块的映射隔离及完备的扩展机制；
- 无SQL脚本即可完成复杂的数据导航、过滤、分页、分组、聚合运算等；
- 符合面向对象开发人员的普遍直觉，易于理解、开箱即用；
- 提供优异的综合性价比及完整的解决方案；
- 最小实现依赖，通常仅需要ADO.NET和原生ADO.NET驱动或更少。

<a name="driver"></a>
## 驱动

| **驱动程序** | **项目路径** | **状态** |
| --- | --- | :---: |
MySQL | [/drivers/mysql](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/mysql) | **可用** |
SQL Server | [/drivers/mssql](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/mssql) | **可用** |
PostgreSQL | [/drivers/postgres](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/postgres) | _待实现_ |
Oracle | [/drivers/oracle](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/oracle) | _待实现_ |
InfluxDB | [/drivers/influx](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/influx) | **规划中** |
Elasticsearch | [/drives/elastics](https://github.com/Zongsoft/Zongsoft.Data/tree/master/drivers/elastics) | **规划中** |

> 提示：如果需要未实现的驱动或商业技术支持，请联系我们([zongsoft@qq.com](mailto:zongsoft@qq.com))。


<a name="environment"></a>
### 开发环境

- .NET Framework 4.6+
- .NET Standard 2.0+<br />

<a name="download"></a>
## 下载

- **源码编译方式**

建议在硬盘的非系统分区中建立一个 **_Zongsoft_** 目录，分别将 [Guidelines](https://github.com/Zongsoft/Guidelines)、[Zongsoft.CoreLibrary](https://github.com/Zongsoft/Zongsoft.CoreLibrary) 以及 [Zongsoft.Data](https://github.com/Zongsoft/Zongsoft.Data) 等项目克隆到该目录中。

<a name="schema"></a>
## 数据模式

数据模式(**S**chema)是一种 DSL(**D**omain **S**pecific **L**anguage)，用以描述要查询或写入 _(**D**elete/**I**nsert/**U**pdate/**U**psert)_ 的数据形状，表现形式有点类似于 [GraphQL](https://graphql.cn) 但不需要预先定义，通过它来定义要获取和写入的数据字段、级联删除的范围等。

在数据访问方法中的 `schema` 参数即为数据模式，[ISchema](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/ISchema.cs) 接口为解析后的模式表达式。

<a name="schema-syntax"></a>
### 语法定义

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
#### 说明

- 星号(`*`)：表示包含所有简单属性（不含导航属性），如果要包含导航属性必须显式指定。
- 叹号(`!`)：表示排除，单个叹号表示排除之前的定义， `叹号+属性` 则表示排除指定名称的属性。

> **注：** 后期还会对数据模式做进一步语法增强，譬如导航属性的限定条件、非确定性导航属性的类型指定 等。


<a name="schema-sample"></a>
### 示例说明

```graphql
*, !CreatorId, !CreatedTime
```
> 表示所有简单(标量)属性但是排除`CreatorId`和`CreatedTime`属性

```graphql
*, Creator{*}
```
> 表示所有简单(标量)属性和`Creator`导航属性（含该导航属性的所有简单属性）

```graphql
*, Creator{Name,FullName}
```
> 表示所有简单(标量)属性和`Creator`导航属性（仅含该导航属性的`Name`和`FullName`两个属性）

```graphql
*, Users{*}
```
> 表示所有简单(标量)属性和`Users`导航属性集 _（一对多）_，属性集无排序、无分页

```graphql
*, Users:1{*}
```
> 表示所有简单(标量)属性和`Users`导航属性集 _（一对多）_，对该属性集进行分页（第1页/每页大小为默认值）

```graphql
*, Users:1/20{*}
```
> 表示所有简单(标量)属性和`Users`导航属性集 _（一对多）_，对该属性集进行分页（第1页/每页20条）

```graphql
*, Users:1/20(Grade,~CreatedTime){*}
```
> 表示所有简单(标量)属性和`Users`导航属性集 _（一对多）_，对该属性集进行排序分页（`Grade`正序、`CreatedTime`倒序；第1页/每页20条）


<a name="mapping"></a>
## 映射文件

数据映射文件是扩展名为 `.mapping` 的XML文件，它是定义实体结构关系的元数据。**不要**将一个大应用内的元数据都写在一个映射文件内，应为每个业务模块单独定义映射文件，以确保模块的隔离性。

我们提供 [Zongsoft.Data.xsd](https://github.com/Zongsoft/Zongsoft.Data/blob/master/Zongsoft.Data.xsd) 这个 XML Schema 文件，大大方便了手写映射文件并消除了手写出错的机会。


> **启用映射文件的XML智能提示：**
> 
> **方法一：** 在**业务模块**项目中添加一个名为“`{业务模块}.mapping`”的XML文件（譬如：[`Zongsoft.Security.mapping`](https://github.com/Zongsoft/Zongsoft.Security/blob/master/src/Zongsoft.Security.mapping) 或 [`Zongsoft.Community.mapping`](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Zongsoft.Community.mapping)），打开该映射文件后点击 **V**isual **S**tudio 的“XML”-“架构”菜单项，在弹出的对话框中点击右上角的“添加”按钮，找到 [Zongsoft.Data.xsd](https://github.com/Zongsoft/Zongsoft.Data/blob/master/Zongsoft.Data.xsd) 文件即可。
> 
> **方法二：** 将 [Zongsoft.Data.xsd](https://github.com/Zongsoft/Zongsoft.Data/blob/master/Zongsoft.Data.xsd) 拷贝到 **V**isual **S**tudio 的 XML Shemas 模板目录中，譬如：
> - **V**isual **S**tudio 2019 _(Enterprise Edition)_ <br />
> 	`C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Xml\Schemas`


> 尽管有些程序员习惯利用工具来生成映射文件，但我们还是建议手写：
> - 数据结构与关系对于任何一个系统而言都是毋庸置疑的最底层的基础结构，数据库表结构就是这种结构关系的具体表现形式，映射文件正是定义上层实体与下层表的结构关系的“藏宝图”。
> - 映射文件应该由系统架构师或模块开发负责人统一更新，映射中的 `inherits`, `immutable`, `sortable`, `sequence` 以及导航属性的设置对业务层开发有至关影响，必须仔细认真的对待。


<a name="usage"></a>
## 使用

所有数据操作均通过[数据访问接口](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/IDataAccess.cs)(位于[核心库](https://github.com/Zongsoft/Zongsoft.CoreLibrary)中的 [`Zongsoft.Data.IDataAccess`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/IDataAccess.cs) 接口)进行，支持下列数据访问操作：

- 计数操作： `int Count(...)` 
- 存在操作： `bool Exists(...)` 
- 递增递减操作： `long Increment(...)` `long Decrement(...)` 
- 执行存储过程： `IEnumerable<T> Execute<T>(...)` `object ExecuteScalar(...)` 
- 删除操作： `int Delete(...)` 
- 新增操作： `int Insert(...)` `int InsertMany(...)` 
- 更新操作： `int Update(...)` `int UpdateMany(...)` 
- 新增更新： `int Upsert(...)` `int UpsertMany(...)` 
- 查询操作： `IEnumerable<T> Select<T>(...)` 

**提醒：**
> 下面的范例均基于 [Zongsoft.Community](https://github.com/Zongsoft/Zongsoft.Community) 开源项目，该项目是一个完整的论坛社区的后台程序。建议你在阅读范例之前，务必先查阅该项目的[数据库表结构](https://github.com/Zongsoft/Zongsoft.Community/blob/master/database/Zongsoft.Community.md)设计文档以了解相关数据结构关系。


<a name="usage-query"></a>
### 查询操作

<a name="usage-query-1"></a>
#### 简单查询

- 默认返回全部简单(标量)字段，可通过 `schema` 参数来显式指定返回的字段集。
- 查询结果为延迟加载，遍历结果集或调用 Linq 的 `ToList()`, `First()` 等扩展方法即可触发数据访问。
- **注意：** 因为查询默认未进行分页，所以应避免使用 Linq 的 `ToList()`, `ToArray()` 等扩展方法将结果集全部加载到内存中，以免不必要的数据访问和浪费内存空间。

```csharp
// 获取指定条件的所有简单(标量)字段的实体集（延迟加载）
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("Visible", true));

// 获取指定条件的单个实体对象（只包含特定字段）
var forum = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("ForumId", 100),
    "SiteId,ForumId,Name,Description,CoverPicturePath").FirstOrDefault();
```

<a name="usage-query-2"></a>
#### 标量查询

查询单个字段的值，可避免返回不需要的字段并避免组装实体类带来的性能损失，同时也让业务代码更简洁。

**调用说明：**

1. 泛型参数指定为返回单个字段的类型或字段的可转换类型；
1. 必须显式指定查询的实体名（即方法的 `name` 参数）；
1. 必须显式指定查询的属性名（即方法的 `schema` 参数指定为一个具体的属性名）。

```csharp
var email = this.DataAccess.Select<string>("UserProfile",
    Condition.Equal("UserId", this.User.UserId),
    "Email" // 通过 schmea 参数显式指定只获取“Email”字段值，该字段为字符串类型
).FirstOrDefault();

/* 返回标量集(IEnumerable<int>) */
var counts = this.DataAccess.Select<int>("History",
    Condition.Equal("UserId", this.User.UserId),
    "Count" // 通过 schema 参数显式指定值获取“Count”字段值，该字段为整数类型
);
```

<a name="usage-query-3"></a>
#### 多列查询

查询多个字段的值，支持返回任意实体类型，包括类、接口、结构、动态类(`ExpandoObject`)、字典。

```csharp
struct UserToken
{
    public uint UserId;
    public string Name;
}

/*
 * 注：该方法的 schema 参数可以省略或为空，实际效果一样。
 * 因为查询方法的返回字段集默认取 schmea 与返回实体类型的属性和字段集的交集。
 */
var tokens = this.DataAccess.Select<UserToken>(
    "UserProfile",
    Condition.Equal("SiteId", this.User.SiteId),
    "UserId, Name"
);
```

```csharp
/*
 * 当要访问的实体与泛型参数类型不同时，
 * 可通过 ModelAttribute 标注实体类(结构、接口)来确定其映射实体名。
 */
[Zongsoft.Data.Model("UserProfile")]
struct UserToken
{
    public uint UserId;
    public string Name;
}

// 因为返回的实体类(结构、接口)标注了映射实体名所以可缺省 name 参数，代码可简化如下所示：
var tokens = this.DataAccess.Select<UserToken>(
    Condition.Equal("SiteId", this.User.SiteId)
);
```

```csharp
/*
 * 1) 通过泛型参数指定返回实体类型为字典。
 * 2) 通过 shcmea 参数显式指定返回的字段集，如果省略或者为星号(*)则默认返回所有字段。
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
 * 通过泛型参数指定返回实体类型为 ExpandoObject，之后通过动态方式访问实体对象。
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
#### 分页查询

通过指定 [`Select`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/IDataAccess.cs) 方法中的 `paging` 参数来进行分页查询，详情请参考 [`Paging`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/Paging.cs) 分页设置类。

```csharp
// 定义查询的分页设置（譬如：第2页，每页25条）
var paging = Paging.Page(2, 25);

var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
    Condition.Equal(nameof(Thread.ForumId), 100),
    paging
);

/*
 * 查询方法调用后，paging 变量即为分页结果：
 * paging.PageCount 表示满足条件的总页数
 * paging.TotalCount 表示满足条件的总记录数
 */
```

<a name="usage-query-5"></a>
#### 排序查询

通过指定 [`Select`](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/IDataAccess.cs) 方法中的 `sortings` 参数来进行排序查询，详情请参考 [Sorting](https://github.com/Zongsoft/Zongsoft.CoreLibrary/blob/master/src/Data/Sorting.cs) 排序设置类。

```csharp
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
    Condition.Equal(nameof(Thread.ForumId), 100),
    Paging.Disable, /* 此处显式指定为不分页（当然你也可以指定为分页） */
    Sorting.Descending("TotalViews"),   // 1.倒序：累计阅读数
    Sorting.Descending("TotalReplies"), // 2.倒序：累计回帖数
    Sorting.Ascending("CreatedTime")    // 3.正序：创建时间
);
```

<a name="usage-query-6"></a>
#### 导航属性

通过 `schema` 参数显式指定导航属性(复合属性)，支持“一对一(零)”、“一对多”的导航关系，并且支持任意层次的嵌套。更多内容请参考 **S**chema 数据模式的语法说明。

<a name="usage-query-7"></a>
##### 一对一

```csharp
/*
 * 1) Thread实体的Post导航属性（关联到Post实体）的结构关系为一对一，
 * 即在映射文件(.mapping)中的 multiplicity="!"，因此它对应的SQL关联为INNER JOIN
 *
 * 2) Thread实体的MostRecentPost导航属性（关联到Post实体）的结构关系是一对零或一（默认值），
 * 即在映射文件(.mapping)中的 multiplicity="?"，因此它对应的SQL关联为LEFT JOIN
 */
var thread = this.DataAccess.Select<Thread>(
    Condition.Equal("ThreadId", 100001),
    "*,Post{*},MostRecentPost{*}"
).FirstOrDefault();
```

<a name="usage-query-8"></a>
##### 一对多

```csharp
/*
 * 1) 论坛组(ForumGroup)的Forums导航属性的结构关系为一对多，
 * 即在映射文件(.mapping)中的 multiplicity="*"，从SQL的角度看它对应一条新的查询语句。
 *
 * 2) 导航属性的任意嵌套，无论是“一对一”还是“一对多”导航属性，它们均支持任意嵌套。
 * 注意：星号(*)表示所有简单属性，不含任何导航属性，所以导航属性必须显式指定。
 */
var groups = this.DataAccess.Select<ForumGroup>(
    Condition.Equal("SiteId", this.User.SiteId),
    "*,Forums{*, Moderators{*}, MostRecentThread{*, Creator{*}}}"
);
```

<a name="usage-query-9"></a>
##### 导航约束

尤其在“一对多”的关系中，时常需要针对导航属性的结果集进行条件约束，这就是导航约束。

> 论坛(`Forum`)与论坛成员(`ForumUser`)之间是一对多的结构关系，版主(`Moderator`)是论坛成员(`ForumUser`)的一个子集，那么这样的结构关系就是通过数据映射文件中的 `complexProperty/constraints` 来表达的。
> 
> 如下面代码所示， [Forum](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Forum.cs) 实体的 `Users` 导航属性表示论坛成员的全集，而 `Moderators` 导航属性为论坛成员的一个子集，它们均关联到 `ForumUser` 实体。

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

		<!-- 导航属性的约束集 -->
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
##### 导航跳板

即指向关联实体中的另一个导航属性，它通常需要搭配使用导航约束进行过滤。以上面映射文件中的 `Forum` 实体的 `Moderators` 导航(复合)属性为例：

1. 指定该复合属性的 `role` 特性的冒号语法：冒号左边为关联的实体名，冒号右边为对应的目标导航属性。
2. 定义该复合属性的 `constraint` 约束条件。

> 说明：由于版主不受论坛成员的 `Permission` 限制，所以定义版主的实体类型为 [`UserProfile`](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/UserProfile.cs) 会更加简洁易用（避免了再通过 `ForumUser.User` 进行跳转导航），故而设置 `Moderators` 导航属性的 `role` 为 _`"ForumUser:User"`_ 即可表达这种需求。
> 
> 以上面的数据映射片段为例，感受下 [Forum](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Forum.cs) 类的 `Users` 和 `Moderators` 属性类型的不同。

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
#### 分组查询

分组查询支持关系型数据库的聚合函数，可见的未来还会针对时序数据库增加更多的统计函数。

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

上述查询方法调用大致生成如下SQL脚本：

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
### 导航条件

对导航属性关联的实体进行条件过滤。

```csharp
/*
 * 查询条件：
 * 1) 浏览记录(History)表关联的精华主题(Thread.IsValued=true)，并且
 * 2) 浏览时间（首次或最后浏览时间）位于最近30天内的。
 */
var histories = this.DataAccess.Select<History>(
    Condition.Equal("Thread.IsValued", true) & /* 导航条件 */
    (
        Condition.Between("FirstViewedTime", DateTime.Today.AddDays(-30), DateTime.Now) |
        Condition.Between("MostRecentViewedTime", DateTime.Today.AddDays(-30), DateTime.Now)
    )
);
```

上述查询方法调用大致生成如下SQL脚本：

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
#### 子查询过滤

一对多的导航属性的条件过滤对应为SQL的子查询，使用 `Exists` 运算符进行表达。

> 下面代码表示获取当前用户所属站点下，论坛可见性为“站内用户(**I**nternal)”或“所有用户(**P**ublic)”的论坛集，如果论坛可见性为“指定用户(**S**pecified)”的话，则判断当前用户是否为版主或具有论坛成员权限。

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

上述查询方法调用大致生成如下SQL脚本：

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
#### 类型转换

当数据库字段类型与之对应的实体属性类型不匹配 _（无法直接转换）_，而需要引入自定义转换逻辑的类型转换器。

譬如 Thread 表的 Tags 字段类型是 `nvarchar`，但是 [Thread](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Thread.cs) 实体类的 Tags 属性的类型是字符串数组，所以数据读写操作需要对这两种类型进行自定义转换。具体实现请参考 [TagsConverter](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/TagsConverter.cs) 类及 [Thread](https://github.com/Zongsoft/Zongsoft.Community/blob/master/src/Models/Thread.cs) 实体类的 Tags 属性定义。

<a name="usage-delete"></a>
### 删除操作

```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("Visible", false) &
    Condition.Equal("Creator.Email", "zongsoft@qq.com")
);
```

上述删除操作大致生成如下SQL脚本：

```sql
DELETE t
FROM Post AS t
    LEFT JOIN UserProfile AS u ON
        t.CreatorId = u.UserId
WHERE t.Visible=0 AND
      u.Email='zongsoft@qq.com';
```

<a name="usage-delete-cascade"></a>
#### 级联删除

支持删除“一对一(零)”或“一对多”的导航属性关联的子表记录。
```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("PostId", 100023),
    "Votes"
);
```

上述删除操作大致生成如下SQL脚本（_SQL Server_）：

```sql
CREATE TABLE #TMP
(
    PostId bigint
);

/* 删除主表，并将一对多导航属性的关联字段值导出到临时表 */
DELETE FROM Post
OUTPUT DELETED.PostId INTO #TMP
WHERE PostId=@p1;

/* 删除从表记录，条件为已删除主表的子集 */
DELETE FROM PostVoting
WHERE PostId IN
(
    SELECT PostId FROM #TMP
);
```

<a name="usage-insert"></a>
### 新增操作

```csharp
this.DataAccess.Insert("Forum", new {
    SiteId = this.User.SiteId,
    GroupId = 100,
    Name = "xxxx"
});
```

<a name="usage-insert-complex"></a>
#### 关联新增

支持“一对一”或“一对多”的导航属性同时插入。

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

上述新增方法调用大致生成如下SQL脚本：

```sql
/* 注：该SQL以MySQL驱动为例 */

/* 主表插入语句，执行一次 */
INSERT INTO Forum (SiteId,ForumId,GroupId,Name,...) VALUES (@p1,@p2,@p3,@p4,...);

/* 子表插入语句，执行多次 */
INSERT INTO ForumUser (SiteId,ForumId,UserId,Permission,IsModerator) VALUES (@p1,@p2,@p3,@p4,@p5);
```

<a name="usage-update"></a>
### 更新操作

```csharp
var user = Model.Build<UserProfile>();

user.UserId = 100;
user.Name = "Popeye";
user.FullName = "Popeye Zhong";
user.Gender = Gender.Male;

this.DataAccess.Update(user);
```

上述更新操作大致生成如下SQL脚本：

```sql
/* 注：没有修改的属性不会被生成到SET子句 */

UPDATE UserProfile SET
Name=@p1, FullName=@p2, Gender=@p3
WHERE UserId=@p4;
```

<a name="usage-update-dynamic"></a>
#### 匿名类

写入的数据可以是匿名类、动态类 _(`ExpandoObject`)_、字典 _(`IDictionary`, `IDictionary<string, object>`)_ 等。

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
#### 排除字段

显式指定的字段，或排除某些字段。

```csharp
/*
 * 通过 schmea 参数显式指定只修改 Name, Gender 两个字段，
 * 其他字段不管有没有发生更改都不予修改。
 */
this.DataAccess.Update<UserProfile>(
    user,
    "Name, Gender"
);

/*
 * 通过 schmea 参数指定可以修改所有字段，但是 CreatorId 和 CreatedTime 两个字段不予修改，
 * 就算 user 变量指向的实体对象包含并更改了这两个属性值，也不会生成它们的设置子句。
 */
this.DataAccess.Update<UserProfile>(
    user,
    "*, !CreatorId, !CreatedTime"
);
```

<a name="usage-update-complex"></a>
#### 关联更新

支持“一对一”或“一对多”的导航属性**同时写入**，对于“一对多”的导航属性，还能确保该属性值 _(集合类型)_ 以 **UPSERT** 模式进行写入。

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

上述更新方法调用大致生成如下SQL脚本(_SQL Server_)：

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
### 新增更新

新增更新操作(**Upsert**)对应于SQL中的单条元语，提供了更高的性能和一致性，为应用层提供了非常简洁的语法支撑。

> 修改 `History` 表，当指定主键值（即 `UserId=100` 并且 `ThreadId=2001` ）的记录存在，则 `Count` 字段值；否则新增一条记录，其 `Count` 字段值为 `1`。

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

上述写入操作大致生成如下SQL脚本：

```sql
/* MySQL 语法 */
INSERT INTO History (UserId,ThreadId,Count,MostRecentViewedTime) VALUES (@p1,@p2,@p3,@p4)
ON DUPLICATE KEY UPDATE Count=Count + @p3, MostRecentViewedTime=@p4;

/* SQL Server 或其他(PostgreSQL/Oracle)支持 MERGE 语句的数据库语法 */
MERGE History AS target
USING (SELECT @p1,@p2,@p3,@p4) AS source (UserId,ThreadId,[Count],MostRecentViewedTime)
ON (target.UserId=source.UserId AND target.ThreadId=source.ThreadId)
WHEN MATCHED THEN
    UPDATE SET target.Count=target.Count+@p3, MostRecentViewedTime=@p4
WHEN NOT MATCHED THEN
    INSERT (UserId,ThreadId,Count,MostRecentViewedTime) VALUES (@p1,@p2,@p3,@p4);
```

<a name="usage-other"></a>
### 其他

更多详细内容（譬如：读写分离、继承表、数据模式、映射文件、过滤器、验证器、类型转换、数据隔离）请查阅相关文档。

如果你认同我们的设计理念，请**关注**(Watch&Fork)、**点赞**(Star)这个项目。

<a name="performance"></a>
## 性能

我们希望提供最佳的**综合性价比**，不会为了某些单项测评分值而妥协设计目标。我们认为对于一个ORM数据访问引擎来说，性能的关注点主要(不限)有这几个要素：

1. 生成简洁高效的SQL脚本，并尽可能利用特定数据库的最新SQL语法；
2. 数据查询结果的实体组装(**P**opulate)过程必须高效；
3. 避免反射，有效的语法树缓存。

得益于 __“以声明方式来表达数据结构关系”__ 的语义化设计理念，相对于命令式设计而言，它将程序意图更加聚焦，天然地更容易将语义转换为语法树进而表示成不同数据提供程序的SQL脚本，并且各个步骤的优化空间也更宽松和自由。

实现层面采用 **E**mitting 动态编译技术对实体组装(**P**opulate)、数据参数绑定等进行预热处理，可通过 [DataPopulator](https://github.com/Zongsoft/Zongsoft.Data/blob/master/src/Common/DataPopulatorProviderFactory.cs) 等相关类了解。

<a name="contribution"></a>
## 贡献

请不要在项目的 **I**ssues 中提交询问(**Q**uestion)以及咨询讨论，**I**ssue 是用来报告问题(**B**ug)和功能特性(**F**eature)。如果你希望参与贡献，欢迎提交 代码合并请求(_[**P**ull**R**equest](https://github.com/Zongsoft/Zongsoft.Data/pulls)_) 或问题反馈(_[**I**ssue](https://github.com/Zongsoft/Zongsoft.Data/issues)_)。

对于新功能，请务必创建一个功能反馈(_[**I**ssue](https://github.com/Zongsoft/Zongsoft.Data/issues)_)来详细描述你的建议，以便我们进行充分讨论，这也将使我们更好的协调工作防止重复开发，并帮助你调整建议或需求，使之成功地被接受到项目中。

欢迎你为我们的开源项目撰写文章进行推广，如果需要我们在官网(_[http://zongsoft.com/blog](http://zongsoft.com/blog)_) 中转发你的文章、博客、视频等可通过 [**电子邮件**](mailto:zongsoft@qq.com) 联系我们。

> 强烈推荐阅读 [《提问的智慧》](https://github.com/ryanhanwu/How-To-Ask-Questions-The-Smart-Way/blob/master/README-zh_CN.md)、[《如何向开源社区提问题》](https://github.com/seajs/seajs/issues/545) 和 [《如何有效地报告 Bug》](http://www.chiark.greenend.org.uk/~sgtatham/bugs-cn.html)、[《如何向开源项目提交无法解答的问题》](https://zhuanlan.zhihu.com/p/25795393)，更好的问题更容易获得帮助。


<a name="sponsor"></a>
## 支持赞助

非常期待您的支持与赞助，可以通过下面几种方式为我们提供必要的资金支持：

1. 关注 **Zongsoft 微信公众号**，对我们的文章进行打赏；
2. 加入 [**Zongsoft 知识星球号**](https://t.zsxq.com/2nyjqrr)，可以获得在线问答和技术支持；
3. 如果您的企业需要现场技术支持与辅导，又或者需要特定新功能、即刻的错误修复等请[发邮件](mailto:zongsoft@qq.com)给我。

[![微信公号](https://raw.githubusercontent.com/Zongsoft/Guidelines/master/zongsoft-qrcode%28wechat%29.png)](http://weixin.qq.com/r/zy-g_GnEWTQmrS2b93rd)

[![知识星球](https://raw.githubusercontent.com/Zongsoft/Guidelines/master/zongsoft-qrcode%28zsxq%29.png)](https://t.zsxq.com/2nyjqrr)


<a name="license"></a>
## 授权协议

本项目采用 [LGPL](https://opensource.org/licenses/LGPL-2.1) 授权协议。
