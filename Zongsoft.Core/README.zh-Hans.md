# Zongsoft Core

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Core)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Core)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) | [简体中文](README.zh-Hans.md)

-----

## 概述

_**Z**ongsoft.**C**ore_ 是 [_**Z**ongsoft Framework_](https://github.com/Zongsoft/framework) 的基础类库。它提供框架其它包共同依赖的抽象接口、基类、工具 API 和基础设施组件。

当前项目目标框架为 `net8.0`、`net9.0` 和 `net10.0`，根命名空间为 `Zongsoft`，并以 `Zongsoft.Core` NuGet 包发布。项目集成了 `Microsoft.Extensions.*` 体系中的依赖注入、配置、选项、宿主抽象、对象池和内存缓存等能力。

## 安装

```powershell
dotnet add package Zongsoft.Core
```

## 包含内容

该包覆盖面较广，是 _**Z**ongsoft_ 其它上层包的公共基础层。主要能力如下：

- **通用工具** _(`Zongsoft.Common`)_
  > 类型转换、随机数、序列、断言谓词、计时器、位向量、字符串/类型/URI 扩展、时间戳、异步锁和验证接口。变量来源包括契约 [`IVariables`](docs/variables.zh-Hans.md#变量契约)、忽略大小写的内存变量字典 `Variables`、[通过 `Variables.Wrap()` / `ToVariables()` 获取的字典实时视图](docs/variables.zh-Hans.md#字典变量视图)，以及[环境变量实时视图 `Variables.Environments()`](docs/variables.zh-Hans.md#环境变量视图)。
- **集合** _(`Zongsoft.Collections`)_
  > 层次节点、分类树、参数包、同步集合、队列、对象池，以及集合和字典扩展。
- **组件模型** _(`Zongsoft.Components`)_
  > 命令基础设施、命令行解析、[命令选项变量](docs/variables.zh-Hans.md#命令选项变量)、事件交换、特性管道、重试/回退/熔断/限流/超时特性、状态机、工作者、监视器、处理器、执行器、过滤器、转换器和标识符。
- **配置** _(`Zongsoft.Configuration`)_
  > [普通设置（Settings）与连接设置（ConnectionSettings）](docs/settings.zh-Hans.md)、配置绑定和识别、[XML 配置提供程序与处理指令](docs/xml-configuration.zh-Hans.md)、模型配置、[Profile/INI 解析、指令扩展及变量视图](docs/profiles.zh-Hans.md)，以及与 `Microsoft.Extensions.Options` 的集成。
- **数据抽象** _(`Zongsoft.Data`)_
  > 查询条件、条件集合、范围、分页、排序、操作数、模型描述、数据访问/数据服务契约、操作选项和事件、元数据模型、归档契约和事务基础类型。
- **服务** _(`Zongsoft.Services`)_
  > 应用上下文和应用模块、服务注册/发现辅助类型、依赖元数据、模块化服务、服务访问器和分布式锁抽象。
- **缓存** _(`Zongsoft.Caching`)_
  > 内存缓存封装、淘汰/变更事件、缓存扫描器、分布式缓存契约，以及用于批量聚合写入的 `Spooler<T>`。
- **通信与消息** _(`Zongsoft.Communication`、`Zongsoft.Messaging`)_
  > [网络地址与协议值](docs/networking.zh-Hans.md)、通道、监听器、发送器、接收器、请求/响应、传输器、分包器、通知器、消息队列、生产者/消费者、轮询器和队列选项抽象。
- **诊断与遥测** _(`Zongsoft.Diagnostics`)_
  > 日志契约、控制台/文本/XML 日志器和格式化器、诊断配置、遥测仪表、指标描述和导出器启动契约。
- **IO 与硬件** _(`Zongsoft.IO`)_
  > 虚拟文件系统契约和本地实现、路径解析、[Searcher 本地通配搜索](docs/searcher.zh-Hans.md)、MIME 辅助方法、压缩工具、二进制/文本读取扩展和硬件档案模型。
- **安全** _(`Zongsoft.Security`)_
  > Claims 辅助方法、凭证、证书、密钥/签名契约、密码工具、认证/授权流程、用户、角色、权限和权限评估器。
- **序列化** _(`Zongsoft.Serialization`)_
  > 序列化契约、JSON 序列化辅助方法、序列化选项、命名约定、成员特性和 System.Text.Json 转换器。
- **表达式** _(`Zongsoft.Expressions`)_
  > 词法分析器/分词器基础设施、表达式求值契约和语法异常。
- **文本与模板** _(`Zongsoft.Text`、`Zongsoft.Text.Templating`)_
  > 正则文本处理、模板契约，以及[文本模板求值、成员导航、格式化和诊断](docs/expressions.zh-Hans.md)。
- **反射** _(`Zongsoft.Reflection`)_
  > 高性能反射辅助方法，以及成员表达式解析和求值。
- **运行时辅助** _(`Zongsoft.Resources`、`Zongsoft.Scheduling`、`Zongsoft.Versioning`、`Zongsoft.Terminals`)_
  > 资源定位、触发器抽象、语义化版本解析，以及终端/控制台命令执行。

## 密码哈希

[`Password`](src/Security/Password.cs) 使用 PBKDF2，默认哈希算法为 SHA256。`Factor` 的范围为 `0～255`，默认值为 `10`。因子为零时关闭额外迭代，仅执行一次 PBKDF2；正值对应 `Factor × 65,535` 次迭代。默认因子 `10` 对应 655,350 次，因子 `127` 对应 8,322,945 次。实际耗时取决于硬件、哈希算法和负载。

文本格式为 `algorithm#factor:nonce|value`，因子为零时可以省略 `#factor`。`Factor` 取代 `Exponent`；使用旧迭代映射生成的密码摘要需要重新生成。

`Password` 实现 `ISpanParsable<Password>` 和 `IParsable<Password>`，提供字符串与字符跨度的公开 `Parse`、`TryParse` 重载。文本格式不受区域设置影响，接口的格式提供者参数被忽略。空引用或空输入表示空密码；无效文本使 `TryParse` 返回 `false` 并输出空密码，`Parse` 则抛出 `FormatException`。

## 服务注册

`ServiceCollectionExtension.Register` 在整个程序集中发现 `IServiceRegistration` 实现，包括内部类和私有嵌套类。优先使用公开静态字段或属性供应的实例，否则调用公开或非公开的无参构造函数。未闭合的泛型类型，以及没有供应实例的抽象注册器不会被实例化；注册异常向调用方传播。

普通 `[Service]` 注册及选项扫描仍要求类型对外可见。非公开外层类型中的公共嵌套服务也不属于对外可见类型；因此可以隐藏注册辅助类，而不会自动公开内部服务。

## 事件调度

`EventExchanger` 通过 `IEventDispatcher` 捕获环境特性，再由内部固定的 `IExecutor<EventContext>` 执行远程投递。`FeatureProvider.Default` 默认包含 `Transaction.Features`，该共享提供者捕获事务绑定的 `IFeature` 与 `IPredication` 组合特性，独立使用时同样有效；应用可扩展其 `Providers` 集合，或注册 `IFeatureProvider` 服务追加条件。通道通过 `IEventReceiver` 接收事件；消息队列通道复用 `Events.GetTopic()` 的主题约定与 `Events.GetHandler(receiver)` 的通用接收处理器。默认投递不启用策略管线；应用插件可以通过 `EventDispatcher.Pipeline` 显式组合 Polly 策略。详见[事件调度与环境特性](docs/event-dispatching.zh-Hans.md)。

## 通信生命周期

`ChannelBase.CloseAsync` 只执行一次，并与并发调用者共享结果。进入前取消会阻止关闭，开始后取消只影响本调用方的等待；前后通知异常不能跳过传输清理。释放先等待关闭，再执行一次派生资源清理。关闭后不能重新打开通道，应创建新实例。生命周期回调可以请求关闭，但不能同步等待包含自身的操作。

`WorkerBase` 以异步操作协调启停、暂停及恢复，并支持 `IAsyncDisposable`。启动失败向上传播，并调用停止扩展点清理部分初始化的资源；停止失败保持 `Stopping`，实际清理完成前拒绝重启。通知异常不回滚已经确定的状态。同步入口阻塞等待同一异步流程。

`ListenerBase<T>` 只派发处理器实参，不解码字节，也不释放注入的共享处理器。未配置处理器时空处理并检查取消。`HandlerBase<T>` 的无附加参数调用传递 null，显式参数保持引用身份。两种 `IPacketizer` 接口仅定义 `Pack` 与 `Unpack`：编码不负责刷新，数据不足时不消费输入，解码成功后推进输入。精确帧长度及解码资源释放属于具体传输实现，由 Net 的打包器基类提供。

## 仓库结构

```text
Zongsoft.Core/
  src/        主类库源码。
  test/       覆盖核心行为的 xUnit 测试。
  samples/    MemoryCache、Spooler、Superviser 和 EventExchanger 控制台示例。
  benchmark/  针对反射、数据模型辅助类型和 Spooler 批处理的 BenchmarkDotNet 基准测试。
```

## 构建与测试

```powershell
dotnet restore Zongsoft.Core.slnx
dotnet build Zongsoft.Core.slnx -c Release
dotnet test test/Zongsoft.Core.Tests.csproj -c Release
```

仓库也提供了 Cake 脚本：

```powershell
dotnet cake build.cake --target=test --edition=Release
```

## 示例

`samples` 目录包含几个使用本包真实 API 的小型控制台程序：

- `memorycache`
  > `MemoryCache`、过期扫描、容量限制和终端输出。
- `spooler`
  > 高写入量场景下的 `Spooler<T>` 批量聚合。
- `superviser`
  > `Superviser`、`Supervisable`、工作状态报告和终端命令。
- `eventexchanger`
  > `EventExchanger` 通道和应用上下文集成。

## 许可

Zongsoft.Core 基于 [LGPL-3.0-or-later](https://github.com/Zongsoft/framework/blob/main/LICENSE) 许可证发布。
