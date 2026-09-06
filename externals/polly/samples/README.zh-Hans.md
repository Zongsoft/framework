# Zongsoft.Externals.Polly 扩展插件库范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 用途与启动

这个 .NET 10 交互程序围绕模拟操作装配韧性策略，不依赖数据库或远程服务。[Program.cs](Program.cs) 注册命令，在 `execute` 时构建管线，并模拟延迟或失败。应用侧的插件装配方式见[类库说明](../README.zh-Hans.md)。

在仓库根目录先构建 Core，因为范例的 Debug 配置直接引用其输出 DLL，然后在交互终端中启动范例：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -f net10.0 -p:GeneratePackageOnBuild=false
dotnet run --project externals/polly/samples/Zongsoft.Externals.Polly.Samples.csproj -c Debug
```

下面的命令在范例提示符中输入，不是在操作系统终端中执行。`info` 显示当前策略，`reset` 清空策略。范例会捕获模拟异常并输出 `Caught:`；当最终失败未被后备策略处理时，这是预期现象。

💡 并发分支使用带异步回调的 `Parallel.For`，全部操作结束前提示符可能已经返回。事件顺序和次数受调度影响，毫秒级窗口尤其明显；这些场景用于观察行为，不是确定性断言或性能基准。等待输出结束后再重置管线。部分拒绝处理器还会调用 `Console.Beep`，其可用性依赖平台。

💡 **注：** 回退 _(**F**allback)_ 策略应位于最外层（_最先添加到执行管线中_）。

## 重试

依次执行下列命令：

> - `reset | retry`
> - `exec --throw`

可观察到触发了默认的 `3` 次 `OnRetry` 重试。

## 超时

依次执行下列命令：

> - `reset | timeout 10ms`
> - `exec --delay:100ms`

可观察到触发了 `1` 次 `OnTimeout` 超时。

## 熔断

依次执行下列命令：

> - `reset | breaker`
> - `exec --round:100 --concurrency --throw`

失败达到配置的熔断阈值时可观察到 `Opened` 事件，次数受调度影响。

## 回退

依次执行下列命令：

> - `reset | fallback`
> - `exec --throw`

可观察到触发了 `1` 次 `OnFallback` 回退。

### 超时并回退

依次执行下列命令：

> - `reset | fallback | timeout 10ms`
> - `exec --delay:100ms`

可观察到依次触发了 `1` 次 `OnTimeout` 超时 _和_ `OnFallback` 回退。

## 限速限流

> 💡 提示：如果不希望触发 `ThrottleException` 异常，可启用 `throttle` 命令的 `--handled` 选项。

### 并发限制

依次执行下列命令：

> - `reset | throttle --handled`
> - `exec --round:5 --concurrency`

并发操作超过配置容量时可观察到 `OnRejected` 事件，次数并不固定。

### 令牌桶限制

依次执行下列命令：

> - `reset | throttle token --permit:1 --queue:0 --value:1 --period:1ms --handled`
> - `exec --round:5 --concurrency`

没有可用令牌时可观察到 `OnRejected` 事件，次数取决于令牌补充时机与调度。

### 固定窗口限制

依次执行下列命令：

> - `reset | throttle fixed --permit:1 --queue:0 --window:1ms --handled`
> - `exec --round:5 --delay:1ms --concurrency`

当前窗口容量耗尽时可观察到 `OnRejected` 事件，次数并不固定。

### 滑动窗口限制

依次执行下列命令：

> - `reset | throttle sliding --permit:1 --queue:0 --window:1ms --segments:1 --handled`
> - `exec --round:5 --delay:1ms --concurrency`

滑动窗口容量耗尽时可观察到 `OnRejected` 事件，次数并不固定。

## 退出与清理

等待待处理输出结束后，执行 `exit` 并确认终端退出提示。策略和模拟状态仅保存在进程内，不创建数据库记录或应用数据文件。停止进程并不能证明真实应用中的在途工作已经排空。
