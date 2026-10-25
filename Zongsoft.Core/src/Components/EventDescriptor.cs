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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Collections;

namespace Zongsoft.Components;

/// <summary>描述一个具名事件及其允许发布的参数契约。</summary>
[DefaultMember(nameof(Handlers))]
public class EventDescriptor : IEquatable<EventDescriptor>
{
	#region 成员字段
	private EventRegistryBase _registry;
	private string _qualifiedName;
	private string _title;
	private string _description;
	private readonly Dictionary<Type, string> _signatures;
	#endregion

	#region 构造函数
	/// <summary>初始化以 object 为参数契约的事件。</summary>
	/// <param name="name">事件名称。</param>
	/// <param name="title">显示名称。</param>
	/// <param name="description">说明。</param>
	public EventDescriptor(string name, string title = null, string description = null) : this(name, [typeof(object)], title, description) { }

	/// <summary>初始化具有固定参数契约集合的事件。</summary>
	/// <param name="name">事件名称。</param>
	/// <param name="argumentTypes">非空、不重复的封闭参数类型集合，构造时复制。</param>
	/// <param name="title">显示名称。</param>
	/// <param name="description">说明。</param>
	public EventDescriptor(string name, Type[] argumentTypes, string title = null, string description = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentNullException.ThrowIfNull(argumentTypes);
		if(argumentTypes.Length == 0)
			throw new ArgumentException(Properties.Resources.EventDescriptor_SignaturesRequired_Message, nameof(argumentTypes));

		_signatures = [];
		var signatures = new HashSet<string>(StringComparer.Ordinal);

		foreach(var type in argumentTypes)
		{
			if(type == null || type == typeof(void) || type.IsByRef || type.IsPointer || type.IsByRefLike || type.ContainsGenericParameters)
				throw new ArgumentException(Properties.Resources.EventDescriptor_ArgumentType_Message, nameof(argumentTypes));

			var signature = TypeAlias.GetAlias(type);
			if(!_signatures.TryAdd(type, signature) || !signatures.Add(signature))
				throw new ArgumentException(Properties.Resources.EventDescriptor_DuplicateSignature_Message, nameof(argumentTypes));
		}

		this.Name = _qualifiedName = name;
		this.Title = title;
		this.Description = description;
		this.ArgumentTypes = Array.AsReadOnly(argumentTypes.ToArray());
		this.Handlers = new HandlerCollection(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取事件名称。</summary>
	public string Name { get; }
	/// <summary>获取事件限定名称。</summary>
	public string QualifiedName => _qualifiedName;
	/// <summary>获取允许发布的声明参数类型，不根据运行时实参扩充。</summary>
	public IReadOnlyList<Type> ArgumentTypes { get; }
	/// <summary>获取或设置显示名称。</summary>
	public string Title { get => _title ?? this.GetTitle(); set => _title = value; }
	/// <summary>获取或设置事件说明。</summary>
	public string Description { get => _description ?? this.GetDescription(); set => _description = value; }

	/// <summary>获取或设置事件级断言；断言应能接收所有声明签名。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public IPredication Predication { get; set; }
	/// <summary>获取类型化处理器集合；不匹配任何签名或匹配歧义的处理器在追加时被拒绝。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public ICollection<IHandler> Handlers { get; }
	#endregion

	#region 签名方法
	/// <summary>检查是否声明指定参数契约。</summary>
	/// <param name="argumentType">声明参数类型。</param>
	/// <returns>精确声明该类型时返回 true。</returns>
	public bool Supports(Type argumentType) => argumentType != null && _signatures.ContainsKey(argumentType);

	internal string GetSignature(Type argumentType) => _signatures.TryGetValue(argumentType, out var signature) ? signature :
		throw new ArgumentException(Properties.Resources.EventDescriptor_ArgumentType_Message, nameof(argumentType));

	internal Type GetArgumentType(string signature) => _signatures.FirstOrDefault(entry => string.Equals(entry.Value, signature, StringComparison.Ordinal)).Key ??
		throw new ArgumentException(Properties.Resources.EventDescriptor_ArgumentType_Message, nameof(signature));

	internal void Validate(Type argumentType, object argument)
	{
		if(!this.Supports(argumentType) || (argument == null ? argumentType.IsValueType && Nullable.GetUnderlyingType(argumentType) == null : !argumentType.IsInstanceOfType(argument)))
			throw new ArgumentException(Properties.Resources.EventDescriptor_ArgumentType_Message, nameof(argument));
	}

	internal void SetRegistry(EventRegistryBase registry)
	{
		_registry = registry;
		_qualifiedName = string.IsNullOrEmpty(registry?.Name) ? this.Name : $"{registry.Name}:{this.Name}";
	}
	#endregion

	#region 处理方法
	/// <summary>以 <see langword="object"/> 契约发布空实参。</summary>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>全部匹配处理器的完成任务。</returns>
	public ValueTask HandleAsync(CancellationToken cancellation = default) => this.HandleAsync<object>(null, null, cancellation);

	/// <summary>以 <see langword="object"/> 契约发布空实参。</summary>
	/// <param name="parameters">附加参数。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>全部匹配处理器的完成任务。</returns>
	public ValueTask HandleAsync(Parameters parameters, CancellationToken cancellation = default) => this.HandleAsync<object>(null, parameters, cancellation);

	/// <summary>以泛型声明类型派发实参。</summary>
	/// <param name="argument">实参。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>全部匹配处理器的完成任务。</returns>
	public ValueTask HandleAsync<TArgument>(TArgument argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);

	/// <summary>以泛型声明类型派发实参。</summary>
	/// <param name="argument">实参。</param>
	/// <param name="parameters">附加参数。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>全部匹配处理器的完成任务。</returns>
	public ValueTask HandleAsync<TArgument>(TArgument argument, Parameters parameters, CancellationToken cancellation = default) => this.HandleAsync(typeof(TArgument), argument, parameters, cancellation);

	/// <summary>以显式声明类型派发实参；不使用运行时类型重新选择签名。</summary>
	/// <param name="argumentType">已声明的参数类型。</param>
	/// <param name="argument">兼容该类型的实参。</param>
	/// <param name="parameters">附加参数。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>全部匹配处理器的完成任务。</returns>
	public async ValueTask HandleAsync(Type argumentType, object argument, Parameters parameters, CancellationToken cancellation = default)
	{
		this.Validate(argumentType, argument);

		if(!await this.OnPredicateAsync(argument, parameters, cancellation).ConfigureAwait(false))
			return;

		var handlers = ((HandlerCollection)this.Handlers).GetHandlers(argumentType);
		var tasks = new List<Task>(handlers.Length);

		foreach(var handler in handlers)
		{
			try { tasks.Add(handler(argument, parameters, cancellation).AsTask()); }
			catch(Exception exception) { tasks.Add(Task.FromException(exception)); }
		}

		await Task.WhenAll(tasks).ConfigureAwait(false);
	}
	#endregion

	#region 保护方法
	/// <summary>计算事件断言。</summary>
	/// <param name="argument">实参。</param>
	/// <param name="parameters">附加参数。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>是否执行处理器。</returns>
	protected virtual ValueTask<bool> OnPredicateAsync(object argument, Parameters parameters, CancellationToken cancellation) =>
		this.Predication?.PredicateAsync(argument, parameters, cancellation) ?? ValueTask.FromResult(true);
	#endregion

	#region 重写方法
	public bool Equals(EventDescriptor other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is EventDescriptor other && this.Equals(other);
	public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.QualifiedName);
	public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.QualifiedName : $"{this.QualifiedName}[{this.Title}]";
	#endregion

	#region 私有方法
	private string GetTitle() => _registry == null ? null : Resources.ResourceUtility.GetResourceString(_registry.GetType(), [$"{this.Name}.{nameof(this.Title)}", this.Name]);
	private string GetDescription() => _registry == null ? null : Resources.ResourceUtility.GetResourceString(_registry.GetType(), $"{this.Name}.{nameof(this.Description)}");
	#endregion

	#region 嵌套子类
	private sealed class HandlerCollection(EventDescriptor descriptor) : ICollection<IHandler>
	{
		private readonly object _sync = new();
		private readonly Dictionary<IHandler, Dictionary<Type, Func<object, Parameters, CancellationToken, ValueTask>>> _handlers = new(ReferenceEqualityComparer.Instance);

		public int Count
		{
			get
			{
				lock(_sync)
					return _handlers.Count;
			}
		}

		bool ICollection<IHandler>.IsReadOnly => false;

		public Func<object, Parameters, CancellationToken, ValueTask>[] GetHandlers(Type argumentType)
		{
			lock(_sync)
				return _handlers.Values.Where(handlers => handlers.ContainsKey(argumentType)).Select(handlers => handlers[argumentType]).ToArray();
		}

		public void Add(IHandler handler)
		{
			ArgumentNullException.ThrowIfNull(handler);
			var contracts = handler.GetType().GetInterfaces().Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IHandler<>)).Select(type => type.GenericTypeArguments[0]).Distinct().ToArray();
			var handlers = new Dictionary<Type, Func<object, Parameters, CancellationToken, ValueTask>>();

			foreach(var argumentType in descriptor.ArgumentTypes)
			{
				var matches = contracts.Where(type => type.IsAssignableFrom(argumentType)).ToArray();
				if(matches.Length == 0)
					continue;

				var contract = matches.FirstOrDefault(type => type == argumentType);
				if(contract == null)
				{
					var specific = matches.Where(type => !matches.Any(other => type != other && type.IsAssignableFrom(other))).ToArray();
					if(specific.Length != 1)
						throw new ArgumentException(Properties.Resources.EventDescriptor_HandlerAmbiguous_Message, nameof(handler));

					contract = specific[0];
				}

				handlers.Add(argumentType, (Func<object, Parameters, CancellationToken, ValueTask>)typeof(HandlerCollection).GetMethod(nameof(Bind), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(contract).Invoke(null, [handler]));
			}

			if(handlers.Count == 0)
				throw new ArgumentException(Properties.Resources.EventDescriptor_HandlerUnsupported_Message, nameof(handler));

			lock(_sync)
				_handlers.TryAdd(handler, handlers);
		}

		public void Clear()
		{
			lock(_sync)
				_handlers.Clear();
		}
		public bool Contains(IHandler handler)
		{
			lock(_sync)
				return handler != null && _handlers.ContainsKey(handler);
		}
		public bool Remove(IHandler handler)
		{
			lock(_sync)
				return handler != null && _handlers.Remove(handler);
		}
		public void CopyTo(IHandler[] array, int arrayIndex)
		{
			lock(_sync)
				_handlers.Keys.CopyTo(array, arrayIndex);
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<IHandler> GetEnumerator()
		{
			lock(_sync)
				return ((IEnumerable<IHandler>)_handlers.Keys.ToArray()).GetEnumerator();
		}

		private static Func<object, Parameters, CancellationToken, ValueTask> Bind<TArgument>(IHandler handler) =>
			(argument, parameters, cancellation) => ((IHandler<TArgument>)handler).HandleAsync((TArgument)argument, parameters, cancellation);
	}
	#endregion
}

/// <summary>描述以指定类型为唯一参数契约的具名事件。</summary>
/// <typeparam name="TArgument">事件参数的声明类型。</typeparam>
public class EventDescriptor<TArgument> : EventDescriptor
{
	#region 构造函数
	/// <summary>初始化具有指定参数契约的事件。</summary>
	/// <param name="name">事件名称。</param>
	/// <param name="title">显示名称。</param>
	/// <param name="description">说明。</param>
	public EventDescriptor(string name, string title = null, string description = null) : base(name, [typeof(TArgument)], title, description) { }

	/// <summary>初始化具有指定参数契约和断言的事件。</summary>
	/// <param name="predication">事件断言；为空时不限制处理。</param>
	/// <param name="name">事件名称。</param>
	/// <param name="title">显示名称。</param>
	/// <param name="description">说明。</param>
	public EventDescriptor(IPredication<TArgument> predication, string name, string title = null, string description = null) : this(name, title, description) => this.Predication = predication;
	#endregion

	#region 处理方法
	/// <summary>以描述器的声明类型派发实参。</summary>
	/// <param name="argument">事件实参。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>全部匹配处理器的完成任务。</returns>
	public ValueTask HandleAsync(TArgument argument, CancellationToken cancellation = default) => base.HandleAsync<TArgument>(argument, null, cancellation);

	/// <summary>以描述器的声明类型派发实参。</summary>
	/// <param name="argument">事件实参。</param>
	/// <param name="parameters">附加参数。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>全部匹配处理器的完成任务。</returns>
	public ValueTask HandleAsync(TArgument argument, Parameters parameters, CancellationToken cancellation = default) => base.HandleAsync<TArgument>(argument, parameters, cancellation);
	#endregion
}
