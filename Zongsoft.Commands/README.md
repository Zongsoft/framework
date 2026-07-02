# Zongsoft.Commands Command Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Commands)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Commands)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**C**ommands](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Commands) is a command plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides a set of common commands out of the box, making it easier to execute specific functionality through the command pattern.

> The command pattern decouples callers from executors and improves application extensibility and maintainability. For example:
> ```csharp
> var phoneNumber = "+8618012345678";
> var template    = "authencode";
> 
> // Send an SMS verification-code notification
> CommandExecutor.Execute($"phone.send {phoneNumber} --template:{template}", { code = "1234" });
> 
> // Make a voice call for verification-code notification
> CommandExecutor.Execute($"phone.call {phoneNumber} --template:{template}", { code = "1234" });
> ```

💡 Deploy this plugin library to the [_terminal host_](https://github.com/Zongsoft/hosting/tree/main/terminal) to call the commands provided by this library from the command line.
