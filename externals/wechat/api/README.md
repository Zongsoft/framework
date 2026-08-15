# Zongsoft.Externals.Wechat.Web Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Wechat.Web` exposes the services of the [Zongsoft WeChat integration](..) as ASP.NET Core Web APIs. It is intended for Zongsoft web hosts that need a consistent HTTP surface for WeChat applets, official-account channels, payments, and certificates.

## API Areas

- Applet login, phone-number retrieval, user lookup, and access-credential refresh.
- Official-account credential, user, message-template, authentication, and postmark operations.
- Bank and branch lookup, including bank-card identification.
- Platform certificate retrieval and temporary media-file upload.

Load `Zongsoft.Externals.Wechat.Web.plugin` together with `Zongsoft.Externals.Wechat`. The controllers use the `Externals/Wechat` area and resolve configured applets, channels, payment authorities, and related services from the core plugin; only the endpoints required by the host should be exposed publicly.
