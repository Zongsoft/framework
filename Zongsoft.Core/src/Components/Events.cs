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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

public static partial class Events
{
	#region 私有变量
	private static readonly ConcurrentDictionary<IApplicationModule, Func<IApplicationModule, EventRegistryBase>> _accessors = new();
	#endregion

	#region 公共方法
	/// <summary>获取当前应用所有模块的事件总数。</summary>
	/// <returns>已注册事件的总数；没有当前应用或模块时为零。</returns>
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

	/// <summary>获取指定事件的描述。</summary>
	/// <param name="qualifiedName">事件的完整限定名；没有模块名前缀时查找公共模块。</param>
	/// <returns>匹配的事件描述；事件不存在时为 <see langword="null"/>。</returns>
	public static EventDescriptor GetEvent(string qualifiedName) => GetEvent(qualifiedName, out _);

	/// <summary>获取指定事件的描述及其所属注册表。</summary>
	/// <param name="qualifiedName">事件的完整限定名；没有模块名前缀时查找公共模块。</param>
	/// <param name="registry">所属模块的事件注册表；模块或注册表不存在时为 <see langword="null"/>。</param>
	/// <returns>匹配的事件描述；事件不存在时为 <see langword="null"/>。</returns>
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

	/// <summary>枚举当前应用所有模块的事件描述。</summary>
	/// <returns>延迟枚举的事件描述，公共模块优先；没有当前应用或模块时为空序列。</returns>
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

	/// <summary>获取指定应用模块的事件描述集合。</summary>
	/// <param name="module">应用模块。</param>
	/// <returns>模块的事件描述集合；模块或注册表不存在时为空序列。</returns>
	public static IEnumerable<EventDescriptor> GetEvents(this IApplicationModule module) => GetEvents(module, out _);

	/// <summary>获取指定应用模块的事件描述集合及注册表。</summary>
	/// <param name="module">应用模块。</param>
	/// <param name="registry">模块的事件注册表；不存在时为 <see langword="null"/>。</param>
	/// <returns>模块的事件描述集合；模块或注册表不存在时为空序列。</returns>
	public static IEnumerable<EventDescriptor> GetEvents(this IApplicationModule module, out EventRegistryBase registry)
	{
		if(module == null)
		{
			registry = null;
			return [];
		}

		registry = GetEventRegistry(module);
		return registry == null ? [] : registry.Events;
	}
	#endregion

	#region 私有方法
	/// <summary>通过缓存的访问器读取模块的事件注册表。</summary>
	/// <param name="module">提供事件注册表的应用模块。</param>
	/// <returns>模块的事件注册表；模块不提供注册表时为 <see langword="null"/>。</returns>
	private static EventRegistryBase GetEventRegistry(IApplicationModule module) =>
		_accessors.GetOrAdd(module, CreateAccessor)?.Invoke(module);

	/*
	 * 生成获取 IApplicationModule<TEvents> 应用模块 Events 属性的调用委托，该委托代码如下所示：
	 * EventRegistryBase Invoke(IApplicationModule module)
	 * {
	 *		return ((IApplicationModule<TEvents>)module).Events;
	 * }
	 */
	/// <summary>为应用模块创建事件注册表访问器。</summary>
	/// <param name="module">用于确定模块类型的实例。</param>
	/// <returns>读取事件注册表的委托；模块不继承 <see cref="ApplicationModule{TEvents}"/> 时为 <see langword="null"/>。</returns>
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
		public static byte[] Marshal(EventContext context)
		{
			if(context == null)
				return null;

			var descriptor = context.Descriptor ?? throw new ArgumentException(Properties.Resources.EventDescriptor_Unregistered_Message, nameof(context));
			var argument = context.GetArgument();
			descriptor.Validate(context.ArgumentType, argument);

			var token = Activator.CreateInstance(typeof(EventToken<>).MakeGenericType(context.ArgumentType));
			((IEventToken)token).Initialize(descriptor.GetSignature(context.ArgumentType), argument, context.Parameters);
			return System.Text.Encoding.UTF8.GetBytes(Serialization.Serializer.Json.Serialize(token));
		}

		public static (Type argumentType, object argument, Collections.Parameters parameters) Unmarshal(string qualifiedName, ReadOnlySpan<byte> data) => Unmarshal(GetEvent(qualifiedName), data);
		public static (Type argumentType, object argument, Collections.Parameters parameters) Unmarshal(EventDescriptor descriptor, ReadOnlySpan<byte> data)
		{
			if(descriptor == null || data.IsEmpty)
				return default;

			using var document = System.Text.Json.JsonDocument.Parse(data.ToArray());
			if(!document.RootElement.TryGetProperty(nameof(EventToken<object>.Signature), out var signature) || signature.ValueKind != System.Text.Json.JsonValueKind.String)
				throw new System.Text.Json.JsonException(Properties.Resources.EventDescriptor_ArgumentType_Message);

			var argumentType = descriptor.GetArgumentType(signature.GetString());
			var token = (IEventToken)Serialization.Serializer.Json.Deserialize(data, typeof(EventToken<>).MakeGenericType(argumentType));
			descriptor.Validate(argumentType, token.GetArgument());
			return (argumentType, token.GetArgument(), token.GetParameters());
		}

		private interface IEventToken
		{
			void Initialize(string signature, object argument, Collections.Parameters parameters);
			object GetArgument();
			Collections.Parameters GetParameters();
		}

		private sealed class EventToken<TArgument> : IEventToken
		{
			[System.Text.Json.Serialization.JsonPropertyName("Signature")]
			public string Signature { get; set; }
			[System.Text.Json.Serialization.JsonPropertyName("Argument")]
			public TArgument Argument { get; set; }
			[System.Text.Json.Serialization.JsonPropertyName("Parameters")]
			public Collections.Parameters Parameters { get; set; }

			public void Initialize(string signature, object argument, Collections.Parameters parameters)
			{
				this.Signature = signature;
				this.Argument = (TArgument)argument;
				this.Parameters = parameters;
			}

			object IEventToken.GetArgument() => this.Argument;
			Collections.Parameters IEventToken.GetParameters() => this.Parameters;
		}
	}
	#endregion
}
