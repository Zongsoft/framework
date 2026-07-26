# Zongsoft.Data.TDengine Data Driver Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.TDengine)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.TDengine)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**ata.**TD**engine](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/tdengine) is a low-level data engine driver in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides access to [_**TD**engine_](https://www.taosdata.com) and is transparent to upper-layer applications; deploy this plugin library to the application plugin directory to enable it.

## Unit Testing

> [!IMPORTANT]
> When running unit tests, especially over the Native protocol, ensure that the installed TDengine client matches the server version and edition (Community or Enterprise). Version mismatches can cause connection or protocol compatibility failures. For example, when testing against TDengine TSDB-OSS `3.4.2.2` on Windows x64, install the [TDengine TSDB-OSS Client 3.4.2.2 for Windows x64](https://downloads.tdengine.com/tdengine-tsdb-oss/3.4.2.2/tdengine-tsdb-oss-client-3.4.2.2-windows-x64.exe). Other platforms and versions are available from the [official download center](https://tdengine.com/downloads/).
