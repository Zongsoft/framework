# 事件调度与环境特性

`EventExchanger` 组织远程投递和本地接收。`IEventDispatcher` 根据调用环境决定执行时机，`IExecutor<EventContext>` 执行投递，`IEventChannel` 承担传输。交换器显式实现 `IEventReceiver.ReceiveAsync()`；通道通过此接口将收到的事件交给本地注册表，不再次交换。

## 消息队列事件约定

Core 的 `Events.Channel.cs` 提供队列事件通道共享的主题与接收处理：

- `Events.GetTopic()` 返回逻辑主题根 `Events`；传入事件完整限定名则返回 `Events/完整限定名`。
- `Events.GetHandler(receiver)` 创建绑定到该接收器的独立 `IHandler<Message>`。处理器校验逻辑主题的 `Events/` 边界，通过 `Events.Marshaler` 按当前应用的事件注册信息还原实参及参数，然后调用 `IEventReceiver.ReceiveAsync()`。

空消息、非事件主题和缺少事件名的主题被忽略。处理任务覆盖接收器的异步执行，反序列化异常、接收失败和取消向调用方传播；处理器不再次交换事件，也不自动确认消息。

主题根不包含队列分组前缀或订阅通配符。适配器负责具体队列的订阅匹配方式，并在交给通用处理器前提供逻辑主题。`ZeroQueueEventChannel` 使用根主题进行前缀订阅，发送时使用完整事件主题，复用上述两个入口；筛选、队列选项和订阅生命周期由通道负责。

## 环境与策略

`IFeatureProvider.GetFeatures()` 在原调用流中捕获环境，返回绑定该环境的运行时特性。`IExecutorContext.Features` 保存集合的只读快照，各特性保留引用身份；它不是执行策略的配置来源。

运行时条件特性组合实现 `IFeature` 与 `Zongsoft.Common.IPredication`，其 `PredicateAsync()` 方法返回环境的最终决定：

| 结果 | 调度行为 |
| --- | --- |
| `true` | 允许执行 |
| `false` | 放弃事件 |
| 尚未完成 | 接收事件快照，等待决定 |

多个条件全部允许才执行，任意条件否决即放弃；同一实例按引用去重，不按特性类型合并。条件必须支持重复、并发求值，不能重新查询当前环境。调度器将事件上下文及其 `Parameters` 传入带参数集的求值重载，每次调度对每项特性只求值一次。条件可以返回共享完成任务。调度器以 `CancellationToken.None` 求值，在等待结果时独立应用事件取消令牌，从而保留共享决定，并在停止时排空条件已完成的事件。直接调用 `IPredication` 时可以取消本次求值，实现者不得将取消传播到共享环境。条件返回 `false` 表示放弃事件，不能将其解释为跳过此项特性后继续执行；无法识别的运行时特性明确报错。

`IFeature` 标识一项特性，不要求实现应用或条件求值方法；运行时条件通过组合 `IPredication` 提供求值能力。`IFeatureBuilder` 配置重试、超时和回退等执行策略，由 `Executor.Pipelines` 构建管线。调度器消费环境特性，不将它们传给管线构建器。默认 `Executor.Build(...)` 不调用管线构建器；普通执行器也不会自动查询环境或延期。

## 特性提供者

`FeatureProvider` 组合多个 `IFeatureProvider` 来源，并捕获固定的特性快照。`FeatureProvider.Default.Providers` 默认包含 `Transaction.Features` 提供的共享实例。默认组合还会在每次捕获时解析当前应用的 `IFeatureProvider` 服务。事务能力在独立使用时同样有效，不依赖应用容器、Core 程序集扫描或插件清单声明。

应用可以通过 `FeatureProvider.Default.Providers.Add(provider)` 追加共享提供者，或注册提供者服务。集合增删与枚举同步，注册方负责提供者及其资源的生命周期。从 `Providers` 移除来源会取消其显式参与；独立注册到服务容器中的来源仍然参与。

每次捕获先固定所有嵌套 `FeatureProvider` 组合的来源快照，再调用各来源的 `GetFeatures()`。提供者与返回的特性均按引用去重，保留首次出现的顺序；不同实例即使类型相同也独立参与。空来源被忽略，重复或循环的组合引用只展开一次。捕获回调不持有集合锁，回调中增删来源只影响后续捕获。返回的特性绑定到已经捕获的环境。

`EventDispatcher` 接受一个 `IFeatureProvider`，`null` 使用 `FeatureProvider.Default`。显式指定的提供者负责全部来源。普通 `new FeatureProvider()` 为空，不查询共享来源和应用服务。为某个调度器扩展默认能力时，显式组合：

```csharp
var provider = new FeatureProvider(FeatureProvider.Default, customProvider);
await using var dispatcher = new EventDispatcher(provider);
await dispatcher.StartAsync();
```

此方法体片段需要导入 `Zongsoft.Components`，其中 `customProvider` 由应用提供。独立使用 `new EventDispatcher()` 即采用默认组合。`Executor` 提供执行器构造和策略组合，不负责环境来源。

## 事务提供者

`Transaction.Features` 暴露私有提供者，由它捕获当前根事务。`TransactionContext.Feature.cs` 中的私有特性实现 `IFeature` 和 `IPredication`，订阅根事务的 `Completed` 通知，在全部登记回调结束后，将 `Committed` 映射为允许执行，将 `Aborted` 和 `Undetermined` 映射为放弃执行。事务条件不使用事件对象及附加参数，只依赖已捕获的根事务。事务终结返回前条件结果已经确定，实际投递由调度器异步管理。

同一根事务及其子事务共享一个特性实例。该特性绑定到原事务，不跟随后续调用环境变化；重复、并发等待观察相同的完成结果，取消某次等待不取消事务或其它等待。提交事务不等待远程投递。

## 接收与生命周期

`DispatchAsync` 立即执行时返回执行结果；发生延期时，返回表示事件已被调度器接收。延期事件在返回前通过 `Events.Marshaler` 快照实参及参数，执行前还原一次，再交给执行器和通道。快照失败向调用方传播。

默认容量为 1024 个未完成工作，包括正在投递的工作。达到容量时立即抛出 `InvalidOperationException`，不等待事务提交释放容量。同一环境条件下的工作按接收顺序执行，不同条件的工作可以独立就绪。环境允许后才进入已配置的策略管线；每次投递尝试均经过执行器的过滤器，并保留已捕获的环境特性及共享执行参数，使用管线传入的取消令牌。

后台投递不继承原调用的环境事务或请求服务。立即执行的异常向调用方传播；延期投递的异常由调度器观察并记录，不影响后续工作。调用方取消可终止该事件的等待和投递，执行器及通道须配合取消令牌。

`EventDispatcher` 使用 `WorkerBase` 管理生命周期。停止后不再接收工作，放弃环境条件尚未完成的事件，等待已经允许执行的工作结束。交换器随后关闭通道；重启创建独立的调度会话。停止调用者取消等待不意味着投递已经结束。执行器若不结束，停止也不会提前完成。

`EventExchanger.Dispatcher` 在交换器停止时配置；交换器内部持有固定的通道投递执行器。`EventDispatcher.Pipeline` 仅允许在调度器停止时设置，`null` 表示直接执行投递；通道集合应在启停和投递期间保持稳定。交换器协调调度器启停和通道开闭，装配方负责最终释放组件。`IEventChannel.CloseAsync()` 后可以重新打开，`DisposeAsync()` 后不能重用。ZeroMQ 通道每次打开建立订阅，关闭时释放订阅，不释放注入的共享队列。

## 可选投递策略

Polly 插件提供 `Executor.Pipelines`，具体投递策略由应用插件显式组合。以下片段使用默认 `EventDispatcher`，在 Polly 插件初始化完成、交换器及调度器停止时执行，需要导入 `Zongsoft.Components.Features`：

```csharp
var dispatcher = (EventDispatcher)EventExchanger.Instance.Dispatcher;
var features = Executor.Features
	.Retry(TimeSpan.FromMilliseconds(100), false, 2)
	.Timeout(TimeSpan.FromSeconds(3));

dispatcher.Pipeline = Executor.Pipelines.Build<EventContext>(features.Build());
```

应用插件可以通过 `object.property` 为 `EventDispatcher.Pipeline` 装配实现 `IFeaturePipeline<EventContext>` 的对象，并通过交换器的 `Dispatcher` 属性接入该调度器。策略只包围实际投递，不计入等待事务完成的时间。管线实例在多次投递及启停之间复用，以保持熔断和限流状态；必须支持并发调用，资源由装配方负责释放。每次启动固定本次会话使用的管线。未配置管线时，调度器不调用全局管线构建器；单纯加载 Polly 不改变默认投递行为。

上述片段重试整个通道投递过程，已经成功的通道可能收到重复事件。需要按通道控制重试时，可在对应通道的适配或装饰中组合策略。ZeroMQ 默认 `MostOnce` 的无匹配订阅结果不是异常，不会因此触发异常重试。当前 Polly 无返回值回退执行回调后仍保留原异常。

内存延期提供提交后的发送时机，不提供持久化发件箱、数据库与消息的原子提交或恰好一次投递。
