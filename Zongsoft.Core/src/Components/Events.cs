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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Services;

namespace Zongsoft.Components
{
	public static class Events
	{
		#region 私有变量
		private static readonly ConcurrentDictionary<IApplicationModule, Func<IApplicationModule, EventRegistryBase>> _accessors = new();
		#endregion

		#region 公共方法
		public static EventDescriptor GetEvent(string qualifiedName) => GetEvent(qualifiedName, out _);
		public static EventDescriptor GetEvent(string qualifiedName, out EventRegistryBase registry)
		{
			if(string.IsNullOrEmpty(qualifiedName))
			{
				registry = null;
				return null;
			}

			(var moduleName, var eventName) = Parse(qualifiedName);

			if(ApplicationContext.Current.Modules.TryGet(moduleName, out var applicationModule))
			{
				registry = GetEventRegistry(applicationModule);

				if(registry != null)
					return registry.Events.TryGetValue(eventName, out var descriptor) ? descriptor : null;
			}

			registry = null;
			return null;

			static (string @namespace, string name) Parse(string text)
			{
				var position = text.LastIndexOf(':');

				return position switch
				{
					0 => (string.Empty, text[1..]),
					< 0 => (string.Empty, text),
					> 0 => position < text.Length - 1 ?
						(text[..position], text[(position + 1)..]) :
						(text[..position], string.Empty),
				};
			}
		}

		public static IEnumerable<EventDescriptor> GetEvents()
		{
			if(ApplicationContext.Current.Modules.TryGet(string.Empty, out var common))
			{
				var registry = GetEventRegistry(common);

				if(registry != null)
				{
					foreach(var descriptor in registry.Events)
						yield return descriptor;
				}
			}

			foreach(var module in ApplicationContext.Current.Modules)
			{
				if(string.IsNullOrEmpty(module.Name))
					continue;

				var registry = GetEventRegistry(module);

				if(registry != null)
				{
					foreach(var descriptor in registry.Events)
						yield return descriptor;
				}
			}
		}
		#endregion

		#region 私有方法
		private static EventRegistryBase GetEventRegistry(IApplicationModule module) =>
			_accessors.GetOrAdd(module, CreateAccessor)?.Invoke(module);

		/*
		 * 生成获取 IApplicationModule<TEvents> 应用模块 Events 属性的调用委托，该委托代码如下所示：
		 * EventRegistryBase Invoke(IApplicationModule module)
		 * {
		 *		return ((IApplicationModule<TEvents>)module).Events;
		 * }
		 */
		private static Func<IApplicationModule, EventRegistryBase> CreateAccessor(IApplicationModule module)
		{
			var type = module.GetType();

			while(type != null && type != typeof(object))
			{
				if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApplicationModule<>))
				{
					var property = type.GetProperty(nameof(UselessModule.Events), BindingFlags.Instance | BindingFlags.Public);
					var parameter = Expression.Parameter(typeof(IApplicationModule));
					var converter = Expression.Convert(parameter, type);
					var invoke = Expression.Call(converter, property.GetMethod);
					return Expression.Lambda<Func<IApplicationModule, EventRegistryBase>>(invoke, parameter).Compile();
				}

				type = type.BaseType;
			}

			return null;
		}

		/// 注意：该类只是为了方便上面方法获取 <see cref="ApplicationModule{TEvents}"/> 泛型类中的 <see cref="ApplicationModule{TEvents}.Events"/> 属性名之用，并无其他意义！
		private sealed class UselessModule : ApplicationModule<UselessModule.EventRegistry>
		{
			public UselessModule() : base(nameof(UselessModule)) { }
			public sealed class EventRegistry : EventRegistryBase
			{
				public EventRegistry() : base(nameof(UselessModule)) { }
			}
		}
		#endregion
	}
}