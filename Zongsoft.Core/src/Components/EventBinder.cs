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

namespace Zongsoft.Components
{
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
				case Delegate @delegate:
					Bind(descriptor, @delegate);
					break;
			}
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
				case Delegate @delegate:
					Unbind(descriptor, @delegate);
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

		public static void Unbind(this EventDescriptor descriptor, object target, EventInfo @event)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			if(@event == null)
				throw new ArgumentNullException(nameof(@event));

			if(_binding.TryGetValue(descriptor, out var binding))
				@event.RemoveEventHandler(target is Type ? null : target, binding);
		}

		public static void Bind(this EventDescriptor descriptor, object target, FieldInfo field)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(!field.FieldType.IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException($"The '{field.Name}' field of type '{field.FieldType}' cannot be event-bound because its type is not a delegate type.");

			(_, var trigger) = GetAdapter(descriptor, field.FieldType);
			field.SetValue(target, trigger);
			_binding[descriptor] = trigger;
		}

		public static void Unbind(this EventDescriptor descriptor, object target, FieldInfo field)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			if(field == null)
				throw new ArgumentNullException(nameof(field));

			if(!field.FieldType.IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException($"The '{field.Name}' field of type '{field.FieldType}' cannot be event-bound because its type is not a delegate type.");

			field.SetValue(target, null);
			_binding.Remove(descriptor);
		}

		public static void Bind(this EventDescriptor descriptor, object target, PropertyInfo property)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(!property.PropertyType.IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException($"The '{property.Name}' property of type '{property.PropertyType}' cannot be event-bound because its type is not a delegate type.");

			(_, var trigger) = GetAdapter(descriptor, property.PropertyType);
			property.SetValue(target, trigger);
			_binding[descriptor] = trigger;
		}

		public static void Unbind(this EventDescriptor descriptor, object target, PropertyInfo property)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			if(property == null)
				throw new ArgumentNullException(nameof(property));

			if(!property.PropertyType.IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException($"The '{property.Name}' property of type '{property.PropertyType}' cannot be event-bound because its type is not a delegate type.");

			property.SetValue(target, null);
			_binding.Remove(descriptor);
		}

		public static Delegate Bind(this EventDescriptor descriptor, Delegate @delegate)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			if(@delegate == null)
				throw new ArgumentNullException(nameof(@delegate));

			(_, var trigger) = GetAdapter(descriptor, @delegate.GetType());

			if(_binding.TryGetValue(descriptor, out var binding))
				Delegate.Remove(@delegate, binding);

			return _binding[descriptor] = Delegate.Combine(@delegate, trigger);
		}

		public static Delegate Unbind(this EventDescriptor descriptor, Delegate @delegate)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			if(@delegate == null)
				throw new ArgumentNullException(nameof(@delegate));

			return _binding.TryGetValue(descriptor, out var binding) ?
				Delegate.Remove(@delegate, binding) : null;
		}
		#endregion

		#region 私有方法
		private static (object adapter, Delegate trigger) GetAdapter(this EventDescriptor descriptor, Type delegateType)
		{
			const string EVENT_BINDING_ERROR_MESSAGE = @"The bound event argument type do not match.";

			static bool IsParametersType(Type type) => typeof(Collections.Parameters).IsAssignableFrom(type);

			if(delegateType == typeof(Action))
			{
				var adapter = new ActionAdapter(descriptor);
				var trigger = new Action(adapter.Raise);
				return (adapter, trigger);
			}

			if(delegateType == typeof(EventHandler))
			{
				if(descriptor.GetArgumentType() != typeof(EventArgs))
					throw new InvalidOperationException(EVENT_BINDING_ERROR_MESSAGE);

				var adapter = new EventHandlerAdapter(descriptor as EventDescriptor<EventArgs>);
				var trigger = new EventHandler(adapter.Raise);
				return (adapter, trigger);
			}

			if(delegateType.IsGenericType)
			{
				var prototype = delegateType.GetGenericTypeDefinition();

				if(prototype == typeof(EventHandler<>))
				{
					if(descriptor.GetArgumentType() != delegateType.GenericTypeArguments[0])
						throw new InvalidOperationException(EVENT_BINDING_ERROR_MESSAGE);

					var adapter = Activator.CreateInstance(
						typeof(EventHandlerAdapter<>).MakeGenericType(delegateType.GenericTypeArguments[0]),
						[descriptor]);
					var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(EventHandlerAdapter<EventArgs>.Raise));
					return (adapter, trigger);
				}

				if(prototype == typeof(Action<,,>))
				{
					if(descriptor.GetArgumentType() != delegateType.GenericTypeArguments[1])
						throw new InvalidOperationException(EVENT_BINDING_ERROR_MESSAGE);

					if(delegateType.GenericTypeArguments[0] == typeof(object) &&
					   IsParametersType(delegateType.GenericTypeArguments[2]))
					{
						var adapter = Activator.CreateInstance(
							typeof(ActionAdapterWithCaller<,>).MakeGenericType(delegateType.GenericTypeArguments[1], delegateType.GenericTypeArguments[2]),
							[descriptor]);
						var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(ActionAdapterWithCaller<object, Collections.Parameters>.Raise));
						return (adapter, trigger);
					}
				}

				if(prototype == typeof(Action<,>))
				{
					if(descriptor.GetArgumentType() != delegateType.GenericTypeArguments[1])
						throw new InvalidOperationException(EVENT_BINDING_ERROR_MESSAGE);

					if(delegateType.GenericTypeArguments[0] == typeof(object))
					{
						var adapter = Activator.CreateInstance(
							typeof(ActionAdapterWithCaller<>).MakeGenericType(delegateType.GenericTypeArguments[1]),
							[descriptor]);
						var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(ActionAdapterWithCaller<object>.Raise));
						return (adapter, trigger);
					}
				}

				if(prototype == typeof(Action<>))
				{
					if(descriptor.GetArgumentType() != delegateType.GenericTypeArguments[0])
						throw new InvalidOperationException(EVENT_BINDING_ERROR_MESSAGE);

					var adapter = Activator.CreateInstance(
						typeof(ActionAdapter<>).MakeGenericType(delegateType.GenericTypeArguments[0]),
						[descriptor]);
					var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(ActionAdapter<object>.Raise));
					return (adapter, trigger);
				}
			}

			return default;
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
				throw new InvalidOperationException($"The specified '{name}' member is undefined in the '{type}' type.");

			return members[0].MemberType switch
			{
				MemberTypes.Field => (FieldInfo)members[0],
				MemberTypes.Property => (PropertyInfo)members[0],
				MemberTypes.Event => (EventInfo)members[0],
				_ => throw new InvalidOperationException($"The '{name}' member of type '{type}' cannot be event-bound because the member is not a property, field, or event."),
			};
		}

		private static void Complete(this ValueTask task)
		{
			if(task.IsCompletedSuccessfully)
				return;

			if(task.IsCompleted)
			{
				var exception = task.AsTask().Exception;
				if(exception == null || exception.InnerException is OperationCanceledException)
					return;

				if(exception.InnerExceptions.Count == 1)
					throw exception.InnerException;
				else
					throw exception;
			}

			task.GetAwaiter().GetResult();
		}

		private static Type GetArgumentType(this EventDescriptor descriptor)
		{
			return descriptor != null && descriptor.GetType().IsGenericType ? descriptor.GetType().GenericTypeArguments[0] : null;
		}
		#endregion

		#region 嵌套子类
		private class ActionAdapter(EventDescriptor descriptor)
		{
			private readonly EventDescriptor _descriptor = descriptor;
			public void Raise() => _descriptor.HandleAsync().Complete();
		}

		private class ActionAdapter<TArgument>(EventDescriptor<TArgument> descriptor)
		{
			private readonly EventDescriptor<TArgument> _descriptor = descriptor;
			public void Raise(TArgument argument) => _descriptor.HandleAsync(argument).Complete();
		}

		private class ActionAdapterWithCaller<TArgument>(EventDescriptor<TArgument> descriptor)
		{
			private readonly EventDescriptor<TArgument> _descriptor = descriptor;
			public void Raise(object _, TArgument argument) => _descriptor.HandleAsync(argument).Complete();
		}

		private class ActionAdapterWithCaller<TArgument, TParameters>(EventDescriptor<TArgument> descriptor) where TParameters : Collections.Parameters
		{
			private readonly EventDescriptor<TArgument> _descriptor = descriptor;
			public void Raise(object _, TArgument argument, TParameters parameters) => _descriptor.HandleAsync(argument, parameters).Complete();
		}

		private class EventHandlerAdapter(EventDescriptor<EventArgs> descriptor)
		{
			private readonly EventDescriptor<EventArgs> _descriptor = descriptor;
			public void Raise(object _, EventArgs argument) => _descriptor.HandleAsync(argument).Complete();
		}

		private class EventHandlerAdapter<TArgument>(EventDescriptor<TArgument> descriptor) where TArgument : EventArgs
		{
			private readonly EventDescriptor<TArgument> _descriptor = descriptor;
			public void Raise(object _, TArgument argument) => _descriptor.HandleAsync(argument).Complete();
		}
		#endregion
	}
}