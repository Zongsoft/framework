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
	[DefaultMember(nameof(Handlers))]
	public class EventDescriptor : IEquatable<EventDescriptor>
	{
		#region 成员字段
		private object _target;
		#endregion

		#region 构造函数
		public EventDescriptor(string name, string title = null, string description = null)
		{
			this.Name = name;
			this.Title = title;
			this.Description = description;
			this.Handlers = new List<IHandler>();
		}
		#endregion

		#region 公共属性
		/// <summary>获取事件名称。</summary>
		public string Name { get; }
		/// <summary>获取事件的显示名。</summary>
		public string Title { get; set; }
		/// <summary>获取事件的描述信息。</summary>
		public string Description { get; set; }

		/// <summary>获取事件的定义对象，对于静态事件则该属性值为所属的<see cref="Type"/>类型。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public object Target
		{
			get => _target;
			set => this.Rebind(value);
		}

		/// <summary>获取事件处理程序集。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public ICollection<IHandler> Handlers { get; }
		#endregion

		#region 绑定事件
		private void Rebind(object value)
		{
			if(_target == value)
				return;

			var older = _target;

			if(value != null)
				this.Bind(value);

			_target = value;

			if(older != null)
				this.Unbind(older);
		}

		private void Unbind(object target)
		{
			var value = GetDelegateMember(target, this.Name);

			switch(value)
			{
				case EventInfo @event:
					EventBinder.Unbind(this, target, @event);
					break;
				case FieldInfo field:
					EventBinder.Unbind(this, target, field);
					break;
				case PropertyInfo property:
					EventBinder.Unbind(this, target, property);
					break;
				case Delegate @delegate:
					EventBinder.Unbind(this, @delegate);
					break;
			}
		}

		private void Bind(object target)
		{
			var value = GetDelegateMember(target, this.Name);

			switch(value)
			{
				case EventInfo @event:
					EventBinder.Bind(this, target, @event);
					break;
				case FieldInfo field:
					EventBinder.Bind(this, target, field);
					break;
				case PropertyInfo property:
					EventBinder.Bind(this, target, property);
					break;
				case Delegate @delegate:
					EventBinder.Bind(this, @delegate);
					break;
			}
		}

		private static object GetDelegateMember(object target, string name)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			Type type = target is Type t ? t : target.GetType();
			MemberInfo[] members = type.GetMember(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

			if(members == null || members.Length == 0)
				throw new InvalidOperationException($"The specified '{name}' member is undefined in the '{type}' type.");

			switch(members[0].MemberType)
			{
				case MemberTypes.Field:
					var field = (FieldInfo)members[0];
					if(!field.FieldType.IsSubclassOf(typeof(Delegate)))
						throw new InvalidOperationException($"The '{name}' field of type '{type}' cannot be event-bound because its type is not a delegate type.");

					return field;
				case MemberTypes.Property:
					var property = (PropertyInfo)members[0];
					if(!property.PropertyType.IsSubclassOf(typeof(Delegate)))
						throw new InvalidOperationException($"The '{name}' property of type '{type}' cannot be event-bound because its type is not a delegate type.");

					return property;
				case MemberTypes.Event:
					return (EventInfo)members[0];
				default:
					throw new InvalidOperationException($"The '{name}' member of type '{type}' cannot be event-bound because the member is not a property, field, or event.");
			}
		}
		#endregion

		#region 执行处理
		public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync(object argument, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			var tasks = new List<Task>(this.Handlers.Count);

			foreach(var handler in this.Handlers)
			{
				tasks.Add(handler.HandleAsync(this, argument, parameters, cancellation).AsTask());
			}

			return new ValueTask(Task.WhenAll(tasks));
		}

		public ValueTask HandleAsync<TRequest>(TRequest argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync<TRequest>(TRequest argument, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			var tasks = new List<Task>(this.Handlers.Count);

			foreach(var handler in this.Handlers)
			{
				if(handler is IHandler<TRequest> generic)
					tasks.Add(generic.HandleAsync(this, argument, parameters, cancellation).AsTask());
				else
					tasks.Add(handler.HandleAsync(this, argument, parameters, cancellation).AsTask());
			}

			return new ValueTask(Task.WhenAll(tasks));
		}
		#endregion

		#region 重写方法
		public bool Equals(EventDescriptor other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is EventDescriptor other && this.Equals(other);
		public override int GetHashCode() => this.Name.GetHashCode();
		public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"{this.Name}[{this.Title}]";
		#endregion
	}
}