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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Components;

[System.Reflection.DefaultMember(nameof(Channels))]
[System.ComponentModel.DefaultProperty(nameof(Channels))]
public sealed class EventExchanger : WorkerBase
{
	#region 单例字段
	public static readonly EventExchanger Instance = new();
	#endregion

	#region 成员字段
	private Func<string, (EventRegistryBase Registry, EventDescriptor Descriptor)> _locator;
	private readonly List<IEventChannel> _channels;
	#endregion

	#region 私有构造
	private EventExchanger()
	{
		_locator = Locate;
		_channels = new List<IEventChannel>();
	}
	#endregion

	#region 公共属性
	/// <summary>获取事件交换通道集合。</summary>
	public ICollection<IEventChannel> Channels => _channels;

	/// <summary>获取或设置事件处理程序定位器。</summary>
	public Func<string, (EventRegistryBase Registry, EventDescriptor Descriptor)> Locator
	{
		get => _locator;
		set => _locator = value ?? Locate;
	}
	#endregion

	#region 公共方法
	public ValueTask ExchangeAsync(EventContext context, CancellationToken cancellation)
	{
		if(this.State != WorkerState.Running)
			return ValueTask.CompletedTask;

		var tasks = new Task[_channels.Count];

		for(int i = 0; i < _channels.Count; i++)
		{
			tasks[i] = _channels[i].SendAsync(context, cancellation).AsTask();
		}

		return new(Task.WhenAll(tasks));
	}

	public ValueTask RaiseAsync(string name, object argument, Parameters parameters, CancellationToken cancellation)
	{
		(var registry, var descriptor) = _locator(name);

		if(registry != null && descriptor != null)
			return registry.RaiseAsync(descriptor, registry.GetContext(descriptor.Name, argument, parameters), cancellation);

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var tasks = new Task[_channels.Count];

		for(int i = 0; i < _channels.Count; i++)
			tasks[i] = _channels[i].OpenAsync(this, cancellation).AsTask();

		if(tasks.Length > 0)
			return Task.WhenAll(tasks);

		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var tasks = new Task[_channels.Count];

		for(int i = 0; i < _channels.Count; i++)
			tasks[i] = _channels[i].CloseAsync(cancellation).AsTask();

		if(tasks.Length > 0)
			return Task.WhenAll(tasks);

		return Task.CompletedTask;
	}
	#endregion

	#region 私有方法
	private static (EventRegistryBase Registry, EventDescriptor Descriptor) Locate(string qualifiedName)
	{
		var descriptor = Events.GetEvent(qualifiedName, out var registry);
		return (registry, descriptor);
	}
	#endregion
}
