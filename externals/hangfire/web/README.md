# Zongsoft.Externals.Hangfire.Web Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Hangfire.Web` integrates the [Hangfire Dashboard](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html) with a Zongsoft ASP.NET Core host.

Loading `Zongsoft.Externals.Hangfire.Web.plugin` registers Hangfire services and calls `UseHangfireDashboard()` during application initialization. The default Hangfire Dashboard route is therefore available to the host after a `JobStorage` has been configured.

This package only supplies the web-host integration. Use the [core Hangfire plugin](..) for scheduling and background processing, and add an appropriate storage package such as the [Redis adapter](../storages/redis). Configure Dashboard authorization and routing according to the hosting application's security requirements; do not expose it publicly without access controls.
