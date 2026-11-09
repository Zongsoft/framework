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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Zongsoft.Web;

public class ControllerActivator : IControllerActivator
{
	private static readonly ConcurrentDictionary<Type, ObjectFactory> _activators = new ConcurrentDictionary<Type, ObjectFactory>();

	public object Create(ControllerContext context)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		var creator = _activators.GetOrAdd(context.ActionDescriptor.ControllerTypeInfo.AsType(), type =>
		{
			if(Services.ServiceInjector.IsInjectable(type))
				return (services, _) => Services.ServiceInjector.Inject(services, type);
			else
				return ActivatorUtilities.CreateFactory(type, Array.Empty<Type>());
		});

		return creator(context.HttpContext.RequestServices, null);
	}

	public void Release(ControllerContext context, object controller)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));
		if(controller == null)
			throw new ArgumentNullException(nameof(controller));

		if(controller is IDisposable disposable)
			disposable.Dispose();
	}

	public ValueTask ReleaseAsync(ControllerContext context, object controller)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));
		if(controller == null)
			throw new ArgumentNullException(nameof(controller));

		if(controller is IAsyncDisposable disposable)
			return disposable.DisposeAsync();

		this.Release(context, controller);
		return default;
	}
}
