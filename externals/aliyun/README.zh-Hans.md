# Zongsoft.Externals.Aliyun 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**A**liyun](https://github.com/Zongsoft/framework/tree/main/externals/aliyun) 将部分[阿里云](https://www.aliyun.com/)服务集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架中。

## 主要功能

- 通过 Zongsoft 文件系统抽象和 `zfs.oss` 方案访问 OSS 存储桶；
- 提供阿里云消息服务的队列与主题访问能力；
- 为框架消息服务提供 MQTT 连接设置驱动器；
- 通过通信发送器和命令支持短信及语音呼叫；
- 支持移动推送及相应的命令集成。

加载 `Zongsoft.Externals.Aliyun.plugin`，并在 `/Externals/Aliyun` 下配置所需的服务中心、凭证、存储桶、消息、通信或推送应用参数。随包提供的[选项文件](src/Zongsoft.Externals.Aliyun.option)展示了配置层次及各项占位值。
