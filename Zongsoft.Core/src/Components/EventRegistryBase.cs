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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components;

public class EventRegistryBase : IFilterable<EventContext>, IEnumerable<EventDescriptor>
{
	#region 构造函数
	protected EventRegistryBase(string name)
	{
		this.Name = name ?? string.Empty;
		this.Events = new EventDescriptorCollection(this);
		this.Filters = new List<IFilter<EventContext>>();
	}
	#endregion

	#region 公共属性
	/// <summary>获取事件注册表名称。</summary>
	public string Name { get; }

	/// <summary>获取事件描述器集合。</summary>
	public EventDescriptorCollection Events { get; }

	/// <summary>获取事件过滤器集合。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public ICollection<IFilter<EventContext>> Filters { get; }

	/// <summary>获取指定索引的事件描述器。</summary>
	/// <param name="index">指定的索引位置。</param>
	/// <returns>返回对应索引位置的事件描述器。</returns>
	public EventDescriptor this[int index] => this.Events[index];

	/// <summary>获取指定名称的事件描述器。</summary>
	/// <param name="name">指定的事件名称。</param>
	/// <returns>返回对应名称的事件描述器。</returns>
	public EventDescriptor this[string name] => this.Events[name];
	#endregion

	#region 声明事件
	protected void Event<TArgument>(EventDescriptor<TArgument> descriptor) => this.Events.Add(descriptor ?? throw new ArgumentNullException(nameof(descriptor)));
	protected EventDescriptor Event<TArgument>(string name, string title = null, string description = null)
	{
		var descriptor = new EventDescriptor<TArgument>(name, title, description);
		this.Events.Add(descriptor);
		return descriptor;
	}
	#endregion

	#region 注册事件
	public bool Register<TArgument>(string name, IHandler<TArgument> handler)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(handler == null)
			throw new ArgumentNullException(nameof(handler));

		if(this.Events.TryGetValue(name, out var descriptor))
		{
			descriptor.Handlers.Add(handler);
			return true;
		}

		return false;
	}

	public bool Unregister<TArgument>(string name, IHandler<TArgument> handler)
	{
		if(string.IsNullOrEmpty(name) || handler == null)
			return false;

		return this.Events.TryGetValue(name, out var descriptor) && descriptor.Handlers.Remove(handler);
	}
	#endregion

	#region 激发方法
	protected bool Raise<TArgument>(string name, TArgument argument, Collections.Parameters parameters = null)
	{
		var task = this.RaiseAsync(name, argument, parameters);

		if(task.IsCompletedSuccessfully)
			return task.Result;
		else
			return task.AsTask().GetAwaiter().GetResult();
	}

	protected ValueTask<bool> RaiseAsync<TArgument>(string name, TArgument argument, CancellationToken cancellation = default) => this.RaiseAsync(name, argument, null, cancellation);
	protected async ValueTask<bool> RaiseAsync<TArgument>(string name, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(this.Events.TryGetValue(name, out var value) && value is EventDescriptor<TArgument> descriptor)
		{
			var context = this.GetContext(name, argument, parameters);

			//执行本地事件调用
			await this.RaiseAsync(descriptor, context, cancellation);

			//执行事件交换器的交换操作
			await EventExchanger.Instance.ExchangeAsync(context, cancellation);

			//返回激发成功
			return true;
		}

		//返回激发失败
		return false;
	}

	internal async ValueTask RaiseAsync<TArgument>(EventDescriptor descriptor, EventContext<TArgument> context, CancellationToken cancellation)
	{
		//依次执行全局事件过滤器
		foreach(var filter in EventManager.Global.Filters)
			await this.OnFiltering(filter, context, cancellation);

		//依次执行当前事件库中的过滤器
		foreach(var filter in this.Filters)
			await this.OnFiltering(filter, context, cancellation);

		//调用事件处理程序集
		await descriptor.HandleAsync(context.Argument, context.Parameters, cancellation);

		//依次执行当前事件库中的过滤器
		foreach(var filter in this.Filters)
			await this.OnFiltered(filter, context, cancellation);

		//依次执行全局事件过滤器
		foreach(var filter in EventManager.Global.Filters)
			await this.OnFiltered(filter, context, cancellation);
	}
	#endregion

	#region 内部方法
	internal protected virtual EventContext<TArgument> GetContext<TArgument>(string name, TArgument argument, Collections.Parameters parameters) => new(this, name, argument, parameters);
	#endregion

	#region 过滤方法
	protected virtual ValueTask OnFiltered(IFilter<EventContext> filter, EventContext context, CancellationToken cancellation) => filter?.OnFiltered(context, cancellation) ?? ValueTask.CompletedTask;
	protected virtual ValueTask OnFiltering(IFilter<EventContext> filter, EventContext context, CancellationToken cancellation) => filter?.OnFiltering(context, cancellation) ?? ValueTask.CompletedTask;
	#endregion

	#region 遍历枚举
	public IEnumerator<EventDescriptor> GetEnumerator() => this.Events.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	#endregion
}