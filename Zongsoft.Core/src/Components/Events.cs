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

namespace Zongsoft.Components;

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

		if(context.Modules.TryGetValue(moduleName, out var applicationModule))
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

		if(context.Modules.TryGetValue(string.Empty, out var common))
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

		public static (object argument, Collections.Parameters parameters) Unmarshal(string qualifiedName, ReadOnlySpan<byte> data) => Unmarshal(GetEvent(qualifiedName), data);
		public static (object argument, Collections.Parameters parameters) Unmarshal(EventDescriptor descriptor, ReadOnlySpan<byte> data)
		{
			if(descriptor == null || data.IsEmpty)
				return default;

			var argumentType = GetArgumentType(descriptor);
			var targetType = argumentType == null ? typeof(EventToken) : typeof(EventToken<>).MakeGenericType(argumentType);
			var result = Serialization.Serializer.Json.Deserialize(data, targetType);

			if(result is EventToken token)
				return (null, token.Parameters);

			var accessor = _accessors.GetOrAdd(argumentType, type =>
			{
				/*
				 * 生成的动态代码：
				 * ValueTuple<object, Parameters> Unnamed(object target)
				 * {
				 *     var token = ((EventToken<XXXXX>)target);
				 *     return new ValueTuple<object, Parameters>((object)token.Argument, token.Parameters);
				 * }
				 */
				var tokenType = typeof(EventToken<>).MakeGenericType(type);
				var parameter = Expression.Parameter(typeof(object), "target");
				var variable = Expression.Variable(tokenType, "token");

				var field1 = Expression.Field(variable, tokenType.GetField(nameof(EventToken<object>.Argument), BindingFlags.Instance | BindingFlags.Public));
				var field2 = Expression.Field(variable, tokenType.GetField(nameof(EventToken<object>.Parameters), BindingFlags.Instance | BindingFlags.Public));
				var constructor = typeof(ValueTuple<object, Collections.Parameters>).GetConstructor([typeof(object), typeof(Collections.Parameters)]);

				var body = Expression.Block([variable],
				[
					Expression.Assign(variable, Expression.Convert(parameter, tokenType)),
					Expression.New(constructor, Expression.Convert(field1, typeof(object)), field2)
				]);

				return Expression.Lambda<Func<object, ValueTuple<object, Collections.Parameters>>>(body, parameter).Compile();
			});

			return accessor.Invoke(result);
		}

		private static Type GetArgumentType(EventDescriptor descriptor)
		{
			return descriptor != null && descriptor.GetType().IsGenericType ? descriptor.GetType().GenericTypeArguments[0] : null;
		}

		private struct EventToken(Collections.Parameters parameters)
		{
			public Collections.Parameters Parameters = parameters;
		}

		private struct EventToken<TArgument>(TArgument argument, Collections.Parameters parameters)
		{
			public TArgument Argument = argument;
			public Collections.Parameters Parameters = parameters;
		}
	}
	#endregion
}