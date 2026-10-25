---
name: zongsoft-data
description: 开发、重构、测试或审查 Zongsoft.Data ORM 数据引擎及其数据库驱动，并创建、编辑或校验 `.mapping` XML 数据映射文件。当 Codex 需要处理数据模式(Schema)的解析与表达式、驱动扩展点、语句构建/绑定/插槽、数据连接与连接故障保护、数据导入、驱动单元测试及 Podman 数据库测试环境，或依据 `Zongsoft.Data.xsd` 建模实体和命令时使用。
---

# Zongsoft.Data 数据引擎

开始工作前先阅读 [AGENTS.md](AGENTS.md)；涉及数据库驱动时再阅读 [drivers/AGENTS.md](drivers/AGENTS.md)。本技能保存数据引擎中需要跨文件理解的契约和已确认实现事实，README 负责用户用法。

## 基本原则

- 先判断问题属于数据引擎通用能力还是特定数据库行为。不要为了单一驱动的小需求轻易扩充 `IDataDriver` 等通用接口。
- 优先通过已有的专用扩展点解决驱动差异；只有多个驱动共享相同语义时才考虑提升到公共抽象。
- 修改文本文件时保持 CRLF 换行，修改代码和 XML 时使用 Tab 缩进。
- 保留工作区中与当前任务无关的用户修改，不要重置或覆盖脏工作树。

## 插件、容器与首次数据访问

- [DataAccessProvider](src/DataAccessProvider.cs) 通过 ServiceAttribute 的 Members 注册静态 Instance；其基类 `DataAccessProviderBase<TDataAccess>` 还声明了 `IDataAccessProvider` 与 `IServiceProvider<IDataAccess>` 契约，服务扫描会读取继承的特性，因此不能仅看派生类特性推断注册契约。创建具名访问器后优先使用同名模块容器注入，缺少模块时回到应用容器。
- [MetadataFileLoader](src/Metadata/Profiles/MetadataFileLoader.cs) 默认递归搜索 ApplicationPath 的 `.mapping`；别把部署了映射等同于创建物理表。命名命令由 container 与 name 组成限定名，mutability 控制读写源选择。
- 驱动清单分别注册数据驱动与连接设置驱动；两者之一缺失都会影响首次查询。README 的[真实插件用例](README.zh-Hans.md#plugin-quickstart)以 Discussions 模块说明公共接口与数据服务的消费方式，不代表全新部署已经通过集成验证。
- 手工调试部署要核对最终 SDK/原生依赖。Windows x64 SQLite 在插件内仅保留嵌套 runtimes 时可能找不到 e_sqlite3，隔离验证采用匹配架构原生 DLL 与托管包装器同目录。不要把构建输出复制成功作为原生库可加载的证明。

## 数据模式(Schema)

数据模式是数据引擎的核心 DSL：数据访问方法的 `schema` 文本参数描述要读取或写入的字段形状，引擎解析后生成 SQL。重构 Schema 子系统前先通读本节。

### 架构分层

Schema 代码分两层，改动前先确认所属层：

- **核心库**（`Zongsoft.Core/src/Data/`）——接口、模式树与通用解析器：
  - `ISchema`：解析后的模式对象（`Name`、`Text`、`ModelType`、`IsEmpty`、`IsReadOnly`，方法 `Clear/Contains/Find/Include/Exclude`）。
  - `ISchema<TMember>`：泛型成员版本，新增 `Members` 集合。
  - `Schema<TMember>`：唯一的模式树通用实现，负责深层路径查找、增删合并和空父节点裁剪。
  - `ISchemaParser` / `ISchemaParser<TEntry>`：解析器接口，`Parse(name, expression, modelType)`。
  - `SchemaParserBase<TMember>`：文本 → 成员树的状态机解析器（词法/语法分析），通过子类重写的 `Resolve(SchemaEntryToken)` 把元素名解析为成员；`ShouldExpand(TMember)` 决定终止成员是否自动展开默认子成员；`Parse(expression, data, members)` 是受保护入口，`TryParse` 用于容错场景。
  - `ISchemaMember` / `SchemaMemberBase`：只读成员契约及基类；`Property` 为空时 `Ignored` 为真，表示模型投影成员不参与数据库持久化。
  - `SchemaMemberCollection<T>`：以成员名（不区分大小写）为键的集合。
- **引擎库**（`Zongsoft.Data/src/`）——实现与元数据绑定：
  - `SchemaParser`：`SchemaParserBase<SchemaMember>` 的实现；先解析实体属性元数据，显式名称未映射时查找模型上的同名公共实例属性或字段，并通过受保护的 `OnUnrecognized` 虚方法提供低频扩展；仅对映射导航属性重写 `ShouldExpand` 返回真。
  - `Schema`：绑定 `IDataEntity` 的 `Schema<SchemaMember>` 特化实现。
  - `SchemaMember`：映射成员持有 `Token` 和 `Ancestors`；计算成员只持有模型字段或属性，其 `Property` 为空且 `Ignored` 为真。

### 解析流程

1. `DataAccessBase` 的每个数据访问方法（`SelectAsync/InsertAsync/UpdateAsync/UpsertAsync/DeleteAsync`）在创建上下文时调用 `this.Schema.Parse(name, schemaText, modelType)` 把文本解析为 `ISchema`；空文本默认解析为 `"*"`（`SchemaParser.Parse` 中处理）。
2. `SchemaParser.Parse` 从 `Mapping.Entities[name]` 取实体，用 `new SchemaData(entity, modelType)` 作为回调数据，调用 `SchemaParserBase.Parse`。
3. `Resolve(token)` 先按元素名在实体元数据中查找属性：
   - 父级为导航属性时切换到外部实体（`complex.Foreign`），并沿 `ForeignProperty` 链继续切换（导航跳板）。
   - 目标类型（`ModelType`）非标量时用 `entity.GetTokens(modelType)`（按模型成员裁剪）查找；标量类型时按 `entity.Properties` 查找。
   - 支持继承实体：沿 `GetBaseEntity()` 逐级向上查找，`*` 会展开所有继承层的简单属性。
   - 显式名称未找到映射时，基类调用 `OnUnrecognized`；派生解析器不处理则默认从当前模型类型查找同名公共实例属性或字段，找到后作为忽略持久化的计算成员，仍未找到才抛出 `DataArgumentException`。
   - `*` 只展开映射中的简单属性，不自动加入模型计算成员。
   - 映射成员始终优先于同名模型成员。
4. 解析结果写入 `SchemaMemberCollection<SchemaMember>`；已存在同名的成员会复用并追加子成员（`Include` 语义），排除用 `Exclude` 移除。
5. 语句构建器消费 `context.Schema.Members`：
   - `SelectStatementBuilder`：为映射简单成员生成 `SELECT` 字段，复杂成员生成 `JOIN`（`?`/`!`）或独立子查询（`*`，经 `statement.Slaves` 惰性加载，`DataSelectExecutor.PopulateSlavesAsync` 填充）；忽略成员不生成数据库字段，计算属性所需的映射字段必须在 schema 中显式声明。
   - 普通查询的一对一导航在投影中补齐外表主键，用于区分空关联与其他所选字段均为 null 的实体；补键只修改语句，不修改可复用的 Schema。分组查询仅投影分组键、聚合项和显式导航字段，不自动补导航主键，也不扩充分组键。导航对象根据所投影字段的非空值组装；调用方需要在聚合结果中区分存在但业务字段均为 null 的关联时，应显式选择可由分组确定的关联键。独立的一对多子查询仍遵守普通查询的补键规则。
   - `InsertStatementBuilder` / `UpdateStatementBuilder` / `UpsertStatementBuilder`：按模式决定写入成员与级联子表（一对多子记录按 UPSERT 语义），跳过忽略成员。
   - `DeleteStatementBuilder`：按模式生成级联删除子表语句，跳过忽略成员。
   - 模式成员的正数 `Limit` 和 `Sortings` 应用到对应子查询；`Limit <= 0` 表示不限。

### 表达式语法要点

语法（详见 `README.zh-Hans.md` 的「数据模式」章节）：

```text
schema ::= { * | ! | exclusion | inclusion } [,...n]
path ::= identifier {"." identifier}
exclusion ::= "!" path
inclusion ::= path [limit] [sorting] ["{" schema "}"] | path ".*"
limit ::= ":"(number | "*")
sorting ::= "(" { ["~"|"-"|"+"]identifier } [,...n] ")"
```

要点与已知行为：

- `*` 只展开简单属性（不含导航属性）；`!` 单独使用时清除当前层级全部成员，`!名称` 或 `!导航.成员` 移除指定成员。连续的 `!*` 以及 `!路径.*` 均为非法语法。
- 点路径与花括号生成同一成员树并可混用，例如 `Department.Manager{Name}`；句点两侧允许空白，首尾点、连续点、`Path.*.Name` 和 `Path.!Name` 非法。斜杠只用于 `ISchema` 对象的路径 API，不属于文本 DSL。
- 映射导航属性作为终止成员时自动展开其简单属性，因此 `Department`、`Department.*`、`Department{*}` 等价；解析结果不保留点路径、花括号或通配符来源。
- `!Navigation` 与 `Navigation{!}` 都移除整个导航；命名排除会逐段解析并验证，未知的根段、中间段或末段均抛出 `DataArgumentException`，合法但尚未包含的路径不修改成员树。
- 限量：冒号后只接受无符号数字或 `*`；正数表示一对多成员最多加载的记录数，`0`、`*` 以及对象模型中的任何负值均表示不限。
- 排序：`~` 或 `-` 前缀表示倒序，`+` 或无前缀表示正序；`+Name` 与 `Name` 完全等价。每一项均可指定前缀，同名排序以最后一次声明的方向和位置为准；规范化输出使用 `~` 表示倒序并省略正序的 `+`。
- 限量或排序会终止点路径；终止导航仍自动展开简单属性，但修饰符之后继续写句点属于非法语法。
- 空白：标识符内部不允许空白；成员之间、`*` 之后、标识符与 `{`/`(`/`:`/`,`/`}` 之间允许空白；冒号之后与排序字段内部不允许空白（排序字段之间的逗号后允许空白）。
- 标识符不能以数字开头；成员名不区分大小写。
- 解析错误统一抛出 `DataArgumentException`（消息前缀 `SyntaxError:`/`ParsingError:`）。
- 映射元数据和模型反射查找位于 `SchemaParser`；`SchemaParserBase` 只在普通解析未命中时调用与元数据无关的 `OnUnrecognized` 回调。
- 为兼容现有 HTTP Schema，连续逗号产生的空段及首尾逗号继续被容忍。

### 重构注意事项

- 保持「`SchemaParserBase`（通用文本解析）→ `SchemaParser`（元数据解析）→ 语句构建器（SQL 生成）」的分层，不要把元数据逻辑下沉到基类。
- 状态机位于 `SchemaParserBase.StateContext`（包括普通成员、点路径、排除、限量与排序状态），重构词法或语法时先跑 `Zongsoft.Core/test/unit/Data/SchemaTest.cs` 中的回归用例。
- 已确认的 XSD 与加载器差异（重构时统一）：`complexProperty` 的 `immutable` XSD 缺省 `true`、加载器缺省 `false`；`command` 的 `mutability` XSD 缺省 `none`、加载器缺省 `Delete|Insert|Update`。
- 通配符只展开映射中的简单属性，不自动枚举模型计算成员，也不要在解析后的 Schema 中保留 `*` 来源。
- `Schema.Text` 保留原始表达式，`Schema.ToString()` 输出当前成员树的规范化花括号文本；空的映射导航输出 `{}` 以避免再次解析时被当作裸导航自动展开。
- `SchemaMember.Ignored` 是唯一的持久化判别属性，由 `Property == null` 推导；不要再引入与它重叠的成员种类枚举。
- 派生解析器的 `OnUnrecognized` 只负责返回特殊绑定的模型字段或属性，不应直接生成 SQL；派生类不处理时由默认反射逻辑继续查找。
- 模式解析属于高频路径（每次数据访问都解析），缓存或编译模式时应评估 `SchemaParserBase` 的状态机分配。

## 驱动与语句扩展点

`IDataDriver` 当前公开 `Binder`、`Builder` 和 `Slotter`：

- 使用 `IStatementBuilder.BuildAsync` 按序准备数据库方言语句，在原字段/条件验证点等待，保持前后局部操作次序。CreateStatement、表达式组装与 SQL 文本输出是纯本地同步操作；不要提前枚举全部字段进行验证。
- `IBatchStatement` 表示写入语句的分批能力，声明按行排列的字段和值，以及 `BatchSize`、`ParameterLimit` 约束。`InsertStatement` 和 `UpsertStatement` 实现此接口，各方言的新增、增改构建器通过 `CreateStatement` 设置约束。执行器和按行参数绑定仅依赖这一能力接口；驱动和通用语句构建器不承担分批策略。级联增改复用当前增改构建器；更新构建器通过 `CreateUpsertStatementBuilder` 提供对应方言的级联增改构建器。
- 使用 `IStatementBinder` 在命令参数创建完成后绑定数据。`StatementExtension.BindAsync(...)` 根据当前数据访问上下文取得 `context.Source.Driver.Binder`，没有驱动绑定器时回退到 `StatementBinder.Default`。
- 需要在通用绑定完成后整理命令时，继承 `StatementBinder` 并重写绑定完成回调；不要把单一驱动的命令整理需求加入 `IDataDriver`。
- TDengine DELETE 会由表达式访问器把条件参数安全内联到 SQL，但参数对象仍会由通用命令创建过程生成。`TDengineStatementBinder` 必须等参数绑定完成后再清除这些已不被 SQL 引用的参数；不要在命令创建或绑定之前清空参数。
- 保持 `IStatementSlotEvaluator` 只负责评估单个 `StatementSlot`。`StatementSlotter` 是静态协调类，它通过 `context.Source.Driver.Slotter` 获取当前评估器并替换命令文本中的插槽。

在 `DataDriverBase` 中通过 `CreateBinder()`、`CreateSlotter()`、`CreateVisitor()` 和 `CreateImporter()` 提供驱动实现。除非需求确实具有通用语义，否则优先重写这些工厂或对应实现类。

## 验证、绑定与导入字段

IStatementBinder.BindAsync 等待序列取值、IDataValueBinder.BindAsync 和 OnBoundAsync；字段替代结果通过 DataValidatorResult.HasValue 判断，Value 为 null 仍是有效替代值。参数写回的时机、字段顺序和默认值处理保持由原语句及 Schema 决定。

DataImporterBase.Member.GetValueAsync 返回 MemberValue（Target、Value）：Target 是序列或验证器回写后的当前行对象，Value 是按数据库字段类型、长度及可空规则转换后的值。驱动须在每次等待后先更新当前行 target，再使用 Value，才能保留结构体重新装箱的结果；不扩大为改写调用方原集合。序列命中时保留原先直接返回序列值、不再执行后续验证的顺序。纯反射读取/写入保持同步。

## 异步执行与结果所有权

DataProvider 的 Executing、Executed、Error 均为可等待委托，按注册顺序逐项等待。Executing 保持在执行 try 外；分批执行结束时还原输入；成功时提交会话后通知 Executed。失败先回滚，再等待 IDataDriver.OnErrorAsync 与 Error；错误处理使用 CancellationToken.None，被处理的异常不触发 Executed。保留原异常转换、回滚故障与通知故障的传播边界。

DataAccess 的数据库执行重写、事件和过滤器只保留异步入口。序列键的实体/字段/依赖文本由 ParseSequenceKey 本地解析，缺少依赖值时等待字段验证器；无上下文不能调用验证器。IDataSequencer 仅异步；内部 ISequenceBase 的强制同步成员抛 NotSupportedException。普通字典和反射对象的依赖值均参与键生成，@ 后内容仅作为依赖值。

DataProvider 对 IEnumerable 和 IAsyncEnumerable 批量新增/增改输入按最多 256 行准备一个类型化批次，逐批执行语句，共用同一上下文、会话和事务，全部完成后才提交。输入仅枚举一次，支持值类型装箱，批次等待传递取消令牌；分批流程结束或失败均还原上下文的原始输入。输入分批是 DataProvider 的私有执行逻辑；Utility.EnumerateAsync 统一枚举输入，值类型异步源通过 Core 的 Cast 适配，写入与 DataImporterBase.GetRecordsAsync 共用这一入口。

DataMutateExecutor 根据实际语句的字段值和参数集合进一步分批，包括级联语句。StatementBatch 保留每行对应的值表达式、参数名和语句公共参数，逐批绑定对应的数据项，完成或异常退出后还原语句集合。序号回填和 RETURNING 只针对当前执行批次，总影响行数累加；从属语句在父语句各批次完成后继续执行。参数预算扣除公共参数，再按单行最大参数数计算；单行超过预算时拒绝执行。

SQL Server 的参数预算为 2098，预留 SqlClient 调用 sp_executesql 的两个参数；MySQL、PostgreSQL 为 65535，SQLite 为 32766。TDengine 每条语句处理一行，以当前行标签计算子表；重复输出同一语句时复用 Subtable 插槽。普通关系型插入使用多行 VALUES，忽略约束和增改仍使用各驱动的既有方言。DataImporterBase 的具体导入器负责其原生批量接口的有界缓存及事务限制。

查询和命名命令的结果只提供 IAsyncEnumerable。每次枚举创建独立 DbCommand，未枚举不创建命令或读取器；每次读取前主动检查取消令牌，不能依赖提供程序的 ReadAsync 在已有缓冲行时响应取消；失败、提前结束和自然结束均等待读取器、连接租约与命令释放。关联查询保存父记录的参数快照；IEnumerable/集合/数组导航在父记录输出前异步物化，IAsyncEnumerable 导航保持异步消费。字段读取与模型反射只处理已读取的本地值。命名命令的输出参数在读取器关闭后回写原 Parameter 对象。

## 驱动异步边界

### 写入返回值与关联更新

DataProvider 的私有嵌套 DataExecutor 通过当前上下文的 `Source.Driver.Executor` 转发执行，每次根据实际数据源选择驱动；`DataProvider.Executor` 保留全局替换或装饰入口。`IDataDriver.Executor` 由 DataDriverBase 的 `CreateExecutor()` 创建，默认使用公共 DataExecutor.Instance。公共 DataExecutor 负责语句分派、通用命令执行与从属语句遍历，各类专用执行器通过受保护工厂延迟创建并安全共享。专用执行器返回的布尔值表示是否继续遍历从属语句；false 表示已处理或无需处理，不触发通用执行器回退。执行器不得保存单次操作的上下文、会话或语句状态。

`Features` 目录及命名空间承载特性接口和实现。同一特性的接口与通用实现放在以实现类命名的同一个文件中，接口在前、实现在后；只有接口的特性保留独立文件。FeatureCollection 的 Get/Set 按精确契约类型管理共享实例，本地未注册时继承父集合，Set(null) 移除本地覆盖；集合只在初始化时配置，不负责释放实例。Count 和枚举仅包含本地注册，枚举项为契约类型与实例的键值对；所有注册变更统一通过 Set 完成，不改变父集合。集合不判断具体特性的能力，也不维护名称、版本或兼容描述。

ITransactionFeature.Suppressed 声明事务抑制行为，TransactionFeature 的构造参数 suppressed 默认为 false，属性只读，Instance 为默认实例。DataSession 仅在 Suppressed 为 true 时禁用事务，未注册或为 false 时保持默认事务行为；数据源可注册默认实例覆盖驱动的抑制声明。MySQL 返回值模拟同样检查该属性。

IConnectionSharingFeature.CanShare(IDataSource) 判断多个活动读取器是否可共用连接。ConnectionSharingFeature 与接口放在同一个文件中，提供默认返回 false 的虚方法及共享 Instance。DataSession 在初始化时获取当前数据源的特性并保存判断结果，未注册或返回 false 时不共享活动读取器连接；判断在分配同步资源及加入环境事务前完成。DataSource 仅继承驱动的特性，不解析数据库专用连接选项。MsSqlDriver 注册继承 ConnectionSharingFeature 的私有嵌套类 MultipleActiveResultSetsFeature，按当前数据源的连接字符串，经与 CreateConnection 相同的 MsSqlConnectionSettingsDriver.GetSettings(...).GetOptions() 流程读取 MultipleActiveResultSets；实例不保存数据源状态。事务和连接共享特性均通过 Get 获取，包含父集合的声明。

IReturningFeature.Supports 按实际语句的操作类型和 ReturningKind 声明能力，构建器据此生成 INSERT/UPDATE/UPSERT/DELETE 投影，忽略不支持的值种类。新增及增改的内建序号取值同样按实际操作的新值能力生成返回字段，否则使用驱动的序号查询。DataMutateExecutor 在有返回投影的写入语句上调用特性；ExecuteAsync 的普通写入回调绕过特性分派，至多调用一次，保留参数绑定、计数和执行器钩子；结果填充回调复用 OnMutatedAsync，不拥有读取器。特性等待所有回调，并释放自己创建的读取器、命令和租约。返回布尔值仍只控制从属语句执行，不表示是否回退。实例不得保存单次操作状态；当前上下文的操作类型不一定等于级联语句的操作类型，能力判定使用实际语句。

ReturningFeature 默认直接等待普通写入回调，使用原生 SQL 返回投影；支持新增新值、更新及增改新旧值和删除旧值。SQLite、DuckDB 使用 older:false，不声明更新或增改旧值。MySqlDriver 注册仅支持 UPDATE/DELETE 的 MySqlReturningFeature，由该特性实现模拟返回值，使用公共 DataExecutor.Instance。IMultitableFeature 只声明 CanUpdate、CanDelete，MultitableFeature 保存不可变能力值；SQL 生成仍属于原有构建器和访问器。

单表关联更新由 UpdateStatement 的内部嵌套类 Link 保存内部返回字段与外键对应关系，DataUpdateExecutor 收集完整键元组、排除含空分量的关联并去重，以最多 128 个元组、900 个总参数的保守预算分批执行。各批次的后续关联键先合并再执行下一层，避免共享目标跨批重复更新。内部关联字段与公开返回列分开收集，不生成临时表。SQLite、DuckDB 使用新值关联键，构建时拒绝修改关联锚点；其他原生驱动使用旧值。

DataReturning 按操作上下文合并 Options.Returning 和上下文 Returning 的请求列，但为双方维护独立 RowCollection 投影和值数组。上下文请求必须由当前 IReturningFeature 支持且为主实体简单属性；调用方请求的不支持种类沿用既有行为。Prepare 在构建及写入前验证必需能力；Populate 仅收集无导航 Schema 的主实体语句；Validate 在整个语句树完成后、会话提交前校验必需返回行及列，数据库空值有效，缺失列无效。会话提交成功后分别 AddRange，失败或取消时 Discard 本次缓冲并清零影响行数，保留调用方以前的结果。上下文只交付本次结果，不以 options 参数存储内部状态。执行期间保持两份请求及列定义不变。环境事务的提交仍由调用方决定，操作级成功不代表环境事务已提交。导入没有 Returning 契约，失败清零 Count，避免异常已处理时后置过滤器误发成功事件。

MySQL 更新、删除模拟在同一会话连接租约与事务中运行：检查映射主键及事务能力，核实参与表的 InnoDB 引擎，创建带完整主键索引的空临时表，再以 INSERT ... SELECT DISTINCT ... FOR UPDATE 保存目标键、所需旧值及内部关联字段，随后核实锁定后的表元数据。查询参数只绑定一次并保存结果，写入参数在锁定后绑定。原生写入使用快照键集合，不重用原筛选条件。更新后核对主键匹配数量，通过联接快照与目标表直接读取新旧值；删除后直接读取快照。临时表在 finally 中以不可取消的清理命令删除，租约归还后才允许提供者提交。该流程不保存客户端 DataTable，不使用独立 Connector 连接或活动读取器的备用连接。显式返回值的关联更新采用通用单表关联流程。

DuckDB 的标准 Appender 必须匹配全部物理列；选择字段导入通过连接级临时表接收所选字段，再以 INSERT ... SELECT 写入目标表，保留未指定列默认值，ConstraintIgnored 使用 INSERT OR IGNORE。DbType.Object 的自定义类型使用参数化逐行写入。默认借用环境事务；无环境事务或显式抑制时，由导入器拥有独立连接与批次事务，失败等待回滚。

- 八个驱动的错误转换使用 OnErrorAsync，导入统一通过 GetRecordsAsync 消费数据，并按字段顺序等待 Member.GetValueAsync；每次返回后先更新 Target，再使用 Value。
- MySQL 异常转换所需索引元数据通过 GetSchemaAsync 读取，按标识符直接筛选行并按 ORDINAL_POSITION 排序，不拼接 DataTable.Select 表达式。临时导入文件的准备、写入和加载共用清理范围，写入与流释放可等待。SQL Server 的 GetBatchesAsync 先准备第一批再取得连接租约，后续批次在上次 SqlBulkCopy 写入完成后准备；每批最多 1024 行，列映射和类型只解析一次，同一 DataTable 清空后复用。输入枚举器及表由异步迭代器释放，累计计数仅在全部成功后写回上下文。默认共用会话事务；显式抑制环境事务且使用无事务独立连接时，失败前完成的批次保留。SqlBulkCopy 的同步 Dispose 只清理批量对象，不释放调用方拥有的连接。PostgreSQL BinaryImport 的 StartRow、Write、Complete、Dispose 均等待异步接口。
- SQLite 底层 Open 和命令执行受 Microsoft.Data.Sqlite 的同步限制；SqliteConnector.OpenAsync 等待 PRAGMA 完成后通知 Open，PRAGMA 缓存策略保持按连接字符串使用。DuckDB 命令读取和释放向提供程序异步入口转发，标准 Appender 的本地写入及释放仍同步。不要以 Task.Run 或阻塞等待包装这些限制。
- TDengine 使用社区维护的 TDengine.Connector.Async 3.2.1.7，默认且只接受 WebSocket。SDK 的 ADO.NET 类型仍以同步客户端执行；TDengineDriver 内部 Connection、Command、Reader 直接调用 ITDengineClientAsync、IStmtAsync、IRowsAsync。命令拥有预备语句，读取器拥有结果集，连接拥有客户端；改变命令文本只标记重新准备，旧语句在下一次异步执行或释放时清理。重复释放等待同一清理任务，同步执行/读取和未完成清理时的同步释放明确拒绝。Database 与 ServerVersion 在 OpenAsync 中异步读取并缓存，ChangeDatabaseAsync 更新数据库名称；属性读取不发起网络请求。
- TDengine 导入通过 DataSession.Connector.ConnectAsync 打开独立 SDK 客户端，保持按子表聚合和每批最多 1000 行的原生语句提交；所有准备、绑定、提交与释放均等待并传递取消。事务仍明确不受支持。
- ClickHouse 的批量写入使用 ClickHouseClient.InsertBinaryAsync，采用租约连接的设置及当前数据库，Compression 同时控制批量压缩。ClickHouse、InfluxDB 尚未完整实现；只维护受影响异步接口，不以接口存在推断业务能力。InfluxDataReader 只异步读取与释放，取消令牌传入源枚举器；InfluxDB3.Client 1.0.0 仅提供同步 Dispose，其本地客户端释放保留该 SDK 边界。

## 数据连接与故障保护

连接路径统一为：

```text
DataSession
  → DataConnectorManager.GetConnector(IDataSource)
  → DataConnector
  → DbConnection.OpenAsync
```

- `DataSession.Connector` 是驱动层建立独立连接的公共入口。普通命令和数据导入都应通过该 Connector 打开连接，不要直接重复实现连接保护。
- `DataConnectorManager` 是公共静态管理器，使用 `ConditionalWeakTable<IDataSource, DataConnector>` 为每个数据源共享 Connector。
- `DataConnector` 是公共类，但熔断器、状态、选项和状态变更事件均为内部实现，不要通过 `IDataDriver`、`DataSession` 或 Feature 暴露熔断器。
- Connector 使用信号量串行化同一数据源的物理连接建立。首次失败会打开内部熔断器，等待者及后续请求在恢复时间到达前快速抛出 `DataConnectionException`，避免连接池和线程池被失败连接风暴耗尽。
- `DataConnector.Failed` 只在真实物理连接失败时通知业务层，熔断期间的快速拒绝不会重复触发，避免形成通知或日志风暴。事件参数 `DataConnectionFailureEventArgs` 提供数据源、连续失败次数、下次重试时间和 `ExceptionHandled`；业务订阅者可将 `ExceptionHandled` 设为 `true` 以接管默认处理。处理器签名为 `Func<object, DataConnectionFailureEventArgs, CancellationToken, ValueTask>`，依注册顺序逐个等待；故障通知使用 CancellationToken.None，处理器异常逐个隔离，仍传播连接异常。
- 未被业务层处理的连接失败默认调用 `Zongsoft.Diagnostics.Logging.GetLogging<DataConnector>().ErrorAsync(...)` 写入错误日志，并等待日志完成；日志异常仍隔离。日志消息从资源文件读取，并可包含数据源名、驱动名、服务器地址、数据库名、用户名、连续失败次数和重试时间；不要记录完整连接字符串、密码或其他凭据。
- `DataConnectionException` 提供 `SourceName`、`DriverName`、`RetryAt` 和 `RetryAfter`。首次物理连接失败且清理成功时抛出提供程序原始异常；熔断后的快速拒绝才抛出该异常。
- 熔断配置从数据源 `Properties` 读取：`CircuitBreaker.FailureThreshold`、`CircuitBreaker.Duration`、`CircuitBreaker.MaximumDuration` 和 `CircuitBreaker.Jitter`。
- 类型化连接设置（`ConnectionSettingsBase<TDriver, TOptions>`）按驱动描述器区分主参数和扩展属性集（`Properties`）；有效复合路径属于主参数，未知项属于扩展参数。完整 `Value` 包含两者，原生选项由 `GetOptions()` 生成。熔断配置写入设置文本或 `settings.Properties`，不得写入提供程序的原生连接字符串；校验主参数修改后扩展项仍可经 `Value` 重建。具体写入约束及默认值语义见 [Core 设置契约](../Zongsoft.Core/docs/settings.zh-Hans.md)。
- SQLite 和 DuckDB 当前也经过相同 Connector、信号量和熔断路径，并没有自动跳过。现有基准显示：DuckDB 没有可见退化；SQLite 默认池化连接在 16 路并发打开时每连接增加约 3 微秒，顺序打开没有稳定差异。该绝对成本不足以支持增加禁用机制，因此保持统一保护；Connector 实现或典型负载发生明显变化后再重新测量。不要在核心中硬编码驱动名称。

驱动导入器需要独立连接时，使用：

```csharp
await using var connection = await context.Session.Connector.ConnectAsync(cancellation);
```

对于提供原生连接函数的驱动，使用 Connector 对应的泛型异步重载。内部 StateChanged 只承载本地状态通知，保持同步。

IDataSource.GetSchemaAsync 负责元数据获取；DataSource 串行保护同一数据源的缓存访问，连接建立、GetSchemaAsync 和释放均被等待。获取失败返回 null，刷新失败不覆盖已有缓存；调用令牌取消时传播取消异常，连接释放异常按标准 finally 语义传播。

PostgreSQL 异常转换以 PostgresException.SqlState 为依据：23505 返回 DataConstraintException，23503 返回 DataConflictException，22001 返回 DataArgumentException；其它错误原样返回，转换结果保留原异常。名称来自结构化字段，不解析 Message/Detail，冲突 Value 保持 null。唯一约束通过同一数据源的 Connector 建立独立连接，参数化查询 pg_catalog 并严格匹配 Schema、表和索引；按索引键顺序返回字段，排除 INCLUDE 列，表达式索引返回空字段。元数据不可用时保留已知唯一约束信息，调用令牌取消仍传播；连接、命令和读取器均异步释放。

## 会话与资源所有权

DataSession 和 ConnectionLease 仅通过 DisposeAsync 释放。会话连接准备依次等待 OpenAsync 和 BeginTransactionAsync；不在异步入口调用同步 BeginTransaction。共享租约计入会话活动，接受提交或回滚后停止接受新的普通活动，终结任务等待最后共享租约归还，再按事务提交/回滚、事务释放、连接释放的顺序收尾。CommitAsync/RollbackAsync/DisposeAsync 的重复调用必须观察已接受的结果；释放租约前等待会话终结会造成循环等待，调用方应先归还租约再等待终结。

独立租约拥有独立连接，不参与环境事务，也不随会话终结释放；外部连接租约不释放借用的连接或事务。环境事务登记等待会话真正终结，操作级 CommitAsync/RollbackAsync 不代替环境事务决定。连接关闭不得丢弃未终结的事务引用；提交结果不明确时保留故障，不能隐式重试写入或报告已经回滚。

SessionCommand 负责转发命令的 DisposeAsync，SessionReader 负责转发 ReadAsync/NextResultAsync 和 DisposeAsync；CloseAsync 与 DisposeAsync 共享读取器终结结果，先释放读取器，再归还租约。命令、读取器和租约的重复释放共享结果。作用域内持有的租约使用 await using；需要转移所有权的资源仅在准备失败时释放，采用标准异常传播语义。会话和读取器的终结器仍须尝试释放全部自有资源，并汇总各项终结故障。ADO.NET 必须实现的同步执行、读取、Prepare 和枚举成员抛出 NotSupportedException；同步 Close/Dispose 仅在异步清理成功结束后无操作。已读取行的本地字段访问保持同步。

## 驱动测试

先在驱动目录中查找测试项目及容器声明：

```powershell
rg --files Zongsoft.Data/drivers | rg -- "-pod\.yaml$|Tests\.csproj$"
```

- SQLite 和 DuckDB 是进程内数据库，运行其单元测试不需要启动数据库服务器容器。
- 其他驱动测试目录的 `*-pod.yaml` 描述 Podman 数据库依赖。先检查已有服务与运行授权；用户允许启用测试容器时才按 YAML 启动缺失服务，并等待数据库真正可连接。Running 不等于 Ready，不停止或删除非本次创建的资源。
- 当前仓库中的 MSSQL、MySQL、PostgreSQL 和 TDengine 测试目录包含 `*-pod.yaml`；仍应每次动态扫描，不要把该列表视为永久不变。
- 本机容器不豁免凭据规则。真实用户名、密码及完整连接串不写入源码、文档、测试输出摘要或提交记录；通过本地环境配置注入，使用唯一测试数据库/表或键前缀，清理仅限本次测试数据。
- 先运行与改动直接相关的测试，再运行对应驱动项目的完整测试。使用 `--blame-hang` 和合理的 `--blame-hang-timeout` 防止连接失败测试无限挂起。
- 多目标框架项目按 `net8.0`、`net9.0`、`net10.0` 分别还原和测试。中央包版本按目标框架定义；发生 NU1605 时先检查 `Directory.Packages.props` 与数据库提供程序的传递依赖约束。
- MSSQL 测试项目在 net9.0 下不要直接固定 `Microsoft.Extensions.Caching.Memory` 9.0.2，因为 `Microsoft.Data.SqlClient 7.0.2` 要求至少 9.0.13；net8.0 和 net10.0 仍需要各自匹配的直接运行时依赖。

连接故障韧性测试至少验证：

1. 使用不可达端点和较小连接池触发一次真实物理连接失败。
2. 首次失败且清理成功时保留提供程序原始异常，下一次请求得到 `DataConnectionException`。
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
- `mutability` 声明命令对数据的读写特性，也是读写分离场景中的数据源路由依据。`DataSourceSelector.DataSourceWeighter.Get` 对 `Execute` 上下文直接读取 `Command.Mutability`：`none` 选择可读数据源，`insert`、`update`、`delete`、`upsert` 选择可写数据源，不会分析 SQL 脚本来推断。注意：XSD 声明的缺省值为 `none`，但映射加载器 `MetadataFileResolver` 对未声明 `mutability` 的命令实际按 `Delete|Insert|Update`（可写）处理，因此**显式声明 `mutability`**，只读命令务必写 `mutability="none"`。加载器对 `type`/`mutability` 的解析不区分大小写，但为通过 XSD 校验应统一使用小写。
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
