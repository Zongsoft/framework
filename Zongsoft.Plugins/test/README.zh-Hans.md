# 插件测试

[English](README.md) | [简体中文](README.zh-Hans.md)

测试通过公开 API 验证插件装配和异步资源所有权。两个项目均通过 `ProjectReference` 引用 Plugins 源码项目，通过 `HintPath` 引用已构建的 Core 程序集，并显式声明所需 Core 运行时包。测试不扩大生产代码作用域，也不依赖框架本地化文案。

## 项目划分

- [单元测试](unit/Zongsoft.Plugins.Tests.csproj)：清单加载与 Schema、插件树、构建器、解析器、配置、命令、工作台组件、初始器所有权和插件异步释放。单元测试不创建 Generic Host、子进程或网络监听器。
- [集成测试](integration/Zongsoft.Plugins.Tests.csproj)：Generic Host 配置、Daemon/Terminal 构建、服务扫描、生命周期顺序、工作器启动回滚、异常汇总、取消与初始化失败清理。程序集名称为 `Zongsoft.Plugins.IntegrationTests`。
- Plugins 主解决方案和 Cake `test` 任务只包含单元测试；[集成测试解决方案](integration/Zongsoft.Plugins.Tests.slnx) 与 Cake `integration-test` 任务用于显式运行集成测试。

两个测试项目复用产品交付的 [Main.plugin](../plugins/Main.plugin) 注册。每个夹具拥有独立临时目录。单元测试等待应用上下文和服务容器异步释放后再删除文件；插件树通过 `DisposeAsync` 释放并移除缓存。

## 运行方式

从框架根目录运行，需要 .NET 10 SDK 和 .NET 8、9、10 Runtime。Debug 构建使用本地依赖程序集，因此先构建 Core。测试平台为 VSTest，框架为 xUnit v3。

```powershell
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins/Zongsoft.Plugins.slnx -p:GeneratePackageOnBuild=false
dotnet test ./Zongsoft.Plugins/test/unit/Zongsoft.Plugins.Tests.csproj --no-build --no-restore
```

按需显式运行集成测试：

```powershell
dotnet test ./Zongsoft.Plugins/test/integration/Zongsoft.Plugins.Tests.slnx -p:GeneratePackageOnBuild=false
```

添加 `-f net8.0`、`-f net9.0` 或 `-f net10.0` 可选择单个目标框架。宿主生命周期用例通过 PATH 中的 `dotnet` 运行当前目标框架的测试程序集。基础生命周期用例的终端执行器使用内存替身；信号用例使用真实控制台执行器和标准输入读取器：Windows 创建独立隐藏控制台，Linux 保持重定向管道打开。不启动监听器或外部服务。超时仅结束测试自己的子进程；完成标记验证停止操作确实返回调用方。

## 生命周期断言

[AsyncPluginDisposalTests](unit/AsyncPluginDisposalTests.cs) 验证仅异步释放和双接口释放、并发销毁、拒绝销毁回调重入、嵌套集合和子插件、异常汇总、取消等待、插件树缓存释放以及借用引用。通过释放次数和实例身份区分插件所有权与共享使用。

[ApplicationLifecycleTests](unit/ApplicationLifecycleTests.cs) 验证工作台状态转换及初始器所有权。[HostWorkerLifecycleTests](integration/HostWorkerLifecycleTests.cs) 使用显式门闩验证启动等待全部工作器、回滚等待全部启动尝试、取消不遗弃清理。[TerminalLifecycleTests](integration/TerminalLifecycleTests.cs) 验证异步输入循环的生命周期。超时只防止挂起，不承担事件顺序判断。

[TerminalSignalTests](integration/TerminalSignalTests.cs) 在隔离子进程中发送真实操作系统信号。空闲提示符收到 SIGINT 后取消调用方的输入等待并停止宿主，无须等待进程级 Console.In.ReadLine 操作结束。已处理的 SIGINT 保持标准输入可用，或使 ReactiveAsync 返回并继续执行下一条命令。SIGQUIT（Windows 的 Ctrl+Break）及 Linux SIGTERM 不受响应模式否决；清理门闩验证重复信号不能跳过异步停止。阻塞中断通知的用例验证公开宿主停止会等待通知返回，并保留通知故障；Linux 另验证真实 SIGQUIT 与 SIGINT 通知交叠。其他用例验证中断订阅者异常、解除信号注册和保留显式 IHostLifetime 注册。支持 Windows 和 Linux 环境；Windows 需要创建控制台的能力，Linux 需要标准 POSIX 信号。测试持有自身子进程句柄进行限时清理，不按保存的 PID 重新定位进程。

应用上下文、构建管理器和插件树缓存包含进程级状态，因此测试程序集禁用用例并行；并发验证由具体用例显式发起。

## 覆盖率与格式检查

```powershell
dotnet test ./Zongsoft.Plugins/test/unit/Zongsoft.Plugins.Tests.csproj -f net10.0 --no-build --no-restore --collect "XPlat Code Coverage" --settings ./Zongsoft.Plugins/test/coverage.runsettings --results-directory ./Zongsoft.Plugins/test/TestResults
git -c core.whitespace=cr-at-eol diff --check -- Zongsoft.Plugins
```

[coverage.runsettings](coverage.runsettings) 仅统计 Plugins 程序集。覆盖率报告属于可删除的中间文件，放在 Git 忽略的 `TestResults` 中。代码遵循[框架要求](../../AGENTS.md)和 [C# 开发规范](../../../guidelines/zongsoft.csharp.guidelines.md)。
