/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Zongsoft.Web
{
	[Services.Service(typeof(IControllerActivator))]
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
	}
}
