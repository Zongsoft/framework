# Zongsoft.Externals.Wechat.Web 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Wechat.Web` 将 [Zongsoft 微信集成](..)中的服务公开为 ASP.NET Core Web API，适用于需要以统一 HTTP 接口访问微信小程序、公众号、支付和证书能力的 Zongsoft Web 宿主。

## API 范围

- 小程序登录、手机号获取、用户查询及访问凭证刷新；
- 公众号凭证、用户、消息模板、身份验证及 Postmark 操作；
- 银行和支行查询，包括银行卡识别；
- 平台证书获取及临时媒体文件上传。

请同时加载 `Zongsoft.Externals.Wechat.Web.plugin` 和 `Zongsoft.Externals.Wechat`。这些控制器使用 `Externals/Wechat` 区域，并从核心插件解析已配置的小程序、公众号、支付机构及相关服务；对公网仅应开放宿主实际需要的端点。
