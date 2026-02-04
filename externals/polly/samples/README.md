## 限速限流

### 并发限制

依次执行下列命令：

> - `reset | throttle --handled`
> - `exec --round:5 --concurrency`

可观察到大约触发了 `3` 至 `4` 次 `OnRejected` 事件。

### 令牌桶限制

依次执行下列命令：

> - `reset | throttle token --permit:1 --queue:0 --threshold:1 --period:1ms --handled`
> - `exec --round:5 --concurrency`

可观察到大约触发了 `3` 至 `4` 次 `OnRejected` 事件。

### 固定窗口限制

依次执行下列命令：

> - `reset | throttle fixed --permit:1 --queue:0 --window:1ms --handled`
> - `exec --round:5 --delay:1ms --concurrency`

可观察到触发了 `4` 次 `OnRejected` 事件。

### 滑动窗口限制

依次执行下列命令：

> - `reset | throttle sliding --permit:1 --queue:0 --window:1ms --windowSize:1 --handled`
> - `exec --round:5 --delay:1ms --concurrency`

可观察到触发了 `4` 次 `OnRejected` 事件。
