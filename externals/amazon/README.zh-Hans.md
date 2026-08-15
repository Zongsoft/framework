# Zongsoft.Externals.Amazon 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Amazon)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Amazon)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**A**mazon](https://github.com/Zongsoft/framework/tree/main/externals/amazon) 将 Amazon Web Services 集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架中。当前程序包主要面向 Amazon S3，并通过 Zongsoft 文件系统抽象提供对象存储访问能力。

## S3 文件系统

插件在 `/Workbench/FileSystem` 下注册 `S3FileSystem`，支持对 S3 存储桶执行文件和目录操作，通过 `amazon.s3` 连接设置驱动器读取参数，并使用 `zfs.s3` 方案标识资源。AWS 区域、服务端点、访问密钥和密钥可在随包选项文件或宿主配置中设置。

加载 `Zongsoft.Externals.Amazon.plugin`，并配置 `/Externals/Amazon/ConnectionSettings` 后即可解析该文件系统。指定自定义服务器地址时，驱动器也可连接兼容 S3 的服务；文件系统用法可参考[测试项目](test)。
