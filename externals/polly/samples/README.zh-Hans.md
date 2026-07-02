# Zongsoft.Externals.Polly 扩展插件库范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

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

可观察到触发了 `1` 至 `3` 次 `Opened` 熔断。

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

可观察到大约触发了 `3` 至 `4` 次 `OnRejected` 事件。

### 令牌桶限制

依次执行下列命令：

> - `reset | throttle token --permit:1 --queue:0 --value:1 --period:1ms --handled`
> - `exec --round:5 --concurrency`

可观察到大约触发了 `3` 至 `4` 次 `OnRejected` 事件。

### 固定窗口限制

依次执行下列命令：

> - `reset | throttle fixed --permit:1 --queue:0 --window:1ms --handled`
> - `exec --round:5 --delay:1ms --concurrency`

可观察到触发了 `4` 次 `OnRejected` 事件。

### 滑动窗口限制

依次执行下列命令：

> - `reset | throttle sliding --permit:1 --queue:0 --window:1ms --segments:1 --handled`
> - `exec --round:5 --delay:1ms --concurrency`

可观察到触发了 `4` 次 `OnRejected` 事件。
