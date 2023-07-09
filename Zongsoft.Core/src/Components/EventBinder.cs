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
	internal static class EventBinder
	{
		private static readonly Dictionary<EventDescriptor, Delegate> _binding = new();

		public static void Bind(this EventDescriptor descriptor, object target, EventInfo @event)
		{
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
			if(@event == null)
				throw new ArgumentNullException(nameof(@event));

			if(_binding.TryGetValue(descriptor, out var binding))
				@event.RemoveEventHandler(target is Type ? null : target, binding);
		}

		public static void Bind(this EventDescriptor descriptor, object target, FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			(_, var trigger) = GetAdapter(descriptor, field.FieldType);
			field.SetValue(target, trigger);
			_binding[descriptor] = trigger;
		}

		public static void Unbind(this EventDescriptor descriptor, object target, FieldInfo field)
		{
			if(field == null)
				throw new ArgumentNullException(nameof(field));

			field.SetValue(target, null);
			_binding.Remove(descriptor);
		}

		public static void Bind(this EventDescriptor descriptor, object target, PropertyInfo property)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			(_, var trigger) = GetAdapter(descriptor, property.PropertyType);
			property.SetValue(target, trigger);
			_binding[descriptor] = trigger;
		}

		public static void Unbind(this EventDescriptor descriptor, object target, PropertyInfo property)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			property.SetValue(target, null);
			_binding.Remove(descriptor);
		}

		public static Delegate Bind(this EventDescriptor descriptor, Delegate @delegate)
		{
			if(@delegate == null)
				throw new ArgumentNullException(nameof(@delegate));

			(_, var trigger) = GetAdapter(descriptor, @delegate.GetType());

			if(_binding.TryGetValue(descriptor, out var binding))
				Delegate.Remove(@delegate, binding);

			return _binding[descriptor] = Delegate.Combine(@delegate, trigger);
		}

		public static Delegate Unbind(this EventDescriptor descriptor, Delegate @delegate)
		{
			if(@delegate == null)
				throw new ArgumentNullException(nameof(@delegate));

			return _binding.TryGetValue(descriptor, out var binding) ?
				Delegate.Remove(@delegate, binding) : null;
		}

		private static (object adapter, Delegate trigger) GetAdapter(this EventDescriptor descriptor, Type delegateType)
		{
			static bool IsParametersType(Type type) => typeof(IEnumerable<KeyValuePair<string, object>>).IsAssignableFrom(type);

			if(delegateType.IsGenericType)
			{
				var prototype = delegateType.GetGenericTypeDefinition();

				if(prototype == typeof(EventHandler<>))
				{
					var adapter = Activator.CreateInstance(
						typeof(EventHandlerAdapter<>).MakeGenericType(delegateType.GenericTypeArguments[0]),
						new object[] { descriptor });
					var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(EventHandlerAdapter<object>.Raise));
					return (adapter, trigger);
				}

				if(prototype == typeof(Action<,,>))
				{
					if(delegateType.GenericTypeArguments[0] == typeof(object) &&
					   IsParametersType(delegateType.GenericTypeArguments[2]))
					{
						var adapter = Activator.CreateInstance(
							typeof(ActionAdapter3<,>).MakeGenericType(delegateType.GenericTypeArguments[1], delegateType.GenericTypeArguments[2]),
							new object[] { descriptor });
						var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(ActionAdapter3<object, IDictionary<string, object>>.Raise));
						return (adapter, trigger);
					}
				}

				if(prototype == typeof(Action<,>))
				{
					if(delegateType.GenericTypeArguments[0] == typeof(object))
					{
						var adapter = Activator.CreateInstance(
							typeof(ActionAdapter2<>).MakeGenericType(delegateType.GenericTypeArguments[1]),
							new object[] { descriptor });
						var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(ActionAdapter2<object>.Raise));
						return (adapter, trigger);
					}
				}

				if(prototype == typeof(Action<>))
				{
					var adapter = Activator.CreateInstance(
						typeof(ActionAdapter1<>).MakeGenericType(delegateType.GenericTypeArguments[0]),
						new object[] { descriptor });
					var trigger = Delegate.CreateDelegate(delegateType, adapter, nameof(ActionAdapter1<object>.Raise));
					return (adapter, trigger);
				}
			}

			if(delegateType == typeof(EventHandler))
			{
				var adapter = new EventHandlerAdapter(descriptor);
				var trigger = new EventHandler(adapter.Raise);
				return (adapter, trigger);
			}

			if(delegateType == typeof(Action))
			{
				var adapter = new ActionAdapter(descriptor);
				var trigger = new Action(adapter.Raise);
				return (adapter, trigger);
			}

			return default;
		}

		private static void DoTask(this ValueTask task)
		{
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

			task.AsTask().Wait();
		}

		private class ActionAdapter
		{
			private readonly EventDescriptor _descriptor;
			public ActionAdapter(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise()
			{
				_descriptor.HandleAsync(null).DoTask();
			}
		}

		private class ActionAdapter1
		{
			private readonly EventDescriptor _descriptor;
			public ActionAdapter1(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(object argument)
			{
				_descriptor.HandleAsync(argument).DoTask();
			}
		}

		private class ActionAdapter1<T>
		{
			private readonly EventDescriptor _descriptor;
			public ActionAdapter1(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(T argument)
			{
				_descriptor.HandleAsync(argument).DoTask();
			}
		}

		private class ActionAdapter2
		{
			private readonly EventDescriptor _descriptor;
			public ActionAdapter2(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(object sender, object argument)
			{
				_descriptor.HandleAsync(argument).DoTask();
			}
		}

		private class ActionAdapter2<T>
		{
			private readonly EventDescriptor _descriptor;
			public ActionAdapter2(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(object sender, T argument)
			{
				_descriptor.HandleAsync(argument).DoTask();
			}
		}

		private class ActionAdapter3<TParameters> where TParameters : IEnumerable<KeyValuePair<string, object>>
		{
			private readonly EventDescriptor _descriptor;
			public ActionAdapter3(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(object sender, object argument, TParameters parameters)
			{
				_descriptor.HandleAsync(argument, parameters).DoTask();
			}
		}

		private class ActionAdapter3<T, TParameters> where TParameters : IEnumerable<KeyValuePair<string, object>>
		{
			private readonly EventDescriptor _descriptor;
			public ActionAdapter3(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(object sender, T argument, TParameters parameters)
			{
				_descriptor.HandleAsync(argument, parameters).DoTask();
			}
		}

		private class EventHandlerAdapter
		{
			private readonly EventDescriptor _descriptor;
			public EventHandlerAdapter(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(object sender, EventArgs argument)
			{
				_descriptor.HandleAsync(argument).DoTask();
			}
		}

		private class EventHandlerAdapter<T>
		{
			private readonly EventDescriptor _descriptor;
			public EventHandlerAdapter(EventDescriptor descriptor) => _descriptor = descriptor;

			public void Raise(object sender, T argument)
			{
				_descriptor.HandleAsync(argument).DoTask();
			}
		}
	}
}