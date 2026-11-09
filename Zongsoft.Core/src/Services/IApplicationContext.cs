/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Security.Claims;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Services;

/// <summary>表示应用程序上下文的接口。</summary>
public interface IApplicationContext : IApplicationModule
{
	#region 事件定义
	event EventHandler Started;
	event EventHandler Stopped;
	event EventHandler Stopping;
	#endregion

	#region 属性定义
	/// <summary>获取当前应用程序的类型。</summary>
	string ApplicationType { get; }

	/// <summary>获取当前应用程序根目录的完整路径。</summary>
	string ApplicationPath { get; }

	/// <summary>获取当前应用程序的应用配置。</summary>
	IConfigurationRoot Configuration { get; }

	/// <summary>获取当前应用程序的环境信息。</summary>
	IApplicationEnvironment Environment { get; }

	/// <summary>获取当前应用程序的安全主体。</summary>
	/// <remarks>该属性始终不会返回空(<c>null</c>)。</remarks>
	ClaimsPrincipal Principal { get; }

	/// <summary>获取当前应用程序的会话数据集。</summary>
	IDictionary<string, object> Session { get; }

	/// <summary>获取当前应用程序的模块集。</summary>
	ApplicationModuleCollection Modules { get; }

	/// <summary>获取当前应用程序的初始化器集。</summary>
	ICollection<IApplicationInitializer> Initializers { get; }

	/// <summary>获取当前应用程序的后台工作者集。</summary>
	ICollection<Components.IWorker> Workers { get; }
	#endregion

	#region 方法定义
	/// <summary>请求宿主停止；本方法不等待资源收尾，不直接退出进程。</summary>
	void Exit();

	/// <summary>设置进程退出代码并请求宿主停止；退出由宿主入口完成。</summary>
	/// <param name="exitCode">进程退出代码。</param>
	void Exit(int exitCode);
	#endregion
}
