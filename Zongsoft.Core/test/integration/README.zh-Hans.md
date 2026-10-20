# Core 终端输入集成测试

[English](README.md)

本项目通过纯托管子进程和重定向标准输入管道，验证进程共享的 `Terminal.Console.Input`。项目独立于 Core 主解决方案，不访问外部服务。

四个管道场景分别验证：父进程保持输入管道打开时，取消输入等待后子进程仍能退出；取消后后续等待者接续同一行，拒绝重叠等待；进程级单例身份、无操作的 Dispose/Close，以及明确拒绝非按行读取；Unicode、空行、末尾未换行文本和 EOF。子进程具有执行期限，由创建它的 `Process` 句柄持有。无需原生 API、Python、PTY 或临时输入文件。

另外两个场景在独立子进程中通过 Console.SetIn 注入受控 TextReader，以明确的门闩验证：反复取消等待仍只执行一次底层 ReadLine；迟到结果和故障由后续等待者领取；同步、无参异步、带取消令牌的按行读取共用状态；EOF 和故障不会触发重复读取。

在本目录执行：

```powershell
dotnet test Zongsoft.Core.IntegrationTests.csproj --framework net8.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.IntegrationTests.csproj --framework net9.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.IntegrationTests.csproj --framework net10.0 -p:GeneratePackageOnBuild=false
```

Linux 执行同样的命令，需对应 SDK 或预先构建的测试程序集与匹配 Runtime。运行已构建程序集时，设置 `DOTNET_HOST_PATH` 为所用 `dotnet` 的绝对路径，以便子进程复用该 Runtime。

这些测试不覆盖真实键盘编辑，以及宿主信号策略。
