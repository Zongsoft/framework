# Core 终端集成测试

[English](README.md)

本项目通过纯托管子进程和重定向标准输入管道，验证进程共享的 `Terminal.Console.Input`。项目独立于 Core 主解决方案，不访问外部服务。

测试覆盖按行输入的取消、结果与故障保留、并发读取拒绝、EOF 和进程退出。Windows 另外覆盖 Shell 命令的输出、退出码、超时与取消。测试不依赖外部服务。

在本目录执行：

```powershell
dotnet test Zongsoft.Core.Tests.csproj --framework net8.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.Tests.csproj --framework net9.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.Tests.csproj --framework net10.0 -p:GeneratePackageOnBuild=false
```

Linux 执行同样的命令，需对应 SDK 或预先构建的测试程序集与匹配 Runtime。运行已构建程序集时，设置 `DOTNET_HOST_PATH` 为所用 `dotnet` 的绝对路径，以便子进程复用该 Runtime。

这些测试不覆盖真实键盘编辑，以及宿主信号策略。
