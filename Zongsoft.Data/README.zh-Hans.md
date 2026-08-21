# Zongsoft.Data 数据引擎

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

<a name="abstract"></a>
## 概述

[Zongsoft.Data](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data) 是一个类 [GraphQL](https://graphql.cn/) 风格的 **ORM**(**O**bject/**R**elational **M**apping) 数据访问框架。

它的核心思路很简单：用声明式写法描述要访问的数据形状和实体关系，由数据引擎生成 SQL。这样，大多数查询、写入和导航访问都不需要手写 SQL 或类 SQL 字符串。

<a name="feature"></a>
## 特性

- 支持严格的 POCO 对象，不依赖特性或注解；
- 支持读写分离；
- 支持继承表的数据操作；
- 支持按业务模块隔离映射，并提供扩展机制；
- 无需手写 SQL，即可完成导航、过滤、分页、分组和聚合；
- 符合面向对象开发习惯，容易理解和上手；
- 兼顾性能、可维护性和易用性；
- 依赖很少，通常只需要 ADO.NET 和对应的原生 ADO.NET 驱动。

<a name="driver"></a>
## 驱动

| **驱动程序** | **项目路径** | **状态** |
| --- | --- | :---: |
MySQL | [/drivers/mysql](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/mysql) | _**A**vailable_ |
SQL Server | [/drivers/mssql](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/mssql) | _**A**vailable_ |
PostgreSQL | [/drivers/postgres](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/postgres) | _**A**vailable_ |
SQLite | [/drivers/sqlite](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/sqlite) | _**A**vailable_ |
DuckDB | [/drivers/duckdb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/duckdb) | _**A**vailable_ |
ClickHouse | [/drivers/clickhouse](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/clickhouse) | _**A**vailable_ |
InfluxDB | [/drivers/influx](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/influx) | _**A**vailable_ |
TDengine | [/drivers/tdengine](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/tdengine) | _**A**vailable_ |

> 提示：如果需要未列出的驱动或商业技术支持，请联系我们（[zongsoft@qq.com](mailto:zongsoft@qq.com)）。


<a name="environment"></a>
### 开发环境

- .NET 8.0
- .NET 9.0
- .NET 10.0<br />

<a name="download"></a>
## 下载

- **源码编译方式**

源码编译时，建议在非系统分区建立一个 **_Zongsoft_** 目录，并将 [Guidelines](https://github.com/Zongsoft/guidelines)、[Zongsoft.Core](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core) 和 [Zongsoft.Data](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data) 克隆到该目录中。

<a name="schema"></a>
## 数据模式

数据模式(**S**chema)是一种 DSL(**D**omain **S**pecific **L**anguage)，用来描述查询或写入 _(**D**elete/**I**nsert/**U**pdate/**U**psert)_ 时要处理哪些字段。它的写法类似 [GraphQL](https://graphql.cn/)，但不需要预先定义服务端 GraphQL 类型。它可用于选择字段、包含导航属性、控制级联范围等。

数据访问方法中的 `schema` 参数就是数据模式文本。

<a name="schema-syntax"></a>
### 语法定义

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

<a name="schema-overview"></a>
#### 说明

- 星号(`*`)：表示包含所有简单属性，不包含导航属性；如果要包含导航属性，必须显式指定。
- 叹号(`!`)：表示排除。单独的 `!` 表示清除当前层级的全部成员；`!名称` 表示排除指定名称的属性。
- 句点(`.`)用于连接嵌套成员名。`Department.Manager.Name` 等价于 `Department{Manager{Name}}`，点路径可以与花括号混用，句点两侧允许空白。
- 以映射导航属性作为终止成员时，自动包含该导航的简单属性，因此 `Department`、`Department.*` 和 `Department{*}` 会生成相同的成员树。`.*` 只能位于路径末尾，之后不能再接路径段。
- `!Department.Manager.Secret` 这类点路径排除会逐段验证成员，仅移除目标成员。`!Department` 会移除整个导航成员，等价于 `Department{!}`。`!*` 和 `!路径.*` 都是非法格式。
- 标识符由字母、数字和下划线组成，不能以数字开头，不区分大小写。
- 多个成员之间使用逗号(`,`)分隔；子模式 _（大括号内的部分）_ 沿用相同的语法，因此可以任意层级嵌套。解析器会忽略连续逗号产生的空段以及首尾逗号。
- 标识符内部不能含有空白字符；但成员之间、星号之后、标识符与子模式/排序/限量符号之间可以包含空白字符，例如 `Users {*}`、`Users :20`、`* , Users`。
- 限量数字内部以及冒号(`:`)之后不允许空白；排序括号内、字段内部不允许空白，但排序字段之间 _（逗号之后）_ 允许空白。
- 限量或排序会终止点路径。例如 `Departments:10(~Name)` 会包含该导航的简单属性，而 `Departments:10.Manager` 和 `Departments:10(~Name).Manager` 都是非法格式。

<a name="schema-paging"></a>
#### 限量与排序

限量写在成员名之后、以冒号(`:`)开头，表示一对多成员最多加载的记录数：

| 写法 | 含义 |
| --- | --- |
| 缺省 | 不限 _（默认值）_ |
| `:N` | `N > 0` 时最多加载 `N` 条 |
| `:0` | 不限 |
| `:*` | 不限 |

解析后的限量是一个整数，任何小于或等于零的值都表示不限；规范化模式文本会省略不限的限量。模式表达式的冒号后只接受无符号数字或 `*`。

排序写在可选限量之后、以一对圆括号包裹；排序字段之间使用逗号分隔。`~` 或 `-` 前缀表示倒序，`+` 前缀或无前缀表示正序：

```
Users:20(-CreatedTime,+Grade){*}
```

每个排序字段都可以单独使用前缀，`+CreatedTime` 与 `CreatedTime` 完全等价。同一字段重复声明时，以最后一次声明的方向和位置为准。

<a name="schema-sample"></a>
### 示例说明

> 提示：以下示例中的属性名取自论坛项目的实体，仅用于演示语法，例如 `Creator` 是 `Thread` 实体的导航属性，`Users` 是 `Forum` 实体的导航属性。

```graphql
*, !CreatorId, !CreatedTime
```
> 表示所有简单属性，但排除 `CreatorId` 和 `CreatedTime`。

```graphql
*, Creator{*}
```
> 表示所有简单属性，并包含 `Creator` 导航属性的所有简单属性。

以下写法等价：

```graphql
*, Creator
*, Creator.*
*, Creator{*}
```

```graphql
*, Creator{Name,Nickname}
```
> 表示所有简单属性，并且只加载 `Creator` 导航属性的 `Name` 和 `Nickname`。

```graphql
*, Users{*}
```
> 表示所有简单属性，并包含 `Users` 集合导航属性 _（一对多）_，该集合不排序也不限量。

```graphql
*, Users:1{*}
```
> 表示所有简单属性，并包含 `Users` 集合导航属性 _（一对多）_，该集合最多加载 1 条。

```graphql
*, Users:20{*}
```
> 表示所有简单属性，并包含 `Users` 集合导航属性 _（一对多）_，该集合最多加载 20 条。

```graphql
*, Users:*{*}
```
> 表示所有简单属性，并包含 `Users` 集合导航属性 _（一对多）_，该集合显式不限量。

```graphql
*, Users:20(-CreatedTime,+Grade){*}
```
> 表示所有简单属性，并包含 `Users` 集合导航属性 _（一对多）_；该集合先按 `CreatedTime` 倒序、`Grade` 正序排序，再最多加载 20 条。

-----

💡 嵌套花括号可以改写为点路径，也可以与点路径混用。以下表达式会生成相同的成员树：

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
### 计算成员

如果 schema 中显式声明的成员没有映射，但对应模型中定义了同名的公共实例属性或字段，仍可将它作为计算成员使用。计算成员会参与返回的模型形状，但不会生成数据库字段；既不存在于映射、也不存在于模型中的名称是非法成员。通配符 `*` 只包含映射的简单属性，不会自动加入计算成员。

💡 提示：计算属性依赖的映射字段必须显式包含在 schema 中。


<a name="mapping"></a>
## 映射文件

数据映射文件是扩展名为 `.mapping` 的 XML 文件，用来定义实体、表、字段、主键和导航关系等元数据。**不要**把大型应用的所有元数据都写在一个映射文件里；应按业务模块分别定义映射文件，以保持模块隔离。

我们提供 [Zongsoft.Data.xsd](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.xsd) 这个 XML Schema 文件，用于给手写映射文件提供智能提示和校验。

映射文件的根节点是 `schema`，每个 `container` 表示一个元数据命名空间。一般一个业务模块定义一个容器，其 `name` 与模块名保持一致。

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

常用映射元素如下：

- `entity` 定义实体到数据表的映射。`table` 是物理表名（省略时默认使用 `命名空间_实体名`，命名空间为空则使用实体名，也支持 `alias` 作为表名的别名写法）；`inherits` 指向父实体；`driver` 将实体限定到指定数据驱动；`immutable="true"` 表示除新增外不允许变更。
- `property` 定义简单属性到字段的映射。常用属性包括 `type`、`field`、`nullable`、`length`、`precision`、`scale`、`default`、`sequence`、`sortable`、`immutable`。
- `sequence="*"` 表示使用数据库内置自增或序列；`sequence="#"` 表示使用 Zongsoft 默认外部序号器；`sequence="#Name"` 表示指定名称的外部序号器；`sequence="#Name@seed/interval"` 可同时指定序号器的种子值与递增量；`sequence="#(ParentId)"` 表示按指定引用属性分组的外部序号器；`sequence="Entity:Property"` 表示引用另一实体属性的序号器。
- `complexProperty` 定义导航属性。`port` 指向目标实体，也可以指向目标实体的导航属性，譬如 `ForumUser:User`。`multiplicity` 支持 `?`（一对零或一，默认）、`!`（一对一）、`*`（一对多）；`link` 定义外键属性与当前实体的关联（`anchor` 指定本体实体侧的锚点，省略时与 `port` 同名）；`constraints` 可添加固定的导航过滤条件（`actor` 省略时按多重性推断：一对多默认为 `Foreign`，其余为 `Principal`）。
- `command` 定义命名 SQL 命令或存储过程，可通过 `Execute`、`Execute<T>`、`ExecuteScalar` 调用。`type` 支持 `text`（默认）与 `procedure`；`mutability` 用来声明命令对数据的读写特性，它不只是描述性元数据，也是配置读写分离后数据源选择器的路由依据：`none` 选择可读数据源，`delete`、`insert`、`update`、`upsert` 选择可写数据源。选择器不会通过分析 SQL 文本来推断读写特性。加载器对枚举值的解析不区分大小写，但为通过 XSD 校验，建议统一使用小写。

```xml
<command name="Forum.GetStatistics" type="text" mutability="none">
    <parameter name="SiteId" type="uint" />
    <parameter name="ForumId" type="ushort" />
    <script driver="MySql"><![CDATA[
        SELECT TotalThreads, TotalPosts
        FROM Discussions_Forum
        WHERE SiteId=@SiteId AND ForumId=@ForumId
    ]]></script>
</command>
```


> **启用映射文件的XML智能提示：**
>
> 将 [Zongsoft.Data.xsd](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.xsd) 和 [Zongsoft.Data.catalog.xml](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/Zongsoft.Data.catalog.xml) 文件拷贝到 **V**isual **S**tudio 的 XML Schemas 模板目录中，譬如：
> - **V**isual **S**tudio 2026 _(Enterprise Edition)_ <br />
> 	`C:\Program Files\Microsoft Visual Studio\18\Enterprise\Xml\Schemas`


> 虽然可以用工具生成映射文件，但我们仍建议手写：
> - 数据结构和关系是系统的基础。数据库表是这种关系的物理形式，映射文件则描述上层实体如何对应到底层表。
> - 映射文件应由系统架构师或模块负责人统一维护。`inherits`、`immutable`、`sortable`、`sequence` 以及导航属性等设置，会直接影响应用层代码。


<a name="connection"></a>
## 连接配置

数据连接配置项的名称必须与 `DataAccess` 的名称匹配。一个 `DataAccess` 可以配置多个数据源，使用井号 `#` 分隔 `DataAccess` 名称和数据源名称，譬如 `Discussions#master`、`Discussions#slave_1`。这种写法主要用于读写分离。

> - **MySQL** 连接字符串参考：https://mysqlconnector.net/connection-options/
> - **ADO.NET** 连接字符串语法：https://learn.microsoft.com/zh-cn/dotnet/framework/data/adonet/connection-string-syntax

- 单数据源的配置：
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

- 多数据源（读写分离）的配置：
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
## 使用

所有数据操作都通过[核心库](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core)中的 [`Zongsoft.Data.IDataAccess`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) 接口完成，支持下列操作：

- 计数操作： `int Count(...)` 
- 聚合操作： `TValue? Aggregate(...)`
- 存在操作： `bool Exists(...)` 
- 执行操作： `IEnumerable<T> Execute<T>(...)` `object ExecuteScalar(...)` 
- 导入操作： `int Import(...)`
- 删除操作： `int Delete(...)` 
- 新增操作： `int Insert(...)` `int InsertMany(...)` 
- 更新操作： `int Update(...)` `int UpdateMany(...)` 
- 新增更新： `int Upsert(...)` `int UpsertMany(...)` 
- 查询操作： `IEnumerable<T> Select<T>(...)` 

**提醒：**
> 下面的范例均基于 [Zongsoft.Discussions](https://github.com/Zongsoft/discussions) 开源项目。它是一个完整的论坛社区后台程序。阅读范例前，建议先查看该项目的[数据库表结构设计文档](https://github.com/Zongsoft/discussions/blob/main/database/Zongsoft.Discussions.md)，这样更容易理解实体之间的关系。

<a name="operand"></a>
### 操作元

操作元(`Operand`)可用于条件(`Condition`)和写入字段的值，主要包括：
- 常量操作元 `ConstantOperand<T>`
- 字段操作元 `FieldOperand`
- 函数操作元 `FunctionOperand`
- 聚合操作元 `AggregateOperand`
- 一元操作元 `UnaryOperand`，包括：
> - `!` 逻辑非
> - `~` 按位取反
> - `-` 算术负号
- 二元操作元 `BinaryOperand`，包括：
> - `+` 加法
> - `-` 减法
> - `*` 乘法
> - `/` 除法
> - `%` 取模
> - `&` 逻辑与或按位与
> - `|` 逻辑或或按位或
> - `^` 异或

#### 范例

- 字段引用
```csharp
var forums = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("MostRecentThreadAuthorId", Operand.Field("MostRecentPostAuthorId"))
);
```

- 常量操作
```csharp
/* 以下两种写法等价 */
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

- 一元运算
```csharp
/* 一元运算：算术取反 */
this.DataAccess.Update<OrderDetail>(
    new {
        Discount = -Operand.Field("Discount")
    },
    Condition.LessThan("Discount", 0)
);

/* 一元运算：逻辑取反 */
this.DataAccess.Update<Thread>(
    new {
        Visible = !Operand.Field("Visible")
    },
    Condition.Equal("ForumId", 404)
);
```

- 二元运算
```csharp
/* 递增 */
this.DataAccess.Update<Thread>(
    new {
        TotalReplies = Operand.Field("TotalReplies") + 1
    },
    Condition.Equal("ThreadId", 404)
);

/* 算术运算 */
this.DataAccess.Update<OrderDetail>(
    new {
        Amount = Operand.Field("UnitPrice") * Operand.Field("Quantity") - Operand.Field("Discount")
    },
    Condition.Equal("OrderId", 404)
);

/* 位与运算 */
this.DataAccess.Select<User>(
    Condition.Equal(Operand.Field("Flags") & 0x74, 0x74)
);
```

- 函数运算
```csharp
this.DataAccess.Update<OrderDetail>(
    new {
        Quantity = Operand.Function("Abs", Operand.Field("Quantity")),
        UnitPrice = Operand.Function("Abs", Operand.Field("UnitPrice"))
    },
    Condition.Equal("OrderId", 404)
);
```

- 聚合运算
```csharp
/* 以下两种写法等价 */
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
/* 以下三种写法等价 */
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
### 条件

`Condition` 是 `Select`、`Exists`、`Aggregate`、`Delete`、`Update`、`Upsert` 通用的条件表达对象。它支持等值、比较、`Like`、`Between`、`In`、`NotIn`、`Exists`、`NotExists` 等条件，可用 `&` 和 `|` 组合多个条件。

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

对于搜索条件 DTO，可使用 `Criteria.Transform(...)` 将模型中已变更的成员转换成查询条件。`ConditionAttribute` 可用于重命名目标成员、指定运算符、忽略空值，或接入自定义条件转换器。

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
### 查询操作

<a name="usage-query-1"></a>
#### 简单查询

- 默认返回全部简单字段，可通过 `schema` 参数显式指定返回哪些字段。
- 查询结果是延迟加载的，遍历结果集或调用 LINQ 的 `ToList()`、`First()` 等方法时才会真正访问数据库。
- **注意：** 查询默认不分页。面对大结果集时，不要随意调用 `ToList()`、`ToArray()` 把全部数据加载到内存。

```csharp
// 查询满足条件的实体集，默认加载全部简单字段（延迟加载）。
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("Visible", true));

// 查询单个实体，并只加载指定字段。
var forum = this.DataAccess.Select<Forum>(
    Condition.Equal("SiteId", this.User.SiteId) &
    Condition.Equal("ForumId", 100),
    "SiteId,ForumId,Name,Description,CoverPicturePath").FirstOrDefault();
```

<a name="usage-query-exists"></a>
#### 存在与聚合查询

只需要判断记录是否存在时使用 `Exists`。只需要聚合值时，可使用 `Count`、`Sum`、`Average`、`Maximum`、`Minimum`、`Median`、`Deviation`、`Variance` 等扩展方法。

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
#### 标量查询

标量查询只返回单个字段的值，可避免读取无用字段，也避免组装完整实体的开销。

**调用说明：**

1. 泛型参数指定为字段类型，或字段可转换到的类型；
1. 通过方法的 `name` 参数显式指定实体名 _（须为映射文件中注册的限定名，即 `容器名.实体名`；容器名为空时可直接使用实体名，下例中的 `Discussions.UserProfile` 即论坛模块映射文件中的限定名）_；
1. 通过方法的 `schema` 参数显式指定一个具体属性名。

```csharp
var email = this.DataAccess.Select<string>("Discussions.UserProfile",
    Condition.Equal("UserId", this.User.UserId),
    "Email" // 只获取 Email 字段，该字段为字符串类型
).FirstOrDefault();

/* 返回标量集(IEnumerable<uint>) */
var counts = this.DataAccess.Select<uint>("Discussions.History",
    Condition.Equal("UserId", this.User.UserId),
    "ViewedCount" // 只获取 ViewedCount 字段
);
```

<a name="usage-query-3"></a>
#### 多列查询

多列查询可返回多个字段，并支持多种目标类型：类、接口、结构、动态对象(`ExpandoObject`)和字典。

> **注意：** 导航属性 _（复合属性）_ 只会在使用实体模型类 _（类/接口/结构）_ 作为目标类型时填充；以字典或 `ExpandoObject` 为目标类型时，只返回简单字段，导航属性不会出现在结果中。

```csharp
struct UserToken
{
    public uint UserId;
    public string Name;
}

/*
 * 注：这里可以省略 schema 参数，或将其设为空。
 * 引擎会取实体元数据与目标类型成员的交集作为返回字段。
 */
var tokens = this.DataAccess.Select<UserToken>(
    "Discussions.UserProfile",
    Condition.Equal("SiteId", this.User.SiteId),
    "UserId, Name"
);
```

```csharp
/*
 * 当目标类型名与实体名不一致时，
 * 可通过 ModelAttribute 指定它对应的映射实体名（限定名）。
 */
[Zongsoft.Data.Model("Discussions.UserProfile")]
struct UserToken
{
    public uint UserId;
    public string Name;
}

// 因为目标类型已声明映射实体名，所以可省略 name 参数：
var tokens = this.DataAccess.Select<UserToken>(
    Condition.Equal("SiteId", this.User.SiteId)
);
```

```csharp
/*
 * 1) 泛型参数指定每行以字典形式返回。
 * 2) schema 参数指定返回字段；省略或写为星号(*)时，默认返回所有字段。
 */
var items = this.DataAccess.Select<IDictionary<string, object>>(
    "Discussions.UserProfile",
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
 * 泛型参数指定为 ExpandoObject 后，可用动态方式访问返回对象。
 */
var items = this.DataAccess.Select<System.Dynamic.ExpandoObject>("Discussions.UserProfile");

foreach(dynamic item in items)
{
    Console.WriteLine(item.UserId); // OK
    Console.WriteLine(item.Name);   // OK
    Console.WriteLine(item.Fake);   // Compiled successfully, but runtime error
}
```

<a name="usage-query-4"></a>
#### 分页查询

向 [`Select`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) 方法传入 `paging` 参数即可进行分页查询，详情请参考 [`Paging`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/Paging.cs)。

```csharp
// 第 2 页，每页 25 条。
var paging = Paging.Page(2, 25);

var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
    Condition.Equal(nameof(Thread.ForumId), 100),
    paging
);

/*
 * 查询返回后，paging 对象会包含分页结果摘要：
 * paging.Count 表示总页数。
 * paging.Total 表示总记录数。
 */
```

<a name="usage-query-5"></a>
#### 排序查询

向 [`Select`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/IDataAccess.cs) 方法传入 `Sorting` 即可进行排序查询，详情请参考 [Sorting](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Data/Sorting.cs)。

```csharp
var threads = this.DataAccess.Select<Thread>(
    Condition.Equal(nameof(Thread.SiteId), this.User.SiteId) &
    Condition.Equal(nameof(Thread.ForumId), 100),
    Paging.Disabled, /* 此处指定不分页；也可以传入具体的分页设置。 */
    Sorting.Descending("TotalViews"),   // 1.倒序：累计阅读数
    Sorting.Descending("TotalReplies"), // 2.倒序：累计回帖数
    Sorting.Ascending("CreatedTime")    // 3.正序：创建时间
);
```

<a name="usage-query-6"></a>
#### 导航属性

通过 `schema` 参数显式指定导航属性(复合属性)。它支持一对一、一对零或一、一对多关系，也支持多层嵌套。

<a name="usage-query-7"></a>
##### 一对一

```csharp
/*
 * 1) Thread.Post 是关联到 Post 的一对一导航属性，
 *    在映射文件(.mapping)中为 multiplicity="!"，因此生成的 SQL 使用 INNER JOIN。
 *
 * 2) Thread.MostRecentPost 是关联到 Post 的一对零或一导航属性，
 *    在映射文件(.mapping)中为 multiplicity="?"，因此生成的 SQL 使用 LEFT JOIN。
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
 * 1) ForumGroup.Forums 是一对多导航属性，
 *    在映射文件(.mapping)中为 multiplicity="*"，因此它会通过单独的 SQL 查询加载。
 *
 * 2) 一对一和一对多导航属性都支持嵌套。
 * 注意：星号(*)只表示所有简单属性，不包含导航属性；导航属性必须显式指定。
 */
var groups = this.DataAccess.Select<ForumGroup>(
    Condition.Equal("SiteId", this.User.SiteId),
    "*,Forums{*, Moderators{*}, MostRecentThread{*, Creator{*}}}"
);
```

<a name="usage-query-9"></a>
##### 导航约束

一对多导航属性经常需要过滤子集合，这类过滤就是导航约束。

> 论坛(`Forum`)与论坛成员(`ForumUser`)是一对多关系，版主是论坛成员的一个子集。这个子集通过映射文件中的 `complexProperty/constraints` 表达。
>
> 在下面的示例中，[Forum](https://github.com/Zongsoft/discussions/blob/main/src/Models/Forum.cs) 实体的 `Users` 导航属性表示全部论坛成员，`Moderators` 导航属性只表示 `IsModerator` 为 `true` 的成员。

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

		<!-- 导航属性的约束集 -->
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
##### 导航跳板

导航跳板表示：当前导航属性并不直接返回关联实体，而是返回关联实体上的另一个导航属性。以上面 `Forum.Moderators` 为例：

1. 在 `port` 特性中使用冒号语法：冒号左边是关联实体名，右边是要跳转到的目标导航属性。
2. 通过 `constraint` 约束筛选要包含的关联行。

> 说明：版主列表并不需要暴露论坛成员的 `Permission` 字段，直接返回 [`UserProfile`](https://github.com/Zongsoft/discussions/blob/main/src/Models/UserProfile.cs) 会更简洁，也避免调用方再通过 `ForumUser.User` 跳转。因此 `Moderators` 设置为 `port="ForumUser:User"`。
>
> 对照上面的映射片段，可以看到 [Forum](https://github.com/Zongsoft/discussions/blob/main/src/Models/Forum.cs) 类中 `Users` 和 `Moderators` 的属性类型不同：

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
  "*, Users{*, User{Name,Email,Avatar}}, Moderators{Name,Email,Avatar}"
).FirstOrDefault();

// moderator 的类型是 UserProfile（导航跳板直接返回 UserProfile，因此只需列出其简单属性）。
foreach(var moderator in forum.Moderators)
{
  Console.Write(moderator.Name);
  Console.Write(moderator.Email);
  Console.Write(moderator.Avatar);
}

// member 的类型是 ForumUser，可通过其 User 导航属性再访问 UserProfile。
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

分组查询支持关系型数据库的聚合函数。

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

上述查询大致生成如下 SQL：

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

导航条件用于按关联实体的字段进行过滤。

```csharp
/*
 * 查询浏览记录：
 * 1) 关联的主题为精华主题(Thread.IsValued=true)；
 * 2) 首次或最后浏览时间位于最近 30 天内。
 */
var histories = this.DataAccess.Select<History>(
    Condition.Equal("Thread.IsValued", true) & /* 导航条件 */
    (
        Condition.Between("FirstViewedTime", DateTime.Today.AddDays(-30), DateTime.Now) |
        Condition.Between("LastViewedTime", DateTime.Today.AddDays(-30), DateTime.Now)
    )
);

/* 与上面的查询等价，只是改用 Range.Timing 构造时间范围。 */
var histories = this.DataAccess.Select<History>(
    Condition.Equal("Thread.IsValued", true) & /* 导航条件 */
    (
        Condition.Between("FirstViewedTime", Range.Timing.Last(30, 'D')) |
        Condition.Between("LastViewedTime", Range.Timing.Last(30, 'D'))
    )
);
```

上述查询大致生成如下 SQL：

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
#### 子查询过滤

一对多导航属性的条件过滤可用 `Exists` 运算符表达，生成 SQL 时对应为子查询。

> 下面的查询获取当前用户所属站点下的论坛：`Internal` 和 `All` 可见性的论坛直接返回；如果可见性为 `Specified`，则要求当前用户是版主或拥有论坛成员权限。

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

上述查询大致生成如下 SQL：

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

当数据库字段类型无法直接转换为实体属性类型时，需要使用类型转换器。

譬如 `Thread` 表的 `Tags` 字段类型是 `nvarchar`，而 [Thread](https://github.com/Zongsoft/discussions/blob/main/src/Models/Thread.cs) 实体的 `Tags` 属性是字符串数组。读写这个属性时就需要自定义转换。具体实现请参考 [TagsConverter](https://github.com/Zongsoft/discussions/blob/main/src/Models/TagsConverter.cs) 以及 [Thread](https://github.com/Zongsoft/discussions/blob/main/src/Models/Thread.cs) 中的 `Tags` 属性定义。

<a name="usage-execute"></a>
### 执行操作

`Execute` 用于执行映射文件中定义的命名 `command`。适合 SQL 语句、存储过程，以及无法自然归入某个实体 CRUD 操作的命令。声明的 `mutability` 是数据源选择器进行读写路由的依据：`mutability="none"` 选择可读数据源，`insert`、`update`、`delete`、`upsert` 选择可写数据源；选择器不会检查 SQL 脚本来判断读写特性。未声明 `mutability` 的命令会被视为可写命令 _（按 `Delete|Insert|Update` 处理）_，如无必要不必显式声明。

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

如果是存储过程，请在映射文件中设置 `type="procedure"`（`alias` 指定存储过程的实际名称）。输出参数和返回参数会在命令执行后写回传入的 `Parameter` 对象；参数方向 `direction` 支持 `input`、`output`、`both`、`return`（加载器也接受 `in`、`out`、`result` 等简写，但为通过 XSD 校验建议使用标准写法）。

```xml
<command name="Forum.RefreshStatistics" alias="Discussions_Forum_RefreshStatistics" type="procedure" mutability="update">
    <parameter name="SiteId" type="uint" />
    <parameter name="ForumId" type="uint" />
    <parameter name="Total" type="int" direction="output" />
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
### 删除操作

```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("Visible", false) &
    Condition.Equal("Creator.Email", "zongsoft@qq.com")
);
```

上述删除大致生成如下 SQL：

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

级联删除可删除通过一对零或一、一对一、一对多导航属性关联的子表记录。
```csharp
this.DataAccess.Delete<Post>(
    Condition.Equal("PostId", 100023),
    "Votes"
);
```

上述删除大致生成如下 SQL（_SQL Server_）：

```sql
CREATE TABLE #TMP
(
    PostId bigint
);

/* 删除主表记录，并将关联键值写入临时表。 */
DELETE FROM Post
OUTPUT DELETED.PostId INTO #TMP
WHERE PostId=@p1;

/* 根据临时表中的键值删除从表记录。 */
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

<a name="usage-insert-options"></a>
#### 新增选项

`DataInsertOptions` 用来控制新增操作的专用行为：

- `IgnoreConstraint()` 忽略数据库约束冲突，譬如主键或唯一键重复。发生冲突的记录会被跳过，而不是抛出冲突异常。
- `Sequence(...)` 控制序号字段如何生成。如果序号值由调用方提供，不希望数据引擎生成，请使用 `DataSequenceBehavior.Never`。
- `Return(...)` 在当前驱动支持返回值时，请求数据库返回生成值。
- `SuppressValidator()` 禁用本次新增操作注册的数据验证器。

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

上述新增大致生成如下 SQL：

```sql
/* MySQL、ClickHouse */
INSERT IGNORE INTO ForumUser (SiteId,ForumId,UserId,Permission) VALUES (@p1,@p2,@p3,@p4);

/* PostgreSQL、SQLite */
INSERT INTO ForumUser (SiteId,ForumId,UserId,Permission) VALUES (@p1,@p2,@p3,@p4)
ON CONFLICT DO NOTHING;
```

如果同时显式提供了序号字段的值，可链式设置序号选项：

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
#### 关联新增

一对一和一对多导航属性可以随主实体一起插入。

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

上述新增大致生成如下 SQL：

```sql
/* 主表插入语句 */
INSERT INTO Forum (SiteId,ForumId,GroupId,Name,...) VALUES (@p1,@p2,@p3,@p4,...);

/* 从表插入语句（批量） */
INSERT INTO ForumUser (SiteId,ForumId,UserId,Permission,IsModerator) VALUES (...),(...),(...);
```

<a name="usage-import"></a>
### 导入操作

`Import` 用于批量导入数据。调用时传入目标实体名、数据集合，以及需要导入的成员列表。

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

如果导入时希望跳过重复记录，可使用 `DataImportOptions.IgnoreConstraint()`。如果过滤器或服务还需要读取本次操作的上下文标记，也可以继续链式设置 `Parameter(...)`：

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
### 更新操作

```csharp
var user = Model.Build<UserProfile>();

user.UserId = 100;
user.Name = "Popeye";
user.Nickname = "Popeye Zhong";
user.Gender = Gender.Male;

this.DataAccess.Update(user);
```

上述更新大致生成如下 SQL：

```sql
/* 注：未修改的属性不会生成到 SET 子句。 */

UPDATE UserProfile SET
Name=@p1, Nickname=@p2, Gender=@p3
WHERE UserId=@p4;
```

<a name="usage-update-dynamic"></a>
#### 匿名类

写入的数据可以是匿名对象、动态对象 _(`ExpandoObject`)_ 或字典 _(`IDictionary`, `IDictionary<string, object>`)_。

```csharp
this.DataAccess.Update<UserProfile>(
    new {
        Name="Popeye",
        Nickname="Popeye Zhong",
        Gender=Gender.Male,
    },
    Condition.Equal("UserId", 100)
);
```

<a name="usage-update-schema"></a>
#### 排除字段

通过 `schema` 可指定要更新的字段，也可排除某些字段。

```csharp
/*
 * 只更新 Name、Gender 两个字段。
 * 其他字段即使发生变化，也不会被写入。
 */
this.DataAccess.Update<UserProfile>(
    user,
    "Name, Gender"
);

/*
 * 星号(*)表示允许更新所有字段，但 CreatorId 和 CreatedTime 被排除。
 * 即使 user 对象中包含这两个属性值，也不会为它们生成 SET 子句。
 */
this.DataAccess.Update<UserProfile>(
    user,
    "*, !CreatorId, !CreatedTime"
);
```

<a name="usage-update-complex"></a>
#### 关联更新

一对一和一对多导航属性可以和主实体**一起写入**。对于一对多集合，子记录会按 **UPSERT** 语义写入。

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

上述更新大致生成如下 SQL(_SQL Server_)：

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
### 新增更新

新增更新(**Upsert**)表示：记录不存在时插入，记录已存在时更新。数据引擎会尽量使用数据库提供的原生 upsert 能力。

> 在下面的例子中，如果 `History` 表中已存在 `UserId=100` 且 `ThreadId=2001` 的记录，则递增 `ViewedCount`；否则新增一条记录，并将 `ViewedCount` 设为 `1`。

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

上述写入大致生成如下 SQL：

```sql
/* MySQL 语法 */
INSERT INTO History (UserId,ThreadId,ViewedCount,LastViewedTime) VALUES (@p1,@p2,@p3,@p4)
ON DUPLICATE KEY UPDATE ViewedCount=ViewedCount + @p3, LastViewedTime=@p4;

/* SQL Server 或 PostgreSQL 支持 MERGE 语句的数据库语法 */
MERGE History AS target
USING (SELECT @p1,@p2,@p3,@p4) AS source (UserId,ThreadId,ViewedCount,LastViewedTime)
ON (target.UserId=source.UserId AND target.ThreadId=source.ThreadId)
WHEN MATCHED THEN
    UPDATE SET target.ViewedCount=target.ViewedCount+@p3, LastViewedTime=@p4
WHEN NOT MATCHED THEN
    INSERT (UserId,ThreadId,ViewedCount,LastViewedTime) VALUES (@p1,@p2,@p3,@p4);
```

<a name="usage-returning"></a>
### 返回写入值

删除、新增、更新、增改选项可以向数据库提供程序请求返回值。它适合获取被删除的文件编号、自增值、生成值、更新后的计数值等。该能力只有在当前驱动支持返回值时才可用。

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

对于简单计数器，可直接使用 `Increase` 和 `Decrease` 辅助方法：

```csharp
var totalViews = this.DataAccess.Increase<Thread>(
    nameof(Thread.TotalViews),
    Condition.Equal(nameof(Thread.ThreadId), threadId));
```

删除选项返回的是删除前的值：

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
### 选项、事件与事务

每类数据访问操作都有对应的选项对象。选项对象本身很小，但很重要，因为验证器、过滤器、事件以及服务钩子都会拿到同一个选项实例。

常用选项成员如下：

| 选项成员 | 适用操作 | 说明 |
| --- | --- | --- |
| `Parameter(...)` / `new DataXxxOptions(parameters)` | 所有选项类型 | 添加本次操作的附加参数。这些参数用于验证器、过滤器、回调和服务钩子，不是 SQL 参数；映射命令的 SQL 或存储过程参数使用 `IEnumerable<Parameter>` 传入。 |
| `SuppressValidator()` | Select、Exists、Aggregate、Delete、Insert、Update、Upsert | 跳过本次操作注册的数据验证器。通常只用于调用方已经完成约束检查，或内部维护操作需要绕开常规验证规则的场景。 |
| `Return(...)` | Delete、Insert、Update、Upsert | 请求返回值。新增返回新增后的值，删除返回删除前的值，更新和增改可指定返回 `ReturningKind.Newer` 或 `ReturningKind.Older`。 |
| `IgnoreConstraint()` | Insert、Import、Upsert | 在驱动支持时，跳过与数据库约束冲突的记录。 |
| `Sequence(...)` | Insert、Upsert | 控制序号字段的生成行为。 |

读取类选项：

| 选项 | 说明 |
| --- | --- |
| `DataSelectOptions.Distinct()` | 生成去重查询，常用于标量或小投影查询。 |
| `DataSelectOptions.SuppressLazy(false)` | 禁用导航子集的延迟加载；如果同时需要去重，可传入 `true`。 |
| `DataExistsOptions.SuppressValidator()` | 在不执行验证器的情况下进行存在性判断。 |
| `DataAggregateOptions.SuppressValidator()` | 在不执行验证器的情况下进行计数、求和等聚合查询。 |

写入类选项：

| 选项 | 说明 |
| --- | --- |
| `DataInsertOptions.Sequence(DataSequenceBehavior.Auto)` | 如果调用方提供了序号值则使用该值，否则由序号器生成。这是默认行为。 |
| `DataInsertOptions.Sequence(DataSequenceBehavior.Alway)` | 总是由序号器生成序号值。 |
| `DataInsertOptions.Sequence(DataSequenceBehavior.Never)` | 不生成序号值，直接使用调用方提供的值。 |
| `DataUpdateOptions.SuppressValidator(UpdateBehaviors.PrimaryKey)` | 允许更新语句包含主键字段。它只适合修复或迁移数据，不适合普通业务更新。 |
| `DataUpsertOptions.IgnoreConstraint()` | 对增改语句表达同样的忽略约束冲突意图。 |

`Parameter(...)` 常用于把临时状态传给验证器、过滤器或服务重写方法。譬如，某个同步过滤器在调用方设置标记时跳过副作用：

```csharp
var options = DataUpdateOptions
    .Parameter("SkipSynchronization")
    .SuppressValidator();

this.DataAccess.Update<Thread>(
    new { TotalViews = Operand.Field(nameof(Thread.TotalViews)) + 1 },
    Condition.Equal(nameof(Thread.ThreadId), threadId),
    options);
```

过滤器或服务钩子可从操作上下文读取该标记：

```csharp
if(context.Options.Parameters.Contains("SkipSynchronization"))
    return;
```

`Parameter(...)` 也可以携带服务钩子需要的对象：

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

对于映射命令，SQL 或存储过程参数仍然放在 `Execute` 参数中；`DataExecuteOptions` 只放操作上下文标记：

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

`Distinct()` 适合标量查询：

```csharp
var creatorIds = this.DataAccess.Select<uint>(
    nameof(Thread),
    Condition.Equal(nameof(Thread.ForumId), forumId),
    nameof(Thread.CreatorId),
    DataSelectOptions.Distinct());
```

内部操作明确需要绕开普通验证规则时，可使用 `SuppressValidator()`：

```csharp
var exists = this.DataAccess.Exists<UserProfile>(
    Condition.Equal(nameof(UserProfile.UserId), userId),
    DataExistsOptions.SuppressValidator());
```

只有在必须修复主键时，才使用 `UpdateBehaviors.PrimaryKey`：

```csharp
this.DataAccess.Update<UserProfile>(
    new { UserId = newUserId },
    Condition.Equal(nameof(UserProfile.UserId), oldUserId),
    nameof(UserProfile.UserId),
    new DataUpdateOptions(UpdateBehaviors.PrimaryKey));
```

每类操作也都有前后回调和对应的 `IDataAccess` 事件，譬如 `Selecting`/`Selected`、`Inserting`/`Inserted`、`Executing`/`Executed`。注册到 `IDataAccess.Filters` 的过滤器会在前置事件之后、数据提供程序执行之前运行，适合承载通用横切逻辑。

当多个操作必须共享同一个环境数据会话，并一起提交或回滚时，可使用 `Transaction`。

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
### 其他

更多内容，譬如读写分离、继承表、数据模式、映射文件、过滤器、验证器、类型转换、数据隔离等，请查阅相关文档。

如果这个项目对你有帮助，欢迎 **Watch**、**Fork** 或 **Star**。

<a name="performance"></a>
## 性能

Zongsoft.Data 追求的是性能、可维护性和易用性的平衡，而不是为了某个单项基准测试牺牲设计目标。对于 ORM 数据访问引擎来说，性能主要取决于：

1. 生成简洁高效的 SQL，并在合适时使用特定数据库的语法；
2. 高效地将查询结果组装(**P**opulate)为实体对象；
3. 在热点路径避免反射，并缓存已解析的表达式树。

由于数据结构关系是声明式表达的，引擎可以把调用意图转换为表达式树，再生成不同数据提供程序的 SQL。应用代码更聚焦，数据提供程序也有更多优化空间。

实现层面使用 **E**mitting 和动态编译技术，提前准备实体组装(**P**opulate)、参数绑定等路径。可通过 [ModelEmitter](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Data/src/Common/ModelMemberEmitter.cs) 等相关类了解细节。
