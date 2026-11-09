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

[Obsolete($"Use the '{nameof(ServiceDependencyAttribute.ServiceName)}' property of '{nameof(ServiceDependencyAttribute)}' annotation class instead.")]
public class ServiceAccessor<T> : IServiceAccessor<T> where T : class
{
	#region 构造函数
	public ServiceAccessor(T value) => this.Value = value;
	public ServiceAccessor(IApplicationModule module, string name)
	{
		//注意：以下代码的处理机制必须遵循服务注入注解类中ServiceName属性的规范！
		if(module == null || module is IApplicationContext)
			this.Value = GetValue(name ?? string.Empty, ApplicationContext.Current.Services);
		else
			this.Value = GetValue(name ?? module.Name, module.Services);

		static T GetValue(string name, IServiceProvider serviceProvider)
		{
			if(serviceProvider == null)
				return default;

			var provider = serviceProvider.GetService<IServiceProvider<T>>();

			if(provider == null)
				return serviceProvider.GetService<T>();

			if(string.IsNullOrEmpty(name))
				return provider.GetService(string.Empty) ?? serviceProvider.GetService<T>();
			else
				return provider.GetService(name) ?? provider.GetService(string.Empty) ?? serviceProvider.GetService<T>();
		}
	}
	#endregion

	#region 公共属性
	public T Value { get; }
	#endregion
}
