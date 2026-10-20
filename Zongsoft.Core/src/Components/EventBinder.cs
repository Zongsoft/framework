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
 * Copyright (C) 2020-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Components;

public static class EventBinder
{
	#region 私有变量
	private static readonly Dictionary<EventDescriptor, Delegate> _binding = new();
	#endregion

	#region 公共方法
	public static void Bind(this EventDescriptor descriptor, object target, string name)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		switch(GetMember(target, name))
		{
			case EventInfo @event:
				Bind(descriptor, target, @event);
				break;
			case FieldInfo field:
				Bind(descriptor, target, field);
				break;
			case PropertyInfo property:
				Bind(descriptor, target, property);
				break;
		}
	}

	public static void Bind(this EventDescriptor descriptor, object target, EventInfo @event)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		if(@event == null)
			throw new ArgumentNullException(nameof(@event));

		(_, var trigger) = GetAdapter(descriptor, @event.EventHandlerType);
		@event.AddEventHandler(target is Type ? null : target, trigger);

		if(_binding.TryGetValue(descriptor, out var binding))
			@event.RemoveEventHandler(target is Type ? null : target, binding);

		_binding[descriptor] = trigger;
	}

	public static void Bind(this EventDescriptor descriptor, object target, FieldInfo field)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		if(field == null)
			throw new ArgumentNullException(nameof(field));

		if(!field.FieldType.IsSubclassOf(typeof(Delegate)))
			throw new InvalidOperationException(string.Format(Properties.Resources.EventBinder_DelegateRequired_Message, field.Name, field.FieldType));

		(_, var trigger) = GetAdapter(descriptor, field.FieldType);
		field.SetValue(target, trigger);
		_binding[descriptor] = trigger;
	}

	public static void Bind(this EventDescriptor descriptor, object target, PropertyInfo property)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		if(property == null)
			throw new ArgumentNullException(nameof(property));

		if(!property.PropertyType.IsSubclassOf(typeof(Delegate)))
			throw new InvalidOperationException(string.Format(Properties.Resources.EventBinder_DelegateRequired_Message, property.Name, property.PropertyType));

		(_, var trigger) = GetAdapter(descriptor, property.PropertyType);
		property.SetValue(target, trigger);
		_binding[descriptor] = trigger;
	}


	public static void Unbind(this EventDescriptor descriptor, object target, string name)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		switch(GetMember(target, name))
		{
			case EventInfo @event:
				Unbind(descriptor, target, @event);
				break;
			case FieldInfo field:
				Unbind(descriptor, target, field);
				break;
			case PropertyInfo property:
				Unbind(descriptor, target, property);
				break;
		}
	}

	public static void Unbind(this EventDescriptor descriptor, object target, EventInfo @event)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		if(@event == null)
			throw new ArgumentNullException(nameof(@event));

		if(_binding.TryGetValue(descriptor, out var binding))
			@event.RemoveEventHandler(target is Type ? null : target, binding);
	}

	public static void Unbind(this EventDescriptor descriptor, object target, FieldInfo field)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		if(field == null)
			throw new ArgumentNullException(nameof(field));

		if(!field.FieldType.IsSubclassOf(typeof(Delegate)))
			throw new InvalidOperationException(string.Format(Properties.Resources.EventBinder_DelegateRequired_Message, field.Name, field.FieldType));

		field.SetValue(target, null);
		_binding.Remove(descriptor);
	}

	public static void Unbind(this EventDescriptor descriptor, object target, PropertyInfo property)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		if(property == null)
			throw new ArgumentNullException(nameof(property));

		if(!property.PropertyType.IsSubclassOf(typeof(Delegate)))
			throw new InvalidOperationException(string.Format(Properties.Resources.EventBinder_DelegateRequired_Message, property.Name, property.PropertyType));

		property.SetValue(target, null);
		_binding.Remove(descriptor);
	}
	#endregion

	#region 私有方法
	private static (object adapter, Delegate trigger) GetAdapter(this EventDescriptor descriptor, Type delegateType)
	{
		var invoke = delegateType.GetMethod("Invoke");
		if(invoke == null || (invoke.ReturnType != typeof(ValueTask) && invoke.ReturnType != typeof(Task)))
			throw new NotSupportedException(Properties.Resources.EventBinder_AsyncDelegateRequired_Message);

		var parameters = invoke.GetParameters();
		var count = parameters.Length;
		if(count > 0 && parameters[count - 1].ParameterType == typeof(CancellationToken))
			count--;

		object adapter;
		if(count == 0)
			adapter = new AsyncAdapter(descriptor);
		else
		{
			var argument = count == 1 ? parameters[0].ParameterType : parameters[1].ParameterType;
			if(descriptor.GetArgumentType() != argument)
				throw new InvalidOperationException(Properties.Resources.EventBinder_ArgumentTypeMismatch_Message);

			if(count > 3 || (count > 1 && parameters[0].ParameterType != typeof(object)) ||
				(count == 3 && !typeof(Collections.Parameters).IsAssignableFrom(parameters[2].ParameterType)))
				throw new NotSupportedException(Properties.Resources.EventBinder_AsyncDelegateRequired_Message);

			adapter = Activator.CreateInstance(count == 3 ?
				typeof(AsyncAdapter<,>).MakeGenericType(argument, parameters[2].ParameterType) :
				typeof(AsyncAdapter<>).MakeGenericType(argument), [descriptor]);
		}

		return (adapter, Delegate.CreateDelegate(delegateType, adapter,
			invoke.ReturnType == typeof(ValueTask) ? nameof(AsyncAdapter.RaiseAsync) : nameof(AsyncAdapter.RaiseTaskAsync)));
	}

	private static object GetMember(object target, string name)
	{
		if(target == null)
			throw new ArgumentNullException(nameof(target));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		Type type = target is Type t ? t : target.GetType();
		MemberInfo[] members = type.GetMember(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

		if(members == null || members.Length == 0)
			throw new InvalidOperationException(string.Format(Properties.Resources.Reflection_MemberNotFound_Message, name, type));

		return members[0].MemberType switch
		{
			MemberTypes.Field => (FieldInfo)members[0],
			MemberTypes.Property => (PropertyInfo)members[0],
			MemberTypes.Event => (EventInfo)members[0],
			_ => throw new InvalidOperationException(string.Format(Properties.Resources.EventBinder_UnsupportedMember_Message, name, type)),
		};
	}

	private static Type GetArgumentType(this EventDescriptor descriptor)
	{
		return descriptor != null && descriptor.GetType().IsGenericType ? descriptor.GetType().GenericTypeArguments[0] : null;
	}
	#endregion

	#region 嵌套子类
	private sealed class AsyncAdapter(EventDescriptor descriptor)
	{
		public ValueTask RaiseAsync() => descriptor.HandleAsync();
		public ValueTask RaiseAsync(CancellationToken cancellation) => descriptor.HandleAsync(cancellation);
		public Task RaiseTaskAsync() => this.RaiseAsync().AsTask();
		public Task RaiseTaskAsync(CancellationToken cancellation) => this.RaiseAsync(cancellation).AsTask();
	}

	private sealed class AsyncAdapter<TArgument>(EventDescriptor<TArgument> descriptor)
	{
		public ValueTask RaiseAsync(TArgument argument) => descriptor.HandleAsync(argument);
		public ValueTask RaiseAsync(TArgument argument, CancellationToken cancellation) => descriptor.HandleAsync(argument, cancellation);
		public ValueTask RaiseAsync(object sender, TArgument argument) => descriptor.HandleAsync(argument);
		public ValueTask RaiseAsync(object sender, TArgument argument, CancellationToken cancellation) => descriptor.HandleAsync(argument, cancellation);
		public Task RaiseTaskAsync(TArgument argument) => this.RaiseAsync(argument).AsTask();
		public Task RaiseTaskAsync(TArgument argument, CancellationToken cancellation) => this.RaiseAsync(argument, cancellation).AsTask();
		public Task RaiseTaskAsync(object sender, TArgument argument) => this.RaiseAsync(sender, argument).AsTask();
		public Task RaiseTaskAsync(object sender, TArgument argument, CancellationToken cancellation) => this.RaiseAsync(sender, argument, cancellation).AsTask();
	}

	private sealed class AsyncAdapter<TArgument, TParameters>(EventDescriptor<TArgument> descriptor) where TParameters : Collections.Parameters
	{
		public ValueTask RaiseAsync(object sender, TArgument argument, TParameters parameters) => descriptor.HandleAsync(argument, parameters);
		public ValueTask RaiseAsync(object sender, TArgument argument, TParameters parameters, CancellationToken cancellation) => descriptor.HandleAsync(argument, parameters, cancellation);
		public Task RaiseTaskAsync(object sender, TArgument argument, TParameters parameters) => this.RaiseAsync(sender, argument, parameters).AsTask();
		public Task RaiseTaskAsync(object sender, TArgument argument, TParameters parameters, CancellationToken cancellation) => this.RaiseAsync(sender, argument, parameters, cancellation).AsTask();
	}
	#endregion
}
