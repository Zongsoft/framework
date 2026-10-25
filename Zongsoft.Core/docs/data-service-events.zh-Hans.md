# 数据服务写入事件

[English](data-service-events.md)

`DataServiceBase<TModel>.EventFilterBase` 是公开的抽象嵌套类，实现 `IDataServiceFilter<TModel>`。它为五类写入操作准备事件数据，并等待派生类的发布回调完成。服务通过 `Filters.Add(...)` 显式启用；构造服务不会自动安装它。读取、执行命令、存在性判断和聚合不属于此过滤器的处理范围。

## 接入

构造函数为 `EventFilterBase(params DataAccessMethod[] methods)`，至少指定一个操作，允许 `Insert`、`Update`、`Delete`、`Upsert`、`Import`。省略参数或传入空数组抛出 `ArgumentException`，null 数组抛出 `ArgumentNullException`，其它操作值抛出 `ArgumentOutOfRangeException`；参数名均为 `methods`。重复值只启用一次。构造时复制操作集合；未启用的操作不准备 Returning、不物化输入，也不调用发布回调。

操作按 `DataAccessMethod` 种类启用，因此 `Insert` 同时覆盖单条和批量新增，`Upsert` 同时覆盖单条和批量增改。过滤器不按 `DataServiceMethod.Name` 区分服务方法；回调仍可读取 `context.Method`。

以下服务仅暴露新增事件。`Item` 与 `ItemEvents` 由业务模块提供；`ItemEvents.OnCreatedAsync` 的两个重载应使用同一个描述器，分别声明 `IDataDictionary<Item>` 和 `IEnumerable<IDataDictionary<Item>>`，并通过注册表的 `RaiseAsync` 发布。示例需要导入 `System`、`System.Threading`、`System.Threading.Tasks`、`System.Collections.Generic` 和 `Zongsoft.Data`。

```csharp
public sealed class ItemService : DataServiceBase<Item>
{
	public ItemService(IServiceProvider services, ItemEvents events) : base(services)
	{
		this.Filters.Add(new EventFilter(events));
	}

	private sealed class EventFilter(ItemEvents events) : EventFilterBase(DataAccessMethod.Insert)
	{
		protected override async ValueTask OnInsertedAsync(IDataServiceContext<Item> context,
			IDataDictionary<Item> argument, CancellationToken cancellation) =>
			await events.OnCreatedAsync(argument, cancellation);

		protected override async ValueTask OnInsertedAsync(IDataServiceContext<Item> context,
			IEnumerable<IDataDictionary<Item>> argument, CancellationToken cancellation) =>
			await events.OnCreatedAsync(argument, cancellation);
	}
}
```

事件名称、描述器、资源、插件路径及发布参数由业务模块定义。回调收到本次 `IDataServiceContext<TModel>`，需要附加参数时由回调显式决定。基类不将服务选项参数自动复制为事件参数。发布继续使用现有事件注册表，以保留过滤器、本地处理、事务延期和远程投递规则；详见[事件调度与参数契约](event-dispatching.zh-Hans.md)。

## 回调与参数

| 操作 | 发布回调 | 数据来源与形态 |
| --- | --- | --- |
| 新增 | `OnInsertedAsync` | 输入及写入时回填的数据；按 `IsMultiple` 选择单条或集合 |
| 更新 | `OnUpdatedAsync` | 当前操作 Returning 的 `Newer`；按完整主键条件选择单条或集合 |
| 删除 | `OnDeletedAsync` | 当前操作 Returning 的 `Older`；按完整主键条件选择单条或集合 |
| 增改 | `OnUpsertedAsync` | 输入及写入时回填的数据；按 `IsMultiple` 选择单条或集合 |
| 导入 | `OnImportedAsync` | 导入输入及回填的数据，始终为集合 |

前四种回调分别提供单条 `IDataDictionary<TModel>` 与集合 `IEnumerable<IDataDictionary<TModel>>` 重载，导入只有集合重载。集合实参已物化为数组。默认回调为空操作，派生类应重写所启用操作所需的重载。

更新和删除仅在条件包含所有主键字段的确定非空等值常量时选择单条。无主键、部分主键、IN、OR、范围、空值或冲突常量等无法证明唯一的条件使用集合；返回一行不改变集合契约。单条契约要求本次返回恰好一行。`UpdateManyAsync` 逐项执行更新，每个内部操作分别通知，不聚合整个循环。

只有影响行数大于零才调用发布回调；更新、删除还要求当前 Returning 有返回行。零行、执行失败或准备失败不发布；更新、删除没有返回数据时不以输入补造结果，也不额外查询。启用操作的准备与发布入口检查取消，发布任务、异常和取消沿原调用链传播。后置回调失败本身不会撤销已经提交的数据库操作。

## Returning 与输入生命周期

更新、删除在执行前调用 `PrepareReturning`，默认追加主实体所有已映射简单字段，不请求关联对象。派生类可重写此方法调整请求列；它必须使用操作上下文的 `Returning`。回调接收该操作同种类的返回数据，具体发布字段由业务回调决定。引擎和驱动须支持并验证请求，不满足条件时操作失败；过滤器不绕过驱动限制。

过滤器不替换调用方 `Options.Returning`，不修改其列定义、顺序、种类或已有结果；引擎仍按调用方投影追加本次返回行。事件只使用上下文的当前返回行，过滤器不改写调用方 `Parameters`。

批量新增、增改在执行前将输入物化为数据字典数组；导入物化为 `TModel[]`。支持同步与异步输入，输入只消费一次并完成枚举器释放，再将数组交给写入过程。这会占用与批次大小成比例的内存，调用方应控制批次大小。新增、增改和导入不保证包含数据库默认值或触发器生成的全部字段；批量总影响行数也不能逐条证明每条输入已写入。

过滤器不保存每次操作的可变状态，可用于重叠操作；派生类须自行保证回调状态的并发安全。数据字典保留字段缺失与显式 null 的区别，消费者按实际提供的字段处理。
