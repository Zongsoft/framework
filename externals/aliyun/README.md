# Zongsoft.Externals.Aliyun Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**A**liyun](https://github.com/Zongsoft/framework/tree/main/externals/aliyun) integrates selected [Alibaba Cloud](https://www.alibabacloud.com/) services with the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework.

## Features

- Exposes OSS buckets through the Zongsoft file-system abstraction and the `zfs.oss` scheme.
- Provides queue and topic access for Alibaba Cloud Message Service.
- Supplies an MQTT connection-setting driver for the framework's messaging services.
- Supports SMS and voice calls through the telecom transmitter and commands.
- Supports mobile push notifications and the corresponding command integration.

Load `Zongsoft.Externals.Aliyun.plugin` and configure `/Externals/Aliyun` with the required service center, certificate, bucket, messaging, telecom, or push application settings. The packaged [option file](src/Zongsoft.Externals.Aliyun.option) documents the configuration hierarchy and placeholder values.
