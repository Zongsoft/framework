---
name: zongsoft-data
description: 开发、重构、测试或审查 Zongsoft.Data ORM 数据引擎及其数据库驱动，并创建、编辑或校验 `.mapping` XML 数据映射文件。当 Codex 需要处理驱动扩展点、语句构建/绑定/插槽、数据连接与连接故障保护、数据导入、驱动单元测试及 Podman 数据库测试环境，或依据 `Zongsoft.Data.xsd` 建模实体和命令时使用。
---

# Zongsoft.Data 数据引擎

## 基本原则

- 先判断问题属于数据引擎通用能力还是特定数据库行为。不要为了单一驱动的小需求轻易扩充 `IDataDriver` 等通用接口。
- 优先通过已有的专用扩展点解决驱动差异；只有多个驱动共享相同语义时才考虑提升到公共抽象。
- 修改文本文件时保持 CRLF 换行，修改代码和 XML 时使用 Tab 缩进。
- 保留工作区中与当前任务无关的用户修改，不要重置或覆盖脏工作树。

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
- 复合属性的 `immutable` 默认为 `true`。当集合或导航属性需要可变时设置 `immutable="false"`。
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
- `mutability` 默认为 `none`；只有命令会改变数据且调用方需要该元数据时，才设置为 `insert`、`update`、`delete` 或 `upsert`。
- 添加 `parameter` 子节点，必填 `name` 和 `type`；除非确实需要输出、双向或返回参数，否则使用 `direction="input"`。
- 添加一个或多个 `script` 子节点，必填 `driver`；为提升可读性并避免转义问题，将 SQL 包在 CDATA 中。

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
