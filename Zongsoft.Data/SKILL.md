---
name: zongsoft-data
description: 开发、重构、测试或审查 Zongsoft.Data ORM 数据引擎及其数据库驱动，并创建、编辑或校验 `.mapping` XML 数据映射文件。当 Codex 需要处理数据模式(Schema)的解析与表达式、驱动扩展点、语句构建/绑定/插槽、数据连接与连接故障保护、数据导入、驱动单元测试及 Podman 数据库测试环境，或依据 `Zongsoft.Data.xsd` 建模实体和命令时使用。
---

# Zongsoft.Data 数据引擎

## 基本原则

- 先判断问题属于数据引擎通用能力还是特定数据库行为。不要为了单一驱动的小需求轻易扩充 `IDataDriver` 等通用接口。
- 优先通过已有的专用扩展点解决驱动差异；只有多个驱动共享相同语义时才考虑提升到公共抽象。
- 修改文本文件时保持 CRLF 换行，修改代码和 XML 时使用 Tab 缩进。
- 保留工作区中与当前任务无关的用户修改，不要重置或覆盖脏工作树。

## 数据模式(Schema)

数据模式是数据引擎的核心 DSL：数据访问方法的 `schema` 文本参数描述要读取或写入的字段形状，引擎解析后生成 SQL。重构 Schema 子系统前先通读本节。

### 架构分层

Schema 代码分两层，改动前先确认所属层：

- **核心库**（`Zongsoft.Core/src/Data/`）——接口、模式树与通用解析器：
  - `ISchema`：解析后的模式对象（`Name`、`Text`、`ModelType`、`IsEmpty`、`IsReadOnly`，方法 `Clear/Contains/Find/Include/Exclude`）。
  - `ISchema<TMember>`：泛型成员版本，新增 `Members` 集合。
  - `Schema<TMember>`：唯一的模式树通用实现，负责深层路径查找、增删合并和空父节点裁剪。
  - `ISchemaParser` / `ISchemaParser<TEntry>`：解析器接口，`Parse(name, expression, entityType)`。
  - `SchemaParserBase<TMember>`：文本 → 成员树的状态机解析器（词法/语法分析），通过子类重写的 `Resolve(SchemaEntryToken)` 把元素名解析为成员；`Parse(expression, data, members)` 是受保护入口，`TryParse` 用于容错场景。
  - `ISchemaMember` / `SchemaMemberBase`：只读成员契约及基类；`Property` 为空时 `Ignored` 为真，表示模型投影成员不参与数据库持久化。
  - `SchemaMemberCollection<T>`：以成员名（不区分大小写）为键的集合。
- **引擎库**（`Zongsoft.Data/src/`）——实现与元数据绑定：
  - `SchemaParser`：`SchemaParserBase<SchemaMember>` 的实现；先解析实体属性元数据，显式名称未映射时查找模型上的同名公共实例属性或字段，并通过受保护的 `OnUnrecognized` 虚方法提供低频扩展。
  - `Schema`：绑定 `IDataEntity` 的 `Schema<SchemaMember>` 特化实现。
  - `SchemaMember`：映射成员持有 `Token` 和 `Ancestors`；计算成员只持有模型字段或属性，其 `Property` 为空且 `Ignored` 为真。

### 解析流程

1. `DataAccessBase` 的每个数据访问方法（`Select/Insert/Update/Upsert/Delete`）在创建上下文时调用 `this.Schema.Parse(name, schemaText, entityType)` 把文本解析为 `ISchema`；空文本默认解析为 `"*"`（`SchemaParser.Parse` 中处理）。
2. `SchemaParser.Parse` 从 `Mapping.Entities[name]` 取实体，用 `new SchemaData(entity, entityType)` 作为回调数据，调用 `SchemaParserBase.Parse`。
3. `Resolve(token)` 先按元素名在实体元数据中查找属性：
   - 父级为导航属性时切换到外部实体（`complex.Foreign`），并沿 `ForeignProperty` 链继续切换（导航跳板）。
   - 目标类型（`ModelType`）非标量时用 `entity.GetTokens(modelType)`（按模型成员裁剪）查找；标量类型时按 `entity.Properties` 查找。
   - 支持继承实体：沿 `GetBaseEntity()` 逐级向上查找，`*` 会展开所有继承层的简单属性。
   - 显式名称未找到映射时，基类调用 `OnUnrecognized`；派生解析器不处理则默认从当前模型类型查找同名公共实例属性或字段，找到后作为忽略持久化的计算成员，仍未找到才抛出 `DataArgumentException`。
   - `*` 只展开映射中的简单属性，不自动加入模型计算成员。
   - 映射成员始终优先于同名模型成员。
4. 解析结果写入 `SchemaMemberCollection<SchemaMember>`；已存在同名的成员会复用并追加子成员（`Include` 语义），排除用 `Exclude` 移除。
5. 语句构建器消费 `context.Schema.Members`：
   - `SelectStatementBuilder`：为映射简单成员生成 `SELECT` 字段，复杂成员生成 `JOIN`（`?`/`!`）或独立子查询（`*`，经 `statement.Slaves` 惰性加载，`DataSelectExecutor.PopulateSlaves` 填充）；忽略成员不生成数据库字段，计算属性所需的映射字段必须在 schema 中显式声明。
   - `InsertStatementBuilder` / `UpdateStatementBuilder` / `UpsertStatementBuilder`：按模式决定写入成员与级联子表（一对多子记录按 UPSERT 语义），跳过忽略成员。
   - `DeleteStatementBuilder`：按模式生成级联删除子表语句，跳过忽略成员。
   - 模式成员的正数 `Limit` 和 `Sortings` 应用到对应子查询；`Limit <= 0` 表示不限。

### 表达式语法要点

语法（详见 `README.zh-Hans.md` 的「数据模式」章节）：

```text
schema ::= { * | ! | !identifier | identifier [limit] [sorting] ["{" schema "}"] } [,...n]
limit ::= ":"(number | "*")
sorting ::= "(" { ["~"|"!"]identifier } [,...n] ")"
```

要点与已知行为：

- `*` 只展开简单属性（不含导航属性）；`!` 单独使用（或 `!*`）清除当前层级全部成员，`!名称` 移除指定成员。
- 限量：冒号后只接受无符号数字或 `*`；正数表示一对多成员最多加载的记录数，`0`、`*` 以及对象模型中的任何负值均表示不限。
- 排序：`~` 或 `!` 前缀表示倒序；每一项均可指定前缀，同名排序以最后一次声明的方向和位置为准。
- 空白：标识符内部不允许空白；成员之间、`*` 之后、标识符与 `{`/`(`/`:`/`,`/`}` 之间允许空白；冒号之后与排序字段内部不允许空白（排序字段之间的逗号后允许空白）。
- 标识符不能以数字开头；成员名不区分大小写。
- 解析错误统一抛出 `DataArgumentException`（消息前缀 `SyntaxError:`/`ParsingError:`）。
- 映射元数据和模型反射查找位于 `SchemaParser`；`SchemaParserBase` 只在普通解析未命中时调用与元数据无关的 `OnUnrecognized` 回调。
- 为兼容现有 HTTP Schema，连续逗号产生的空段及首尾逗号继续被容忍。

### 重构注意事项

- 保持「`SchemaParserBase`（通用文本解析）→ `SchemaParser`（元数据解析）→ 语句构建器（SQL 生成）」的分层，不要把元数据逻辑下沉到基类。
- 状态机位于 `SchemaParserBase.StateContext`（`None/Asterisk/Include/Exclude/Limit/SortingField/SortingGutter`），重构词法或语法时先跑 `Zongsoft.Core/test/Data/SchemaTest.cs` 中的回归用例。
- 已确认的 XSD 与加载器差异（重构时统一）：`complexProperty` 的 `immutable` XSD 缺省 `true`、加载器缺省 `false`；`command` 的 `mutability` XSD 缺省 `none`、加载器缺省 `Delete|Insert|Update`。
- 通配符只展开映射中的简单属性，不自动枚举模型计算成员，也不要在解析后的 Schema 中保留 `*` 来源。
- `SchemaMember.Ignored` 是唯一的持久化判别属性，由 `Property == null` 推导；不要再引入与它重叠的成员种类枚举。
- 派生解析器的 `OnUnrecognized` 只负责返回特殊绑定的模型字段或属性，不应直接生成 SQL；派生类不处理时由默认反射逻辑继续查找。
- 模式解析属于高频路径（每次数据访问都解析），缓存或编译模式时应评估 `SchemaParserBase` 的状态机分配。

## 驱动与语句扩展点

`IDataDriver` 当前公开 `Binder`、`Builder` 和 `Slotter`：

- 使用 `IStatementBuilder` 构建数据库方言语句。
- 使用 `IStatementBinder` 在命令参数创建完成后绑定数据。`StatementExtension.Bind(...)` 根据当前数据访问上下文取得 `context.Source.Driver.Binder`，没有驱动绑定器时回退到 `StatementBinder.Default`。
- 需要在通用绑定完成后整理命令时，继承 `StatementBinder` 并重写绑定完成回调；不要把单一驱动的命令整理需求加入 `IDataDriver`。
- TDengine DELETE 会由表达式访问器把条件参数安全内联到 SQL，但参数对象仍会由通用命令创建过程生成。`TDengineStatementBinder` 必须等参数绑定完成后再清除这些已不被 SQL 引用的参数；不要在命令创建或绑定之前清空参数。
- 保持 `IStatementSlotEvaluator` 只负责评估单个 `StatementSlot`。`StatementSlotter` 是静态协调类，它通过 `context.Source.Driver.Slotter` 获取当前评估器并替换命令文本中的插槽。

在 `DataDriverBase` 中通过 `CreateBinder()`、`CreateSlotter()`、`CreateVisitor()` 和 `CreateImporter()` 提供驱动实现。除非需求确实具有通用语义，否则优先重写这些工厂或对应实现类。

## 数据连接与故障保护

连接路径统一为：

```text
DataSession
  → DataConnectorManager.GetConnector(IDataSource)
  → DataConnector
  → DbConnection.Open/OpenAsync
```

- `DataSession.Connector` 是驱动层建立独立连接的公共入口。普通命令和数据导入都应通过该 Connector 打开连接，不要直接重复实现连接保护。
- `DataConnectorManager` 是公共静态管理器，使用 `ConditionalWeakTable<IDataSource, DataConnector>` 为每个数据源共享 Connector。
- `DataConnector` 是公共类，但熔断器、状态、选项和状态变更事件均为内部实现，不要通过 `IDataDriver`、`DataSession` 或 Feature 暴露熔断器。
- Connector 使用信号量串行化同一数据源的物理连接建立。首次失败会打开内部熔断器，等待者及后续请求在恢复时间到达前快速抛出 `DataConnectionException`，避免连接池和线程池被失败连接风暴耗尽。
- `DataConnector.Failed` 只在真实物理连接失败时通知业务层，熔断期间的快速拒绝不会重复触发，避免形成通知或日志风暴。事件参数 `DataConnectionFailureEventArgs` 提供数据源、连续失败次数、下次重试时间和 `ExceptionHandled`；业务订阅者可将 `ExceptionHandled` 设为 `true` 以接管默认处理。
- 未被业务层处理的连接失败默认调用 `Zongsoft.Diagnostics.Logging.GetLogging<DataConnector>().Error(...)` 写入错误日志。日志消息从资源文件读取，并可包含数据源名、驱动名、服务器地址、数据库名、用户名、连续失败次数和重试时间；不要记录完整连接字符串、密码或其他凭据。
- `DataConnectionException` 提供 `SourceName`、`DriverName`、`RetryAt` 和 `RetryAfter`。首次物理连接失败仍抛出提供程序原始异常；熔断后的快速拒绝才抛出该异常。
- 熔断配置从数据源 `Properties` 读取：`CircuitBreaker.FailureThreshold`、`CircuitBreaker.Duration`、`CircuitBreaker.MaximumDuration` 和 `CircuitBreaker.Jitter`。
- 类型化 `ConnectionSettingsBase<TDriver, TOptions>` 遇到未定义连接字符串项时，通过 `OnUnrecognized` 将其放入 `Properties`；已定义的复合属性仍保留在内部设置项中。因此熔断配置应直接写入连接字符串，并验证它们出现在 `settings.Properties`。
- SQLite 和 DuckDB 当前也经过相同 Connector、信号量和熔断路径，并没有自动跳过。现有基准显示：DuckDB 没有可见退化；SQLite 默认池化连接在 16 路并发打开时每连接增加约 3 微秒，顺序打开没有稳定差异。该绝对成本不足以支持增加禁用机制，因此保持统一保护；Connector 实现或典型负载发生明显变化后再重新测量。不要在核心中硬编码驱动名称。

驱动导入器需要独立连接时，使用：

```csharp
using var connection = context.Session.Connector.Connect();
await using var connection = await context.Session.Connector.ConnectAsync(cancellation);
```

对于提供原生连接函数的驱动，使用 Connector 对应的泛型同步或异步重载。

## 驱动测试

先在驱动目录中查找测试项目及容器声明：

```powershell
rg --files Zongsoft.Data/drivers | rg -- "-pod\.yaml$|Tests\.csproj$"
```

- SQLite 和 DuckDB 是进程内数据库，运行其单元测试不需要启动数据库服务器容器。
- 其他驱动的测试目录中如果存在 `*-pod.yaml`，表示测试依赖由 Podman pod 托管的数据库服务器。运行测试前先检查对应 pod/容器是否已经存在并处于运行状态；如果尚未启动，则根据该 YAML 启动 pod，并等待数据库服务真正就绪后再运行测试。Pod 处于 Running 不一定表示数据库已经接受连接。
- 当前仓库中的 MSSQL、MySQL、PostgreSQL 和 TDengine 测试目录包含 `*-pod.yaml`；仍应每次动态扫描，不要把该列表视为永久不变。
- 使用本机 Pod 容器的数据库进行测试，可以在源代码文件中的连接字符串中包含用户名和密码，测试输出摘要或提交记录。
- 先运行与改动直接相关的测试，再运行对应驱动项目的完整测试。使用 `--blame-hang` 和合理的 `--blame-hang-timeout` 防止连接失败测试无限挂起。
- 多目标框架项目按 `net8.0`、`net9.0`、`net10.0` 分别还原和测试。中央包版本按目标框架定义；发生 NU1605 时先检查 `Directory.Packages.props` 与数据库提供程序的传递依赖约束。
- MSSQL 测试项目在 net9.0 下不要直接固定 `Microsoft.Extensions.Caching.Memory` 9.0.2，因为 `Microsoft.Data.SqlClient 7.0.2` 要求至少 9.0.13；net8.0 和 net10.0 仍需要各自匹配的直接运行时依赖。

连接故障韧性测试至少验证：

1. 使用不可达端点和较小连接池触发一次真实物理连接失败。
2. 首次失败保留提供程序原始异常，下一次请求得到 `DataConnectionException`。
3. 并发执行查询、新增和数据导入，并在明确时限内全部快速拒绝。
4. 并发完成后运行线程池哨兵任务，确认进程仍能调度工作。
5. 将 `CircuitBreaker.Duration` 等设置写入连接字符串，并断言未知设置已进入 `settings.Properties`。

## 数据映射

### 工作流程

以 `Zongsoft.Data.xsd` 作为 schema 权威。项目中的 `.mapping` 文件只用于推断命名和建模约定；不要复制不符合 schema 的历史误写属性。

1. 找到最近的 `Zongsoft.Data.xsd` 和相关的同级 `.mapping` 文件。
2. 根据项目或模块确定容器命名空间。使用 `container name="Security"`、`name="Things"` 等；只有在既有项目已经这样做时才使用空名称。
3. 先建模实体：主键、标量属性，然后是复合/导航属性。
4. 只有普通实体 CRUD 无法表达的自定义 SQL 或存储过程操作才添加命令。
5. 在可用时使用 XML 校验器按 XSD 校验结构；否则也要按 XSD 规则进行人工核对。

### 文件结构

从以下带命名空间的结构开始：

```xml
<?xml version="1.0" encoding="utf-8" ?>

<schema xmlns="http://schemas.zongsoft.com/data">
	<container name="ModuleName">
		<entity name="EntityName" table="Module_TableName">
			<key>
				<member name="EntityId" />
			</key>

			<property name="EntityId" type="uint" nullable="false" sequence="#" />
			<property name="Name" type="varchar" length="100" nullable="false" />
			<property name="Creation" type="datetime" nullable="false" default="now()" />
		</entity>
	</container>
</schema>
```

顶层允许的内容是 `schema/container/entity` 和 `schema/container/command`。同一容器内的实体名和命令名必须唯一。

### 实体

为每个数据聚合或表模型使用一个 `entity`。

- `name` 必填，是代码中使用的实体名。
- `table` 可选；当数据库表名与实体名一致时省略。对于 `Security_Role` 这类带前缀的表使用它。
- `driver` 可选；只用于驱动特定实体，例如 `driver="MySQL"`、`driver="TDengine"`。
- `immutable="true"` 表示实体支持读取和新增，但不支持更新和删除。
- `inherits` 由 XSD 支持，用于指定父实体名；如果父实体在其他容器或命名空间中，使用完整限定名。

在属性之前定义 `<key>`，该元素表示对应实体的主键；主键成员字段通过 `<member>` 来指向对应的字段名。

```xml
<key>
	<member name="TenantId" />
	<member name="BranchId" />
	<member name="Key" />
</key>
```

### 标量属性

使用 `property` 定义标量列。必填属性是 `name` 和 `type`；项目文件中几乎总是显式指定 `nullable`，所以要有意识地设置它。

- 使用常见字段类型：`uint`、`ulong`、`ushort`、`byte`、`short`、`int`、`bool`、`varchar`、`nvarchar`、`string`、`datetime`、`timestamp`、`date`、`decimal`、`double`、`binary`。
- 字符串或二进制字段使用 `length`。`decimal`、`money`、`currency` 使用 `precision` 和 `scale`。
- 只有数据库列名与属性名不同时才使用 `field`。
- 使用 `default` 表示默认值，例如 `true`、`false`、`_`、`0`、`now()`、`today()`。
- 对只在新增时设置的值使用 `immutable="true"`，例如所有者 ID 和创建元数据。
- 只有字段确实要参与排序或查询排序时才使用 `sortable="true"`。
- `hint` 只用于驱动特定行为，例如时序映射使用 `hint="tag"` 和 `hint="timestamp"`。

序号约定：

- `sequence="*"` 使用数据库内置序号器。
- `sequence="#"` 为该属性使用外部序号器。
- `sequence="#(TenantId,BranchId)"` 根据列出的属性创建有作用域的子级序号。
- `sequence="Security.Role:RoleId"` 引用另一个实体/属性的序号。

```xml
<property name="OrderId" type="ulong" nullable="false" sequence="#" />
<property name="AttachmentId" type="ulong" nullable="false" sequence="#(OrderId)" />
<property name="TenantId" type="uint" nullable="false" immutable="true" sortable="true" />
<property name="Amount" type="decimal" precision="18" scale="2" nullable="false" default="0" />
```

### 复合属性

使用 `complexProperty` 定义导航属性。必填属性是 `name` 和 `port`。

- `port` 通常命名目标实体。
- `port="JoinEntity:TargetNavigation"` 表示通过中间实体路由到该实体上的导航/属性。
- `multiplicity` 默认为 `?`（零或一）。使用 `!` 表示恰好一个，使用 `*` 表示集合。
- 复合属性 `immutable` 的默认值存在已知不一致：`Zongsoft.Data.xsd` 声明的缺省值为 `true`，但映射加载器 `MetadataFileResolver` 实际按 `false`（可写）处理。因此**不要依赖缺省值**，需要可写集合时显式写 `immutable="false"`，只读关联显式写 `immutable="true"`；重构时应统一 XSD 与加载器。
- `behaviors="principal"` 标记目标为主控端；明细或子类型记录指向其所有者时常见。

链接把目标端口连接到当前实体锚点：

- `<link port="TenantId" />` 表示目标端口和当前锚点使用同名属性。
- `<link port="MemberId" anchor="UserId" />` 表示把目标 `MemberId` 映射到当前 `UserId`。
- 组合关系使用多个 `link`。

```xml
<complexProperty name="Branch" port="Branch">
	<link port="TenantId" />
	<link port="BranchId" />
</complexProperty>

<complexProperty name="Roles" port="Member:Role" multiplicity="*">
	<link port="MemberId" anchor="UserId" />

	<constraints>
		<constraint name="MemberType" value="0" />
	</constraints>
</complexProperty>
```

使用 `constraints` 为关系添加常量过滤条件。`actor` 可选；出现时，使用 `Principal` 表示当前/主控端，使用 `Foreign` 表示外链/目标端。

```xml
<constraints>
	<constraint name="Target" actor="Foreign" value="Device" />
</constraints>
```

### 命令

使用 `command` 定义命名 SQL 或存储过程操作。命令应放在其操作实体所在的同一容器内。

- `name` 必填。
- `alias` 可用于指定存储过程、函数或视图名称。
- `type` 可以是 `text` 或 `procedure`；样例项目中省略的命令都是 SQL 文本。
- `mutability` 声明命令对数据的变更性：`none` 表示只读命令（会路由到只读数据源），`insert`、`update`、`delete`、`upsert` 表示写命令。注意：XSD 声明的缺省值为 `none`，但映射加载器 `MetadataFileResolver` 对未声明 `mutability` 的命令实际按 `Delete|Insert|Update`（可写）处理，因此**显式声明 `mutability`**，只读命令务必写 `mutability="none"`。加载器对 `type`/`mutability` 的解析不区分大小写，但为通过 XSD 校验应统一使用小写。
- 添加 `parameter` 子节点，必填 `name` 和 `type`；`direction` 使用 `input`（默认）、`output`、`both`、`return`（加载器也接受 `in`、`out`、`result` 等简写，但 XSD 校验只认标准写法）。
- 添加一个或多个 `script` 子节点，必填 `driver`；为提升可读性并避免转义问题，将 SQL 包在 CDATA 中。也可以不写 `script`，而在映射文件同目录提供名为 `{命令名}-{驱动名}.sql` 的脚本文件（加载器自动装载）。

```xml
<command name="SetDeviceMetricMappedCode" mutability="update">
	<parameter name="Prefix" type="string" direction="input" />
	<parameter name="DeviceId" type="uint" direction="input" />
	<script driver="MySql">
		<![CDATA[
		UPDATE Things_Metric m
		INNER JOIN Things_DeviceMetric dm ON dm.MetricId = m.MetricId
		SET m.MappedCode = CONCAT(@Prefix, m.MetricCode)
		WHERE dm.DeviceId = @DeviceId;
		]]>
	</script>
</command>
```

### 审查清单

完成 `.mapping` 修改前：

- 保持 XML 命名空间精确为 `http://schemas.zongsoft.com/data`。
- 编辑项目映射文件或 XML 示例时使用 Tab 缩进。
- 确认每个主键成员都有对应的标量属性。
- 对主键、必填字段和使用序号的属性设置 `nullable="false"`。
- 文本字段添加 `length`，小数字段添加 `precision` 和 `scale`。
- 优先沿用周边模块已有的 `varchar`/`nvarchar` 约定。
- 确保每个 `complexProperty` 至少有一个 `link`，并且 anchor/port 都指向真实属性。
- 集合导航使用 `multiplicity="*"`；只有必需的单值导航才使用 `!`。
- 驱动名称与附近文件保持一致（历史上同时出现过 `MySql`、`MySQL` 等，按本地项目写法匹配）。
- 不要引入 `Zongsoft.Data.xsd` 之外的属性，即使附近旧文件中出现过。
