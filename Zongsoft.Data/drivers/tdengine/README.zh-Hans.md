# Zongsoft.Data.TDengine 数据驱动插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.TDengine)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.TDengine)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**ata.**TD**engine](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/tdengine) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的数据引擎的底层驱动，提供了 [_**TD**engine_](https://www.taosdata.com) 时序库访问的相关功能，对上层应用透明，只需将该插件库部署到应用程序的插件目录中即可。

## 单元测试

> [!IMPORTANT]
> 运行单元测试时，尤其是使用 Native 协议时，请确保本机安装的 TDengine 客户端与服务器的版本号及版本类型（社区版或企业版）保持一致，否则可能出现连接失败或协议不兼容等问题。例如，在 Windows x64 上测试 TDengine TSDB-OSS `3.4.2.2` 时，请安装 [TDengine TSDB-OSS 3.4.2.2 Windows x64 客户端](https://downloads.tdengine.com/tdengine-tsdb-oss/3.4.2.2/tdengine-tsdb-oss-client-3.4.2.2-windows-x64.exe)。其他平台或版本的客户端可前往 [TDengine 官方下载中心](https://tdengine.com/downloads/) 获取。
