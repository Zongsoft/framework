# Zongsoft.Externals.Amazon Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Amazon)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Amazon)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**A**mazon](https://github.com/Zongsoft/framework/tree/main/externals/amazon) integrates Amazon Web Services with the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. The current package focuses on Amazon S3 and exposes object storage through Zongsoft's file-system abstraction.

## S3 File System

The plugin registers `S3FileSystem` under `/Workbench/FileSystem`. It supports file and directory operations on S3 buckets, uses the `amazon.s3` connection-setting driver, and addresses resources with the `zfs.s3` scheme. AWS regions, service endpoints, access keys, and secret keys are supplied through the packaged option file or the host configuration.

Load `Zongsoft.Externals.Amazon.plugin` and configure `/Externals/Amazon/ConnectionSettings` before resolving the file system. The driver also accepts S3-compatible endpoints when a custom server address is configured. See the [tests](test) for file-system examples.
