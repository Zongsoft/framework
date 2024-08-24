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
		public static int GetCount()
		{
			var context = ApplicationContext.Current;
			if(context == null || context.Modules.Count == 0)
				return 0;

			int count = 0;

			foreach(var module in context.Modules)
			{
				var registry = GetEventRegistry(module);
				count += registry == null ? 0 : registry.Events.Count;
			}

			return count;
		}

		public static EventDescriptor GetEvent(string qualifiedName) => GetEvent(qualifiedName, out _);
		public static EventDescriptor GetEvent(string qualifiedName, out EventRegistryBase registry)
		{
			var context = ApplicationContext.Current;

			if(context == null || string.IsNullOrEmpty(qualifiedName))
			{
				registry = null;
				return null;
			}

			(var moduleName, var eventName) = Parse(qualifiedName);

			if(context.Modules.TryGet(moduleName, out var applicationModule))
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
			var context = ApplicationContext.Current;
			if(context == null || context.Modules.Count == 0)
				yield break;

			if(context.Modules.TryGet(string.Empty, out var common))
			{
				var registry = GetEventRegistry(common);

				if(registry != null)
				{
					foreach(var descriptor in registry.Events)
						yield return descriptor;
				}
			}

			foreach(var module in context.Modules)
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

		public static IEnumerable<EventDescriptor> GetEvents(this IApplicationModule module) => GetEvents(module, out _);
		public static IEnumerable<EventDescriptor> GetEvents(this IApplicationModule module, out EventRegistryBase registry)
		{
			if(module == null)
			{
				registry = null;
				return [];
			}

			registry = GetEventRegistry(module);
			return registry == null ?[] : registry.Events;
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

		#region 嵌套子类
		public static class Marshaler
		{
			private static readonly ConcurrentDictionary<Type, Func<object, ValueTuple<object, Collections.Parameters>>> _accessors = new();

			public static byte[] Marshal(EventContext context)
			{
				if(context == null)
					return null;

				var argument = context.GetArgument();
				var content = argument == null ?
					Serialization.Serializer.Json.Serialize(new EventToken(context.Parameters)) :
					Serialization.Serializer.Json.Serialize(Activator.CreateInstance(typeof(EventToken<>).MakeGenericType(argument.GetType()), argument, context.Parameters));

				return System.Text.Encoding.UTF8.GetBytes(content);
			}

			public static (object argument, Collections.Parameters parameters) Unmarshal(string name, byte[] data)
			{
				if(data == null || data.Length == 0)
					return default;

				var descriptor = Events.GetEvent(name);
				var argumentType = GetArgumentType(descriptor);
				var targetType = argumentType == null ? typeof(EventToken) : typeof(EventToken<>).MakeGenericType(argumentType);
				var result = Serialization.Serializer.Json.Deserialize(data, targetType);

				if(result is EventToken token)
					return (null, token.Parameters);

				var accessor = _accessors.GetOrAdd(argumentType, type =>
				{
					var field1 = result.GetType().GetField(nameof(EventToken<object>.Argument), BindingFlags.Instance | BindingFlags.Public);
					var field2 = result.GetType().GetField(nameof(EventToken<object>.Parameters), BindingFlags.Instance | BindingFlags.Public);
					var parameter = Expression.Parameter(typeof(object));
					var converter = Expression.Convert(parameter, typeof(EventToken<>).MakeGenericType(type));
					var constructor = typeof(ValueTuple<object, Collections.Parameters>).GetConstructor(new[] { typeof(object), typeof(Collections.Parameters) });
					var tuple = Expression.New(constructor, Expression.Field(converter, field1), Expression.Field(converter, field2));
					return Expression.Lambda<Func<object, ValueTuple<object, Collections.Parameters>>>(tuple, parameter).Compile();
				});

				return accessor.Invoke(result);
			}

			private static Type GetArgumentType(EventDescriptor descriptor)
			{
				return descriptor != null && descriptor.GetType().IsGenericType ? descriptor.GetType().GenericTypeArguments[0] : null;
			}

			private struct EventToken
			{
				public EventToken(Collections.Parameters parameters) => this.Parameters = parameters;
				public Collections.Parameters Parameters;
			}

			private struct EventToken<TArgument>
			{
				public EventToken(TArgument argument, Collections.Parameters parameters)
				{
					this.Argument = argument;
					this.Parameters = parameters;
				}
				public TArgument Argument;
				public Collections.Parameters Parameters;
			}
		}
		#endregion
	}
}