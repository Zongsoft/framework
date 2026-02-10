# Zongsoft.Commands 命令插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Commands)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Commands)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**C**ommands](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Commands) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的命令插件库，提供了一套开箱即用的常用命令，便于通过命令模式执行特定功能。

> 通过命令模式实现调用者与执行者的解耦，旨在提升应用程序的可扩展性与可维护性。譬如：
> ```csharp
> var phoneNumber = "+8618012345678";
> var template    = "authencode";
> 
> // 发送验证码通知短信
> CommandExecutor.Execute($"phone.send {phoneNumber} --template:{template}", { code = "1234" });
> 
> // 拨打验证码通知语音电话
> CommandExecutor.Execute($"phone.call {phoneNumber} --template:{template}", { code = "1234" });
> ```

💡 如果将本插件库部署到 [_终端宿主_](https://github.com/Zongsoft/hosting/tree/main/terminal) 程序中，即可通过命令行调用本库提供的各种命令。
