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
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services;

public interface IApplicationBuilder<TApplication> where TApplication : IHost
{
	IServiceCollection Services { get; }
	ConfigurationManager Configuration { get; }
	IHostEnvironment Environment { get; }

	/// <summary>创建并初始化宿主；初始化失败时等待已经创建的资源释放。</summary>
	/// <returns>初始化完成的宿主。</returns>
	ValueTask<TApplication> BuildAsync();
}