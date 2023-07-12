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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components
{
	[System.Reflection.DefaultMember(nameof(Events))]
	public class EventRegistry : IFilterable<EventContextBase>, IEnumerable<EventDescriptor>
	{
		#region 构造函数
		public EventRegistry()
		{
			this.Events = new EventDescriptorCollection();
			this.Filters = new List<IFilter<EventContextBase>>();
		}
		#endregion

		#region 公共属性
		/// <summary>获取事件描述器集合。</summary>
		public EventDescriptorCollection Events { get; }

		/// <summary>获取事件过滤器集合。</summary>
		public ICollection<IFilter<EventContextBase>> Filters { get; }
		#endregion

		#region 注册事件
		public void Event(EventDescriptor descriptor) => this.Events.Add(descriptor ?? throw new ArgumentNullException(nameof(descriptor)));

		public EventDescriptor Event(string name, string title = null, string description = null)
		{
			var descriptor = new EventDescriptor(name, title, description);
			this.Events.Add(descriptor);
			return descriptor;
		}

		public EventDescriptor Event(object target, string name, string title = null, string description = null)
		{
			var descriptor = new EventDescriptor(name, title, description);
			this.Events.Add(descriptor);
			descriptor.Target = target;
			return descriptor;
		}
		#endregion

		#region 公共方法
		public void Raise(string name, object argument, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			var task = RaiseAsync(name, argument, parameters);
			if(!task.IsCompletedSuccessfully)
				task.AsTask().GetAwaiter().GetResult();
		}

		public void Raise<TArgument>(string name, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			var task = RaiseAsync(name, argument, parameters);
			if(!task.IsCompletedSuccessfully)
				task.AsTask().GetAwaiter().GetResult();
		}

		public ValueTask RaiseAsync(string name, object argument, CancellationToken cancellation = default) => RaiseAsync(name, argument, null, cancellation);
		public async ValueTask RaiseAsync(string name, object argument, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(this.Events.TryGetValue(name, out var descriptor) && descriptor != null)
			{
				var context = new EventContext(this, name, argument, parameters);

				foreach(var filter in this.Filters)
					await this.OnFiltering(filter, context, cancellation);

				await descriptor.HandleAsync(argument, parameters, cancellation);

				foreach(var filter in this.Filters)
					await this.OnFiltered(filter, context, cancellation);
			}

			throw new InvalidOperationException($"The '{name}' event to raise is undefined.");
		}

		public ValueTask RaiseAsync<TArgument>(string name, TArgument argument, CancellationToken cancellation = default) => RaiseAsync(name, argument, null, cancellation);
		public async ValueTask RaiseAsync<TArgument>(string name, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(this.Events.TryGetValue(name, out var descriptor) && descriptor != null)
			{
				var context = new EventContext<TArgument>(this, name, argument, parameters);

				foreach(var filter in this.Filters)
					await this.OnFiltering(filter, context, cancellation);

				await descriptor.HandleAsync(argument, parameters, cancellation);

				foreach(var filter in this.Filters)
					await this.OnFiltered(filter, context, cancellation);
			}

			throw new InvalidOperationException($"The '{name}' event to raise is undefined.");
		}
		#endregion

		#region 虚拟方法
		protected virtual ValueTask OnFiltered(IFilter<EventContextBase> filter, EventContextBase context, CancellationToken cancellation) => filter?.OnFiltered(context, cancellation) ?? ValueTask.CompletedTask;
		protected virtual ValueTask OnFiltering(IFilter<EventContextBase> filter, EventContextBase context, CancellationToken cancellation) => filter?.OnFiltering(context, cancellation) ?? ValueTask.CompletedTask;
		#endregion

		#region 遍历枚举
		public IEnumerator<EventDescriptor> GetEnumerator() => this.Events.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion
	}
}