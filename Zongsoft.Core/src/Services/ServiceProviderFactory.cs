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

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services;

public sealed class ServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
	#region 私有变量
	private readonly ServiceProviderOptions _options;
	#endregion

	#region 构造函数
	public ServiceProviderFactory(ServiceProviderOptions options = null)
	{
		_options = options ?? new ServiceProviderOptions();
	}
	#endregion

	#region 公共方法
	public IServiceCollection CreateBuilder(IServiceCollection services) => services;
	public System.IServiceProvider CreateServiceProvider(IServiceCollection services) => new ServiceProvider(services, _options);
	#endregion
}
