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

namespace Zongsoft.Services;

public interface IModularService
{
	object GetValue(IServiceProvider provider);
}

public abstract class ModularServiceBase<TContract> : IModularService where TContract : class
{
	private readonly object _service;
	private readonly Type _serviceType;

	protected ModularServiceBase(Type serviceType) => _serviceType = serviceType;
	protected ModularServiceBase(object service)
	{
		_service = service;
		_serviceType = service?.GetType();
	}

	public object GetValue(IServiceProvider provider) => _service ?? (_serviceType == null ? null : provider.GetService(_serviceType));
}