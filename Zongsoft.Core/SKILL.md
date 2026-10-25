---
name: zongsoft-core
description: 设计、修改、审查或测试 Zongsoft.Core 的公共契约与基础实现。适用于 Data、Messaging、Communication、Services、Configuration、IO、Security、Serialization 等跨项目 API，以及评估公共变更的兼容性和下游影响；不用于只属于某个驱动或第三方适配器的行为。
---

# Zongsoft.Core 公共契约

## 先确定契约所有者

1. 阅读 [AGENTS.md](AGENTS.md) 和目标领域现有接口、基类、扩展方法与测试。
2. 搜索 Data、Plugins、Web、messaging、externals 中的所有实现和消费者。
3. 只有多个生产实现具有相同语义时才修改 Core；单一实现需求留在下游。

## 兼容性检查

- 公共类型、成员、默认值、异常类型、取消和释放语义都属于兼容契约。
- 序列化、文本解析、配置键、URL/Topic、时间和数值格式变化需要兼容旧输入或明确迁移策略。
- 异步 API 不同步阻塞，不吞掉取消或后台异常；集合和注册表明确线程安全与快照语义。
- 数据异步重载将 CancellationToken 放在末尾，并为常见参数前缀提供直接传令牌的短重载；调用方使用位置令牌检验便利性，不以命名 cancellation 绕过缺失入口。默认值只放在能明确承接省略调用的入口：短入口的令牌可选时，长入口对应的业务参数必填；不能同时匹配同一正常省略调用。调用方以具体类型的常量、实例或默认值区分 null/default 的业务含义，不为未定型表达式排列组合增加重载。保留 params 与位置令牌所需入口，核对类型明确的参数、条件、泛型键和排序的绑定及转发。
- 可选能力通过 Feature、Provider、Factory 或现有扩展点表达，不用宽泛接口成员迫使所有实现伪支持。
- 热路径优化先确认分配、锁竞争和复杂度，再用聚焦测试或基准证明。

## 异步调用与资源契约

- `Message.AcknowledgeAsync`、发送器扩展和 `IRequestToken.GetResponsesAsync` 的消费必须可等待；响应通过 `await foreach` 枚举，保留取消、超时和提前结束的清理语义。临时数据流由创建者在发送完成后释放。
- `IMessageQueue`、`IMessagePoller` 与 `IDistributedLock` 使用异步释放。队列和轮询器停止接收操作后等待在途工作及自有资源清理；重复释放共享完成结果。借用容器服务不由调用者释放。
- 事件注册器通过 `RaiseAsync` 等待处理器；`EventBinder` 只绑定可等待的委托事件。认证事件使用 `Func<object, TArgs, CancellationToken, ValueTask>`，等待所有处理器；认证失败通知使用不可取消标记，通知故障保留原认证异常。
- 命令完成协议为 `ICommandCompletion.OnCompletedAsync`。执行器按逆序等待全部清理，单项失败不能跳过其余资源。Windows Shell 等待进程退出并排空 stdout/stderr；超时返回 -1，取消保留调用方令牌，两者都不终止已启动进程。
- 日志异步入口由调用方等待并观察失败。`Logging` 立即调用日志器，将每个 `ValueTask` 恰好一次转换为 `Task`，返回的异步结果涵盖全部已发起操作；单个日志器同步抛错不跳过其余日志器，取消标记原样传递。同步 `Log` 与 `Flush` 通过 `Common.Asynchronous.FireAndForget` 观察返回任务，不阻塞等待，不向调用方传播日志器失败。`Logging` 不缓存日志、不维护在途任务集合、批次或错误状态；缓存、完成保证和故障报告由具体日志器负责。`FlushAsync` 只转发刷新，不作为此前同步调用或未等待异步调用的完成屏障。上下文在自有模块和初始化器清理后刷新日志，宿主容器随后释放共享日志器；有完成要求的清理日志必须使用并等待异步入口。
- `HardwareProfile.LoadAsync/GetCurrentAsync` 与 `IHardwareCollector.CollectAsync` 构成硬件采集入口；外部进程输出应异步读取并等待退出。
- `IFile` 和 `ISequence` 同时提供同步与异步契约；是否支持同步远程调用由适配器明确声明。Core 不为异步适配器提供阻塞包装。
- `IDataAccess`、`IDataService` 的数据库操作与结果枚举由数据引擎负责。事务终结只使用可等待入口，调用方必须保持完成顺序，不能以弃置任务隐藏同步边界，也不能将边界扩散到通用事件、文件或消息接口。

## 数据访问与服务

IDataAccess、IDataService 的数据库操作使用异步入口；查询返回 IAsyncEnumerable<T>，通过 await foreach 或可等待的集合扩展读取。CT 位于最后，纯内存条件、模式和键转换保持同步。IDataAccess 由所有者等待 DisposeAsync；服务容器提供的共享访问器由容器释放。

条件查找统一使用 ConditionExtension 的匹配遍历，ConditionCollection 的 Contains/Find/FindAll/Match/Matches 转发到此入口。每层只返回本子树的匹配数量，数量上限按剩余额度传给子树，首项匹配须停止整个遍历。深度只在 AND/OR 组合切换时递增。匹配使用 FieldOperand.Name，与单个 Condition 一致，不限制操作符。需要确定等值时使用 ConditionExtension.TryGetValue；它复用深度为 1 的公共遍历并排除 OR 根，只接受 AND 链的等值常量，解包 ConstantOperand，拒绝区间、集合（字符串和 byte[] 除外）、条件及表达式。重复值按 Object.Equals 检查冲突，不推断数据库转换；泛型重载只在取得确定常量后转换，null 仅适用于引用或可空类型。DataServiceBase 的公开及受保护 UpdateAsync 统一调用此入口，不在服务层另写条件遍历；DeleteAsync 保留完整筛选条件。条件查找、条件改写和父记录条件投影继续使用 Find/Matches，不替换为单值提取。

DataServiceBase 构造时按类型继承层级选取最近一层声明的 DataServiceAttribute，泛型特性的不同封闭类型不跨层合并。没有本层声明时才继承；同层多个匹配声明维持反射歧义异常。

访问层按前回调、前事件、过滤器前段、执行、过滤器后段、后事件、后回调的顺序等待；前回调或前事件取消时跳过后续执行和后通知。服务层保留前事件后调用前过滤器、后事件后调用后过滤器的顺序。事件使用 Func<object, TArgs, CancellationToken, ValueTask> 并按订阅顺序逐项等待。失败立即向当前操作传播，不以 async void 或后台任务隐藏失败。

SelectAsync 在调用时启动一次准备，枚举时等待同一准备结果；未枚举也观察准备故障并释放上下文。可兼容的已完成短路结果保持对象身份。方法令牌和枚举令牌共同约束枚举；取消一个等待者不取消共享准备。IDataAccessContext 的 Dispose 只清理本地状态，不接管数据会话。

DataSelectContextBase.Result 可承载内存序列或兼容的异步序列。Get 的结果过滤发生在首次读取前；Getted 在取得首条或确认无结果后通知，DataGettedEventArgs.Result 是已有值快照，读取属性不再次枚举。DataGettingEventArgs 只提供待查询上下文与取消标记，不提供尚未取得的 Result。取消的 Get 不执行 Getted。

InsertManyAsync、UpsertManyAsync、UpdateManyAsync 与访问层 ImportAsync 支持 IAsyncEnumerable<T> 及原有内存集合输入。服务批量新增/增改通过 IAsyncEnumerable<IDataDictionary<TModel>> 的 OnInsertManyAsync/OnUpsertManyAsync 扩展点处理，逐条等待 OnModel 后的验证；公共内存入口也走这一扩展点。DataImportContextBase.Data 使用 object，ModelType 识别两种输入的元素类型。GetModelAsync/GetExportModelAsync 等待模板取数，导入流和导出流的所有权属于调用方。

IPageable.Paginated 使用可等待的委托，分页处理完成后才输出首条记录。包装器按需订阅源事件并转发自身作为 sender，取消最后一个订阅者时解除源订阅。纯内存筛选可以同步计算，不允许由同步迭代器读取数据库。

GetDataDictionariesAsync<T> 为插入、复写和更新上下文提供可等待的数据字典遍历，支持异步引用类型、值类型及内存集合；同步 GetDataDictionaries<T> 只用于内存集合。上下文 Data 的重复读取能力由输入数据源决定，需要多次读取的业务在边界保存已验证字典，不能重复消费一次性源。服务导入的 OnImportAsync 数据钩子只接收 IAsyncEnumerable<TModel>。

DataUpdateContextBase 保持单条更新契约；业务后置过滤器若额外接受批量输入，按实际 Data 形态选择字典工厂，不能以 IsMultiple 推断批量。字典本身应视为单条。

DataDictionary.GetDictionariesAsync<T> 转换内存集合，GetDictionariesAsync<T, TSource> 转换异步数据源；公共工厂只负责字典转换与逐条等待 handle，服务授权、模式准备、OnModel 和验证保持在调用方。无回调入口接受末尾可选令牌；带回调入口的 handle 必填，允许显式 null。保留惰性枚举、空元素跳过和源枚举器的释放语义，不缓冲或重复消费输入。

## 数据验证与异步结果转换

IDataMutateContextBase.Returning 是四种写入上下文各自拥有的混合种类请求与结果，初始列和行均为空。扩展在执行前追加必需列，完成后只读本次结果；不能借用或改写 Options.Returning、Parameters。DataServiceBase 的导入数据入口通过 IDataAccess 的前后回调调用已有服务过滤器；前置短路跳过执行及后置过滤器，后置故障向调用者传播。

Returning 的 ColumnCollection 使用列数组，构造时复制输入数组，增删列时替换数组。TryGetValue 按不区分大小写的名称与 ReturningKind 查找并返回实际存储的列；省略种类或传入 null 时使用集合固定种类，无固定种类则使用 Older。名称为空、种类与集合不符或列不存在时返回 false，输出默认值；Contains(name, kind) 复用此查找入口。Row 保存采集时的列数组、默认 ReturningKind 和值数组，通过名称与 ReturningKind 查找值。RowCollection 根据列数组身份及读取器字段名缓存列序号，优先匹配别名，再回退到名称；缺失列与数据库空值分别保存。

RowCollection.Populate 解析并追加当前记录，AddRange 直接追加另一个行集合中的行。Core 只负责记录解析和行集合操作，操作过程中的暂存、成功追加与失败丢弃由 Data 引擎负责。集合不支持并发修改。

Row.ToDictionary(kind)/ToDictionary<T>(kind) 使用该行采集时保存的列和值生成忽略大小写的独立字段字典，再复用 DataDictionary 工厂。缺失字段跳过，null 保留，不按 T 反射补字段，不深复制字段值。必须显式选择 Older/Newer，非法或与固定种类冲突时抛出 ArgumentOutOfRangeException。RowCollection.ToDictionaries 两个重载逐行惰性转换，每次枚举生成新的字典，不跳过空投影行、不修改结果、不查询数据库。事件等需要稳定集合的调用方自行一次物化；选项复用时自行截取本次新增行。

IDataValidator 的条件验证使用 ValidateAsync；字段验证使用 OnImportAsync、OnInsertAsync、OnUpdateAsync，返回 DataValidatorResult。HasValue 为 false 表示不提供替代值，为 true 时 Value 允许为 null 或 IDataValueBinder。上下文验证保持显式条件优先、缺少验证器及 ValidatorSuppressed 跳过验证器的行为，不自行改写 Criteria；字段转发保持 Insert/Update 的选择规则。IDataValueBinder.BindAsync 在参数绑定时被等待，不能在返回验证结果时提前执行。

Pageable.FilterAsync/MapAsync 消费 IAsyncEnumerable，按原枚举次序逐条等待回调，把枚举取消令牌传给源与回调。FilterResult<T>.Accepted 决定是否输出 Data，允许接受 null，default 表示丢弃。纯内存 Filter/Map 保留；包装器传递 IPageable、Suppressed 和分页事件，事件 sender 为最外层包装器。重复读取 Current 不重复调用回调；提前退出或失败等待源枚举器释放，并保持标准 await foreach 的异常优先级。

## 事务生命周期

事务调用方的改写以保留现有行为语义为前提：先核对事务范围、提交时机、异常处理、取消和资源归属，再选择 await using、显式终结或 RollbackAsync(Exception)。RollbackAsync(Exception) 只适用于需要保留业务异常原因的回滚路径；调用后是否重抛由原调用方语义决定，不把已经处理的异常改为抛出。语义修正须基于已发现的 bug，说明问题、预期行为及对应验证。

`Transaction` 只实现 IAsyncDisposable；CommitAsync/RollbackAsync/DisposeAsync 共享 TransactionContext 的终结结果。首次提交在发布终结决议前检查取消；已接受的提交、回滚、登记回调与完成通知使用不可取消标记。DisposeAsync 必须在非 async 外壳中同步恢复调用方的 AsyncLocal 环境，再返回终结任务。嵌套上下文只处理本地完成通知，登记进入根事务；嵌套回滚将根事务标记为 rollback-only。

RollbackAsync(Exception) 只在首次接受回滚决议时捕获业务异常；回滚及通知成功时正常返回，不主动重抛业务异常或业务取消。存在终结错误时才合并业务异常，按引用避免重复添加同一业务异常，并将聚合结果发布到共享终结任务，确保后续 DisposeAsync 不覆盖聚合结果。业务异常不参与 Status 判定；已接受的提交或回滚复用原结果，不追加原因、不重新执行回滚。null 原因沿用无原因回滚语义；无类型 default 会使公开回滚重载产生歧义，调用方使用无参数入口或明确类型的令牌。行为验证见 test/unit/Data/TransactionLifecycleTest.cs。

TransactionContext 在短锁内保存终结任务和登记快照，锁外依次等待所有 IEnlistment.OnEnlistAsync。登记失败不能跳过其它登记；登记结果决定 Status，通知失败仅进入最终异常结果。根事务的条件特性在用户 Completed 回调之前发布，回调可等待条件而不能反向等待自身终结任务。Completed 使用 Func<object, EventArgs, CancellationToken, ValueTask> 并逐项等待，失败汇总后向调用方传播。

三个 UpdateManyAsync 批量更新入口、角色与用户批量删除及 StateMachine.StopAsync 使用 await using 管理事务；业务操作和提交置于同一 try 块，catch 中等待 RollbackAsync(exception) 后以 throw 重抛，保留业务失败与回滚失败。参数校验和事务范围保持各入口的既定边界。StateMachine 通过 RunAsync 依次等待 GetStateAsync、TransferAsync 与 HandleAsync；状态值比较、CanTransfer 和处理器定位保持同步。DisposeAsync 按原记录顺序等待 FinishAsync、SetStateAsync、停止事务及清理，完成阶段使用 CancellationToken.None。FinishAsync 的同步及异步失败均保留 TargetInvocationException 包装；派生类通过 DisposeAsyncCore 扩展，重复释放共用完成结果。调用方先等待 RunAsync 完成再释放，不并发使用同一状态机。调用方异常传播见 test/unit/Data/TransactionCallerTest.cs，事务测试位于 test/unit/Data，状态机释放测试位于 test/unit/Components。

## 应用宿主生命周期

ApplicationContext 通过 IHostedLifecycleService 等待工作器启动、失败回滚和停止；同一阶段共享完成任务，取消等待不放弃清理，回调不得反向等待所属上下文生命周期。Exit()/Exit(exitCode) 只请求 IHostApplicationLifetime.StopApplication，不强制结束进程。上下文通过 DisposeAsync 等待实际停止，再释放自有初始化器和模块；自有资源清理后等待 Logging.FlushAsync，共享日志接收器由宿主释放。容器解析的初始化器由容器释放。IApplicationBuilder<TApplication> 使用 BuildAsync，创建入口须等待初始化失败时的异步释放。服务容器视图不拥有其引用的容器，根容器支持异步释放。

## 终端输入

ITerminalExecutor 仅通过三个 RunAsync 重载运行命令循环，调用方等待 Task<int> 获取退出码。ITerminal.Input 使用 TextReader；普通流和内存文本直接使用 StreamReader、StringReader。Terminal.Console.Input 默认返回唯一的进程级行读取器，仅两种 ReadLineAsync 共用读取状态，ReadLine 始终抛出 NotSupportedException，不消费待领取结果；首次读取启动一个后台线程，按需调用 Console.In.ReadLine，不预读下一行。标准输入同时只允许一个等待者，取消只结束等待，未完成读取及结果留给下一次调用，EOF/故障保留；Peek 和字符读取不受支持。标准输入 Dispose/Close 无操作，执行器停止不关闭输入或等待底层标准输入读取。注入读取器由创建者释放，其取消语义取决于具体 TextReader，不承诺标准输入的结果保留行为。读取开始后不得更换 Console.In 或混用其它标准输入读取入口。具体契约见 [终端输入](docs/terminal-input.zh-Hans.md)。

ITerminal.Abort 同步触发 Aborting 并返回最终 Cancel，异常向调用方传播。Core 不订阅全局进程信号或修改控制台模式。Plugins 终端宿主对 Ctrl+C 优先通知终端、未处理时停止，SIGQUIT/SIGTERM 直接停止，等待业务清理。ReactiveAsync 在运行取消或进入失败后仍等待不可取消的退出清理，保留运行和清理故障。

## 周期任务生命周期

[Common.Timer](docs/timers.zh-Hans.md) 通过构造回调组合行为，构造时不启动时钟；每次 Start 创建独立的私有 Runner 并串行执行回调。每次运行使用接受 TimeProvider 的 PeriodicTimer，顺序等待 tick 与回调，由 PeriodicTimer 合并到期通知；所有生命周期状态共用同一个短锁。Start、Stop、StopAsync、Dispose、DisposeAsync 均无取消参数；每次运行创建内部取消源，停止和释放请求取消在途回调。Stop 不等待回调和清理结束，StopAsync 等待实际退出及收尾；Dispose 永久关闭但不等待，DisposeAsync 永久关闭并等待同一次清理，重复释放不重复清理。调用方在释放后不得再启动或访问 Period，仍可读取 IsRunning、IsDisposed。Failed 在清理结束后通知故障，Stopped 随后通知每次实际运行已结束；派生类通过 OnFailed(FailureEventArgs) 和 OnStopped(EventArgs) 定制通知，调用基类方法以通知订阅者；Failed 逐个调用订阅者，Stopped 使用普通事件调用、订阅者抛错中断后续通知；异步停止与异步释放等待通知调用结束并保留运行和通知异常，计时器内部观察后台任务异常、不写日志。回调及取消或事件通知不得反向等待自身运行；回调内请求停止或释放用 Stop、Dispose，由调用方保证不存在循环依赖，计时器不检测执行上下文。运行结束前不得释放回调依赖的外部资源。所有者通过事件衔接业务任务时，业务任务在事件调用链外等待 StopAsync 后继续。自定义 TimeProvider.CreateTimer 须成功返回有效计时器；不额外包装 PeriodicTimer 的失败构造终结器限制，具体约束见计时器文档。测试见 test/unit/Common/TimerTest.cs，使用受控时钟和同步信号验证时序。

[Spooler<T>](docs/spoolers.zh-Hans.md) 以周期、满载写入或显式 FlushAsync 串行触发消费性 IEnumerable<T> 回调；支持组合和继承，Limit=0 为无界，正数同时限制容量与每次枚举数量。生产者在满载时参与刷新；FlushAsync 等待在途回调，再处理一轮剩余数据，不建立接收水位。仅实现 IAsyncDisposable；通道引用置空统一表示关闭并确定唯一关闭执行者，DisposeAsync 停止接收并结束容量等待，排队刷新取得执行权后检查关闭状态；释放等待在途刷新、计时器清理及故障通知，重复调用共享结果，不排空缓冲区。派生类通过 DisposeAsyncCore 在等待基类后清理回调依赖；周期和通知异常由异步释放传播，显式操作异常仍归原调用方。直接操作传播异常，周期故障终止该次定时运行并通过 Failed 通知，不永久关闭缓冲器、不重试、不写日志。Failed 使用普通事件调用，订阅者抛错中断后续通知，运行及通知异常保留到异步释放。调用方负责避免在回调与通知中等待自身刷新、异步释放或满载投递，有容量时允许投递；所有者在回调外等待释放，观察异步投递并管理依赖资源。两个构造函数均可传入 TimeProvider clock 控制周期触发。`Zongsoft.Caching.Spooler` 指标源发布接收数、当前队列、回调耗时、满载投递耗时和回调故障数；无监听器时不读取操作时钟，队列使用弱引用注册表按需采集，关闭时注销并清空 reader 委托。FileLogger 拥有其缓冲器，通过 Failed 转发周期故障；仅提供异步释放，停止接收并等待缓冲及直接写入后清理同步资源，不自动排空。测试见 test/unit/Caching/SpoolerTest.cs、SpoolerDiagnosticsTest.cs 及 test/unit/Diagnostics/FileLoggerLifecycleTest.cs；性能工作负载位于 benchmark。

## 网络声明值

[NetworkAddress 和 NetworkProtocol](docs/networking.zh-Hans.md) 提供通用网络地址及协议组合的解析、规范化和相等语义，均实现 `ISpanParsable<T>`（包含 `IParsable<T>`），公开 string 与 `ReadOnlySpan<char>` 重载。NetworkAddress 不设置应用方案白名单，保留地址族、区分大小写的 IPv6 区域及原始路径。NetworkProtocol 接受任意合法上层名称与 tcp、udp、http、http1、http2、http3 承载组合，组合顺序无关，不补全应用协议的默认承载。集合使用必须指定分隔符的 Parse/TryParse 重载，在相同上层名称内归并 HTTP 泛称，保留所有显式版本及未指定承载的名称。先通过 TemplateEvaluator 求值，再解析协议文本；应用简写、绑定路径限制及地址与协议的一致性由消费者负责。二者不执行 zongsoft.tools.binding PI、不探测后端、不选择最高版本、不生成代理配置。EndpointConverter 的 EndPoint 转换及 HandlerAttribute 的元数据契约保持独立。测试为 Communication/NetworkAddressTest 与 NetworkProtocolTest。

## 服务注册与解析调用链

- [ServiceCollectionExtension](src/Services/ServiceCollectionExtension.cs) 扫描程序集 DefinedTypes，跳过非类和未闭合的泛型类型：`IServiceRegistration` 支持非公开实现，优先使用公开静态字段或属性供应实例，否则以公开或非公开无参构造创建；无实例供应的抽象注册器不实例化。普通 `ServiceAttribute` 与选项扫描仍要求 Type.IsVisible；通常将实现注册为 Singleton，契约指向同一实例，Members 分支读取静态成员。不要推断成 transient。
- 模块归属来自 [ApplicationModuleAttribute](src/Services/ApplicationModuleAttribute.cs)，可沿点分主程序集约定查找；[ApplicationModule.Services](src/Services/ApplicationModule.cs) 延迟建立命名容器，模块注册优先，再回退共享服务。模块容器不是每次 HTTP 请求的作用域。
- [ServiceProvider](src/Services/ServiceProvider.cs) 对集合结果先收集模块项，再补共享项，按具体类型去重；修改顺序与去重方式会影响 Find 和默认服务选择。
- [ServiceProviderExtension](src/Services/ServiceProviderExtension.cs) 区分别名 Resolve 与匹配器 Find；[ServiceLocator](src/Services/ServiceLocator.cs) 的 `name@provider` 先解析提供者，再调用 `IServiceProvider<T>.GetService(name)`，不是解析模块容器。
- [ServiceInjector](src/Services/ServiceInjector.cs) 处理可写公共字段/属性的服务和选项注入。`ServiceDependency.Provider` 选择模块容器，ServiceName 才是请求具名实例；`~`/`.` 可映射所属模块名。
- 应用自定义 `Module.Current` 及模块树节点由应用提供，Core 不存在通用的 `Module.Current` 单例。不要为了让示例编译新增这种全局 API。

## 配置与验证陷阱

- 修改普通设置（`Settings`）、连接设置（`ConnectionSettings`）、字典接口、`Value` 或配置绑定时，先读[设置契约](docs/settings.zh-Hans.md)，从[配置测试](test/unit/Configuration)中选择对应行为的回归；驱动原生参数通过 `GetOptions()` 生成。
- [XmlStreamConfigurationProvider](src/Configuration/Xml/XmlStreamConfigurationProvider.cs) 将 XML 属性映射为配置键，文本节点则按 `[value]` 集合项处理。写标量配置时核对实际键，不能套用其它 XML Provider 的直觉。
- 连接设置驱动、服务注册、提供者首次创建分属不同阶段。服务出现在容器中不证明配置或底层 SDK 可用。
- 服务测试路由：[ServiceLocatorTest](test/unit/Services/ServiceLocatorTest.cs)、[TaggedServiceTest](test/unit/Services/TaggedServiceTest.cs)。插件装配回归需另验证应用与模块容器、属性注入、配置键和共享实例身份。
- 所有权以实际注册与工厂为准，普通消费者不释放解析到的共享实例；实例被缓存、弱引用或静态复用也不等于自动实现取消/请求隔离。

## 领域路由

- 消息契约变化同时使用 [../messaging/SKILL.md](../messaging/SKILL.md) 检查全部驱动。
- 数据 Schema 或映射变化使用 [../Zongsoft.Data/SKILL.md](../Zongsoft.Data/SKILL.md)。
- 插件装配变化使用 [../Zongsoft.Plugins/SKILL.md](../Zongsoft.Plugins/SKILL.md)。

## 验证

`Zongsoft.Core.slnx` 和默认 Cake `test` 只包含 `test/unit/Zongsoft.Core.Tests.csproj`。终端输入和 Shell 进程测试位于 `test/integration`，通过 `test/integration/Zongsoft.Core.Tests.slnx` 或 Cake `integration-test` 显式执行；Windows Shell 场景只在 Windows 运行。构建主解决方案并运行对应单元测试，集成测试按所改行为单独选择。公共行为变化至少选择一个真实下游实现做定向构建；多目标差异分别验证 net8.0、net9.0、net10.0。

集成测试通过重定向标准输入管道和隔离子进程验证进程级读取器；受控 `TextReader` 使用显式门闩模拟阻塞的原生输入。Windows Shell 用唯一命名管道控制测试子进程，并持有进程句柄完成期限控制和清理；不将这类场景加入默认单元测试。

## 监视器资源释放

`Superviser<T>` 的缓存逐出通知异步执行；`Count == 0` 或 `Unsupervised` 事件已经触发，都不能证明回调已经结束。观察者在写入缓存前计数，在逐出回调的 `finally` 中清理并递减；只有 `Dispose(bool)` 完成且全部观察者回调结束后，才能以原子交换取得缓存并释放。释放完成路径与最后一个回调都要尝试收尾，以覆盖空监视器和回调先结束的情况。不要在 `DISPOSING` 阶段释放缓存，也不要按缓存条目数提前注销逐出处理器，否则可能遗漏取消监视通知。

`MemoryCacheScanner` 只定时调用缓存的内部 `Scan()`：用私有缺失键触发底层缓存按 `ScanFrequency` 执行后台过期检查，不插入条目、不刷新业务条目的滑动有效期，也不调用 `Compact(0)` 创建压缩候选列表。底层过期扫描间隔在缓存构造时设置；`Scan()` 仅触发检查并忽略缓存已被并发释放时的 `ObjectDisposedException`，不负责同步 `Options` 的后续修改。定时器是扫描器的私有同步锁，仅串行化 `Start/Stop/Dispose`；回调通过 `Volatile.Read` 取得缓存后在锁外触发检查。`Dispose()` 清除引用并停止定时器，不等待已经开始的底层检查或逐出通知，不释放缓存；释放后 `Stop/Dispose` 幂等，`Start` 抛 `ObjectDisposedException`。监视器无需与扫描器交换完成回调。回归测试见 [SuperviserDisposalTest](test/unit/Components/SuperviserDisposalTest.cs) 和 [MemoryCacheScannerTest](test/unit/Caching/MemoryCacheScannerTest.cs)，覆盖空闲过期、滑动有效期、停止/重启、并发控制、阻塞和重入释放，以及原始逐出回调竞态。

## 字典变量视图

IVariables、Variables（含 Variables.Environments.cs）和 VariablesExtension 位于 src/Common，命名空间为 Zongsoft.Common。变量契约独立于表达式和模板语法；ProfileExtension 留在 Configuration.Profiles，TemplateEvaluator 留在 Text.Templating，均使用此公共契约。变量行为测试位于 test/unit/Common。

Variables.Wrap 提供 Wrap<TDictionary>(TDictionary dictionary, bool reuse = false) where TDictionary : IDictionary，以及 IDictionary<string, object> 和 IDictionary<object, object> 两个接口重载；VariablesExtension.ToVariables 提供相应三个扩展方法。非泛型 IDictionary 的具体实现优先匹配受约束泛型入口，避免普通 Dictionary 直接调用的重载歧义。公开入口不接受任意 object；同时实现两个泛型接口但未实现非泛型 IDictionary 的类型须显式转换，裸 null 须指定字典类型。内部适配器依次优先使用字符串键泛型接口、对象键泛型接口和非泛型接口，null 抛 ArgumentNullException，参数名为 dictionary。字典本身实现 IVariables 时直接返回，其它字典由私有嵌套适配器包装。默认每次新建适配器，不读取、登记或移除缓存条目；reuse=true 时统一 ConditionalWeakTable<object, IVariables> 按引用身份复用，不同静态接口类型共享视图且不阻止无外部引用对象回收。缓存同步不扩展到源字典。仅字符串键提供变量，其它键忽略且不调用 ToString；查询遵循 Variables 的完整键约定和 OrdinalIgnoreCase；标准 Dictionary、ConcurrentDictionary 使用相同比较器时直接查询，其它来源按枚举顺序返回第一个匹配项，不检查重名，null 值仍查询成功。测试覆盖引用身份、并发复用、关闭复用、GC 生命周期、实时修改和模板集成。详见 [变量文档](docs/variables.zh-Hans.md#字典变量视图)。

## 多来源查询

VariablesExtension.TryGetValue 为 IEnumerable<IVariables> 提供四个重载，布尔 fallback 缺省为 false。启用时按命名空间层级查询：每一级先按来源顺序进行 false 查询，再进入父级直至全局；全部普通查询失败后，才按来源顺序执行 null 命名空间的 true 查询，允许来源自身已声明的默认值。命中 null、空字符串、false 或 0 均终止。模板通过 TemplateEvaluatorOptions.Fallback 控制，默认 false，递归沿用。命名空间没有查询模式保留名。来源集合可重复枚举且查询期间稳定，忽略 null 来源，不缓存或吞掉异常；TemplateEvaluator 独立拒绝 Providers 中的 null。详见 [多来源查询](docs/variables.zh-Hans.md#多来源查询)。

## 环境变量视图

Variables.Environments(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) 及私有适配器位于 Variables.Environments.cs。每个有效来源共享无可变状态的视图，查询直接调用 Environment.GetEnvironmentVariable(name, target)，不缓存值、不展开、不合并来源。只提供全局命名空间，null/empty 等价；非空命名空间仅在 fallback=true 时回退到全局，本目标没有额外默认值，名称为 null 抛 ArgumentNullException。原生空字符串仍表示成功；Unix/Linux 的 User/Machine 查询为空，无效 target 在工厂抛 ArgumentOutOfRangeException。IVariables 保持名称与命名空间使用 OrdinalIgnoreCase 的原契约；只有 Variables.Environments 返回的视图是明确特例，环境变量名称遵循平台规则，Unix/Linux 调用方负责大小写。字典/Profile 行为不变。Wrap(Environment.GetEnvironmentVariables()) 是字典快照，不具备环境实时视图语义。测试只修改唯一名称的进程环境变量并在 finally 恢复，不修改 User/Machine 或依赖真实环境值。

## 命令选项变量

[CommandLine.CmdletOptionCollection](src/Components/CommandLine.Options.cs) 显式实现 IVariables 的四个查询重载：不带布尔参数等价于 fallback=false。只提供全局变量；false 只查全局显式选项，true 允许从具名回退到全局并在显式值缺失时查询已声明的默认值。选项名称中的点号和连字符映射为下划线，结果须符合 ASCII 标识符 [A-Za-z_][A-Za-z0-9_]*；非法选项连同短名称整体忽略，其它合法短名称仍可查询。查询参数不裁剪、不转换、不拆命名空间，名称为 null 抛 ArgumentNullException，其它非法名称返回 false。CommandOptionDescriptor 和 CommandOptionAttribute 的 HasDefaultValue 保留默认值声明状态，无默认参数的构造保持 false，显式 null 也属于已声明；Describe 仅复制已声明默认值，DefaultValue 不合成类型零值。普通 GetValue/TryGetValue 等选项 API 使用原始选项名并允许已声明默认值回退，缺失时 GetValue 抛未找到异常、TryGetValue 返回 false；可选读取使用 TryGetValue 或带调用方默认值的 GetValue。内部选项字典与描述集合统一使用 OrdinalIgnoreCase。显式值与默认值独立保留，各层重名分别按枚举顺序取首项；null、空字符串、false 和 0 均终止回退。CmdletOptionCollection 使用同文件上下两个 partial 声明，IVariables 实现在文件下部，查询逻辑直接放在带命名空间的显式 TryGetValue 中，无命名空间重载转发该入口。首次有效查询惰性创建 OrdinalIgnoreCase 名称索引，之后直接字典查找；私有只读 VariableEntry 保存 Specified、显式值以及默认值的描述引用。名称映射建立后固定，不跟踪描述集合增删或替换；默认值仍从原描述实时读取。通过 Volatile.Read 与 LazyInitializer 安全发布索引，初始化后只读，不扩展描述集合的并发保证。不复制默认值、不展开模板，集合可直接注册到 TemplateEvaluator.Providers。测试入口为 [CommandLineVariablesTest](test/unit/Components/CommandLineVariablesTest.cs)，使用公开 CommandContext 创建集合。

## Profile 指令与读取

读取机制见 [实现文档](docs/profiles.zh-Hans.md#读取与导入)。ProfileReader 识别紧接注释符的 @name，名称与参数以空格或 Tab 分隔；Argument 去除两端空白，语义由具体指令解释。ProfileReader 的内部嵌套类 Session 固定注册表与选项快照、调度指令并管理活动链及文件通知；ProfileReader 仅解析单个来源。Directives 子命名空间中的 ImportDirective 将完整 Argument 作为单个文件路径，内部空格、Tab 和 | 不分隔文件，不解释或移除引号；导入多个文件使用多条指令。它负责路径、可选/严格缺失和深度选项，经上下文内部读取操作共享会话，不能调用 Profile.Load 重建根会话。未知指令可由回调接管。

ProfileOptions 提供 PreserveBlanks、只读 Directives 集合与 Loading/Loaded；集合提供 Processing/Processed 和指回所属 ProfileOptions 的只读 Options。集合仅由所有者构造，克隆时必须绑定到新所有者。ProfileDirectiveOptions 按只读 Name 对应指令，Behavior 使用 None/Strict/Ignore/Suppress。集合名称忽略大小写，拒绝 null 和重名项，缺省配置采用指令内置默认值。ProfileDirectiveOptions.Import 创建公开嵌套 ImportOptions，MaximumDepth=0 采用默认 64，正数指定上限，负数拒绝；根配置计一层。根读取复制集合并调用每个选项的虚拟 Clone，含可变引用成员的派生类型负责复制这些成员。

Loading/Loaded 均为 Action<ProfileContext>，覆盖根与导入；文件打开及循环、深度检查后触发 Loading，解析和递归导入成功、子文件合并后触发 Loaded。FilePath/Depth/Referer/Profile 只读；匿名路径为空字符串，根 Referer 为 null，前置 Profile 为 null，后置为解析结果，前后使用不同上下文。缺失或被拒绝文件不通知，回调异常终止加载并清理，不回滚已有合并。

Directives.Processing/Directives.Processed 均为 Action<ProfileDirectiveContext>，每条指令一组，成功处理该指令及其递归导入后完成。前置可改 Argument 或设 Handled=true 接管；后置修改不重执行。上下文提供 Name、Argument、Handled、Behavior、Options、Profile、Section、FilePath、LineNumber、Depth。Options 是独立副本，指令实现直接读取它；前置回调可调整本条指令的专用设置（例如导入的 MaximumDepth），不影响会话设置、其它指令或已固定的 Behavior，未显式配置时为普通选项。Ignore 不执行且不通知指令回调；Suppress 在指令回调前拒绝；未知指令 None 未接管时保留注释，Strict 未接管则失败。文件通知与指令通知按递归顺序嵌套，保存保留原始指令注释。Profile 不提供 Variables 或自动变量展开。

[ApplicationManifest](docs/application-manifest.zh-Hans.md) 通过 Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress) } 禁止导入，并转换为 FormatException。Load(Stream) 关闭输入流，Load(TextReader) 保持读取器打开；基于 FileStream 的输入提供路径。Writer 只固定 PreserveBlanks，不复制指令选项、不执行指令或回调。Save() 按来源写回自身及导入子树的修改，显式输出仅处理当前配置；声明及保存规则见 [文档](docs/profiles.zh-Hans.md#声明与保存)。不为测试新增生产入口。

Profile.Directives 为全局线程安全注册表，内部使用 SynchronizedDictionary，默认 Directives.ImportDirective.Instance；Add 拒绝空值和重名，不提供移除或替换，枚举使用快照。每次根加载在克隆选项前复制注册表，递归共享实例引用；后续注册只影响下次加载，执行指令不持锁。ProfileDirectiveBase 提供 Name/Process，实现不得保存调用状态。选项集合不注册实现，Writer 不依赖任何指令实现。

ProfileExtension.ToVariables 为 Profile、ProfileSection、ProfileEntry 提供 IVariables 实时视图。配置读取当前有效集合；章节包含子树并保留完整命名空间；条目仅提供自身，章节和条目视图不随原集合替换而改换对象。章节层级用点连接，章节名称内的点保留，每个点分段须为 ASCII 标识符；条目名称中的点和连字符改为下划线后验证标识符。非法来源不提供变量，查询参数不裁剪或归一化。null/empty 命名空间等价全局命名空间，大小写忽略；fallback=true 才按父命名空间逐级回退，不越出视图范围，值为 null 仍表示找到。仅查询到映射重名的变量时抛 ProfileException，不预先拒绝整个视图。适配器返回原始值，不求值、不缓存结果或复制字典，也不负责并发同步；模板通过显式注册视图求值，Profile 加载与保存仍保留原文。

## 本地搜索

Searcher 仅增强 System.IO.DirectoryInfo，详见 [机制文档](docs/searcher.zh-Hans.md)。Path 用于逻辑命名，Result 为实际目标，大小写按平台固定。搜索不穿过模式中的目录链接，显式起点可为链接；配置逻辑来源规则不变。不要将遍历提前移动到固定前缀，否则会绕过中间目录链接。

Searcher 统一通过 Search 扩展方法搜索；Searcher.Target.Files、Directories、Both 指定结果类型，默认 Both；Both = 0，调用方负责传入有效的 target，Search 不进行枚举值校验。

## XML 处理指令

XML 配置读取及 PI 契约见 [实现文档](docs/xml-configuration.zh-Hans.md)。XmlConfigurationReader 负责单流词法与配置映射，XmlConfigurationSession 管理注册表及选项快照、指令调度、活动导入链和依赖。静态 XmlStreamConfigurationProvider.Read(Stream) 与 Read(Stream, out instructions, filePath) 只解析当前流，不执行指令；文件和流提供程序执行指令并发布按导入位置深度优先展开的 Instructions，只读快照与本次配置字典一起发布。内部 ConfigurationData 承载指令；文件基类清空 Data 时指令视图也为空，不能另存一个会残留的指令列表。来源的 Options 只读，AddOptionFile/AddOptionStream 的配置委托可设置它。流来源 FilePath 仅作来源标识；null 推导 FileStream.Name，其它流为空，不能以显式标识充当导入基准。

XmlConfigurationInstruction 的 Name、Data、Path、FilePath、LineNumber、LinePosition 只读，构造仅供读取器内部使用。Path 是 XML 父元素的限定名称层级，不是 option 路径或命名集合映射键；根外为空，保留顺序及重复。Data 使用 XmlReader.Value，不额外裁剪或解码。PI、声明等跳过节点不更新配置空元素检测的 PreviousNodeType 状态；option 内实际贡献配置的指令标记为有效内容，不能额外生成空项。GetAttributes/TryGetAttributes 可选，复用 XML 属性词法、单次实体解码和空白规范化，OrdinalIgnoreCase 检索且拒绝大小写变体重名，必须消费全部数据。成功缓存只读字典，格式错误为 FormatException，Try 失败输出 null；不吞掉其它异常。属性解析不继承父元素命名空间或 DTD。

XmlConfiguration.Instructions 是线程安全的处理器注册表，元素为 XmlConfigurationInstructionProcessor，默认包含 Instructions.ImportInstructionProcessor.Instance。名称按 OrdinalIgnoreCase 比较，拒绝空项、重名，不允许移除或替换，枚举为快照；共享实例不得保存每次执行状态。来源 XmlConfigurationOptions.Instructions 配置行为及 Processing/Processed，Loading/Loaded 通知根文件和导入文件。每次根加载先复制注册表，再克隆来源选项；递归共享快照，重载重新取快照。指令上下文保留原始 Instruction，Data 可改并使属性解析缓存失效；Options 每条独立克隆，Behavior 在前置回调前固定，Handled 可接管。None 使用默认规则，Ignore 只保留原定义并跳过校验及指令回调，Suppress 在回调前拒绝，未知 Strict 指令未被接管则失败。文件回调在打开及循环/深度校验后开始，Loaded 在合并到引用方之前发生；Configuration 仅含当前文件已读值，不包含引用方配置。

Import 使用必需的 path 和默认 true 的 required；None 按 required 处理，Strict 忽略 required 并要求目标存在。required=false 只忽略打开目标时的 FileNotFoundException/DirectoryNotFoundException。只允许根前、根后或 option 的直接子级；根外导入全部，option 内仅选目标中同规范化路径的块，完全匹配且忽略大小写，不包含另行声明的子路径。未匹配成功返回空；未选选项的 PI 不执行、不收集、不跟随或监听引用，目标 XML 仍须格式良好。文件级递归导入传递筛选条件。每个文件独立字典，成功后按位置合并、后值覆盖；同文件普通 XML 声明重复键仍拒绝，处理器写入不算普通声明。

物理路径相对于实际声明文件目录，FileStream 的实际路径不受诊断标识覆盖；无物理来源的普通流仅可导入绝对路径。虚拟来源复用同一 IFileProvider 并规范化逻辑路径，不退回磁盘。循环只针对活动链，物理身份解析目录和文件链接，重复或菱形依赖重新读取。ImportOptions.MaximumDepth 默认为 64，必须为正，根文件计一层。XmlConfigurationProvider 的私有嵌套类 FileProvider 将有效依赖接入文件基类的重载通知；监听缺失可选文件、无匹配已读文件，刷新及释放订阅，失败沿用基类清空与 OnLoadException 规则。静态读取和流提供程序不关闭调用方流，导入流由会话关闭。Core 不提供 set 或 binding 业务语义；需要最终值的消费者在构建完成后结合 Instructions 和最终配置提取。
## 通信生命周期与派发

ChannelBase 以按需创建的共享完成任务协调一次关闭和释放，不创建内核事件；派生 DisposeAsync(bool) 在关闭结束后执行，不负责启动关闭。关闭异常不回滚状态，重复调用观察原结果；关闭后不得复用实例。IWorker 仅提供 StartAsync、StopAsync、PauseAsync、ResumeAsync；WorkerBase 和 IListener<T> 仅公开 IAsyncDisposable 释放契约，所有者使用 await using 或等待 DisposeAsync。StateChanged.ActionName 仍为 Start、Stop、Pause、Resume。WorkerBase 通过短锁原子协调操作接收与释放标记，扩展点及通知均在锁外执行；启动失败调用 OnStopAsync 清理部分初始化资源，停止失败保持 Stopping，成功收尾前不允许重启。停止开始后调用者取消仅取消等待，派生停止实现负责实际完成与期限。OnStopAsync 停止运行并释放全部自有资源，支持未启动、部分初始化及失败后重试；停止成功后可重新启动。DisposeAsync 永久关闭普通生命周期入口，复用 OnStopAsync 清理，不另设释放钩子。并发释放共享当前尝试，失败后可再次 DisposeAsync 重试，成功后幂等；IsDisposed 表示清理已成功，通知失败不改变清理结果。释放开始后新的 StartAsync、StopAsync、PauseAsync、ResumeAsync 均抛 ObjectDisposedException，已接收操作继续收尾；诊断属性和 ToString 可读。回归测试见 WorkerDisposalTest。

- ChannelBase 的关闭权和释放权各只取得一次，完成结果安全发布给并发及后续调用者；关闭前已取消的令牌阻止操作，开始后的取消只结束当前等待。Closing、Closed 各通知一次，通知或扩展点故障不能跳过剩余清理。派生实现不能在关闭流程使用字段前清空它们。
- ChannelBase 的生命周期回调可以请求重入操作，但不得同步等待包含自身的操作；通知在状态确定后发出，通知故障不能恢复已失效资源或覆盖原始故障。关闭与释放只通过可等待的异步入口执行，不引入同步阻塞桥接。
- WorkerBase 的 Enabled 仅控制是否允许启动；启动后修改仅影响下一次启动，不触发停止，也不影响暂停、恢复、停止或释放，回调中可直接修改该属性。相同的在途动作共享完成结果；停止等待当前动作结束后再检查状态，启动、暂停和恢复遇到其它在途动作直接返回，不排队执行。状态与 Enabled 在接收动作时检查；启动失败清理不完整初始化，尚未释放时停止失败可再次 StopAsync 收尾。生命周期扩展点和 StateChanged不得反向等待所属工作器的生命周期或释放操作；调用方负责避免循环依赖，由回调外的所有者在当前操作结束后协调后续动作。不同工作器之间可按所有权关系依次调用。暂停保留资源与内部状态，恢复继续使用；暂停或恢复失败时实现者负责撤销部分动作，基类恢复原稳定状态。CanPauseAndContinue 为 false 时拒绝暂停和恢复。测试见 WorkerCallbackTest。下游不叠加另一套工作器状态机，不根据是否附加调试器决定异常是否传播。

`start`、`stop`、`pause` 和 `resume` 命令等待对应的异步工作器操作，并传递命令取消标记。`--timeout` 仅限制等待，默认五秒、非正数取五秒、上限三十秒；超时抛出 TimeoutException，不取消工作器操作，不再输出完成结果，后续结果由工作器负责通知。命令等待退出后继续观察任务故障，不持有命令上下文。暂停与恢复命令忽略不适用于当前状态的请求。完成扩展点接收 CommandContext、IWorker 和 TimeSpan timeout，结果处理不再等待状态；恢复结果扩展点为 WorkerResumeCommand.OnResumed。测试见 WorkerCommandTest。

ListenerBase<T> 的 T 是处理器实参，不提供 Packetizer 或 IReceiver；传输解码由实际接收者负责。注入处理器不由监听器释放，无处理器时检查取消后返回成功。HandlerBase<T> 无参数入口传递 null，显式 Parameters 保留引用身份。两种 IPacketizer 接口均只提供 Pack 与 Unpack；Pack 不 Flush，Unpack 失败不消费、成功须推进。帧长度与解码资源释放属于具体传输层，由 Net 的 PacketizerBase<T> 提供 GetLength 与 Release，不扩充 Core 接口。回归入口为 ChannelLifecycleTest、WorkerLifecycleTest 和 ListenerDispatchTest。

通信相关变更的验证应覆盖并发关闭／释放、重入、事件与扩展点抛错、独立取消等待、启动失败、停止重试及派生清理恰好一次。ChannelBase 的直接下游包括消息消费者、缓存订阅及 ZeroMQ／Redis 通道；IEventChannel 独立定义可重开的事件通道，FeatureProvider.Default 的 Providers 默认包含 Transaction.Features，并合并当前应用的 IFeatureProvider 服务；FeatureProvider 先按引用固定整个组合的来源快照，再捕获并去重特性，不依赖 Core 服务扫描；运行时条件组合 IFeature 与 Common.IPredication，TransactionContext.Feature.cs 中的私有特性在根事务的登记处理结束后、用户 Completed 通知前发布条件结果；EventDispatcher 传入事件上下文及其 Parameters，每次调度每项条件只求值一次，求值使用 CancellationToken.None，事件取消仅作用于结果等待，false 放弃事件而非跳过特性，未知运行时特性明确报错；EventDispatcher 接受单个 IFeatureProvider，默认使用 FeatureProvider.Default，显式指定时由该提供者负责全部来源；EventExchanger 内部固定投递执行器，EventDispatcher.Pipeline 仅停止时可设置，启动时固定会话管线，环境允许后才进入策略；管线在多次调用及启停间复用，由装配方管理资源；WorkerBase 包括事件交换器、事件调度器、消息守护器、诊断工作器、ZeroMQ、OPC 和 Hangfire 服务；ListenerBase 包括 MQTT 服务。修改 HandlerBase 时在全仓搜索派生类型和参数访问，核对允许 null 的调用链。先阅读下游就近约定，验证实际受影响项目，不为测试扩大生产可见性。

## 数据服务写入事件过滤器

DataServiceBase<TModel>.EventFilterBase 是公开抽象嵌套类，放在 DataServiceBase.EventFilter.cs；服务须显式挂载，构造函数以 params DataAccessMethod[] methods 指定五类写入操作；空数组抛出 ArgumentException，null 数组抛出 ArgumentNullException，非写入操作抛出 ArgumentOutOfRangeException，参数名均为 methods。校验后复制并去重到私有只读 HashSet，按访问操作种类匹配，不按 DataServiceMethod 的方法名称匹配。未启用操作完全跳过，读取操作不能启用。过滤器无每次操作的共享可变状态。

OnFilteringAsync 仅对启用操作准备数据：更新/删除调用可重写的 PrepareReturning 请求当前上下文的 Newer/Older 简单字段；批量新增/增改物化字典，导入物化模型，不修改调用方 Returning 或 Parameters。OnFilteredAsync 只处理正影响行数；更新/删除还要求当前返回行，完整主键的确定非空等值条件选择单条，其它条件选择集合。集合回调保留 IEnumerable<IDataDictionary<TModel>> 契约，不依赖数组长度或运行时类型。前四类提供两种 OnInserted/Updated/Deleted/UpsertedAsync 回调，导入仅集合；默认回调为空，业务派生类通过原事件注册表发布，不直接调用描述器 HandleAsync 绕过过滤和交换。取消、回调异常与异步完成由调用链观察。公开用法见 docs/data-service-events.zh-Hans.md，通用回归入口为 test/unit/Data/DataServiceEventFilterTest.cs。

## 数据字典序列化

DataDictionaryConverterFactory 根据声明或实现的 IDataDictionary<T> 契约选择泛型转换器，将 JSON 对象还原为 IDataDictionary<T>；非泛型 IDataDictionary 保持原有转换器。两者均保持字段缺失与显式 null 的区别，不为缺失字段创建模型默认值。事件实参采用 IDataDictionary<T> 时，Events.Marshaler.Unmarshal 及事务延期快照使用同一转换规则。回归入口为 test/unit/Serialization/DataDictionarySerializationTest.cs。

## 事件注册表扩展

EventRegistryBase 接受已有注册表时，共享根注册表的 Name、Events 和 Filters；事件描述器存入根集合后以声明它的注册表作为资源定位来源，重复登记同一实例不改变来源；Title 与 Description 在读取时按 CurrentUICulture 查找 {Name}.Title（回退 {Name}）和 {Name}.Description，非 null 显式值覆盖资源。RaiseAsync 委托根注册表执行上下文创建、过滤器、处理器和交换流程。多层扩展直接归一到根注册表。领域扩展不重复加入应用模块集合；消费者和远程接收仍从所属模块的根注册表查找事件。事件集合在启动期装配，不新增并发写入保证。扩展实例上的上下文和过滤方法重写不参与根注册表的发布流程。回归入口为 test/unit/Components/EventRegistryExtensionTest.cs。

两个构造函数均在事件和过滤器集合就绪后执行自动发现：读取具体注册表类型的公开实例非索引属性元数据，从属性声明类型（含继承成员）中读取声明类型可赋值给 EventDescriptor 的 public static readonly 字段（含泛型描述器及其派生类），通过 Event(...) 统一登记；不调用实例属性 getter、不读取事件组的静态属性、不扫描未暴露的嵌套类型。按注册表类型缓存字段元数据，每个实例仍向自己的集合登记。空描述符沿用 ArgumentNullException；同一实例重复登记忽略，不同实例的同名声明（不区分大小写）仍由集合抛出 ArgumentException，保持原有描述符和处理器。按名称创建描述符的重载也经过相同入口，因此再次创建同名描述符仍为冲突。回归入口为 test/unit/Components/EventRegistryDiscoveryTest.cs。

## 事件参数签名与处理器选择

EventDescriptor<TArgument> 继承 EventDescriptor，构造时固定声明 typeof(TArgument)，支持 IPredication<TArgument> 首参数构造；类型化 HandleAsync 直接转发基类泛型入口，不独立实现断言或处理器循环。Event<TArgument> 返回泛型描述器。EventDescriptor 在构造时复制允许的封闭参数类型，缓存 TypeAlias 签名并拒绝重复类型或签名。HandlerCollection 在 Add 时为每个声明类型绑定 IHandler<T> 委托：精确匹配优先，其次唯一最具体可赋值契约，歧义立即报错。集合按实例引用去重，锁只保护集合操作和委托快照，不在锁内执行处理器。派发调用泛型契约，不经过 HandlerBase 的对象转换入口。

RaiseAsync 以 typeof(TArgument) 选择声明；EventContext 持有描述器与类型，GetArgumentValue 由泛型上下文重写，支持非泛型派生上下文。远程接收和延期快照通过缓存的泛型桥接调用根注册表的 GetContext<TArgument>，保留覆盖点及过滤顺序。Marshaler 的 Signature 只在当前描述器允许列表中解析；消息主题、事件查询和订阅仍以名称为键。EventBinder 以描述器、目标实例及成员保存独立绑定；EventLocatorBase 实现需提供声明类型。回归入口为 GenericEventDescriptorTest、EventSignaturesTest、EventDispatcherTest、TransactionEventDispatchTest、Plugins 的 EventSignaturePluginTests 和 ZeroMQ 的 ZeroQueueEventChannelTests。
