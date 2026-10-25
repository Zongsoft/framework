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
public sealed class EventExchanger : WorkerBase, IEventReceiver
{
	#region 单例字段
	public static readonly EventExchanger Instance = new();
	#endregion

	#region 成员字段
	private Func<string, (EventRegistryBase Registry, EventDescriptor Descriptor)> _locator;
	private readonly IExecutor<EventContext> _executor;
	private readonly List<IEventChannel> _channels;
	private IEventDispatcher _dispatcher;
	#endregion

	#region 私有构造
	private EventExchanger()
	{
		_locator = Locate;
		_dispatcher = new EventDispatcher();
		_channels = new List<IEventChannel>();
		_executor = Executor.Build<EventContext>(this.SendAsync);
	}
	#endregion

	#region 公共属性
	/// <summary>获取事件交换通道集合。</summary>
	public ICollection<IEventChannel> Channels => _channels;

	/// <summary>获取或设置事件调度器；仅允许在停止时替换。</summary>
	/// <remarks>实现 <see cref="IWorker"/> 的调度器随交换器启停，停止完成后才关闭通道。</remarks>
	public IEventDispatcher Dispatcher
	{
		get => _dispatcher;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException();
			_dispatcher = value ?? throw new ArgumentNullException(nameof(value));
		}
	}

	/// <summary>获取或设置事件处理程序定位器。</summary>
	public Func<string, (EventRegistryBase Registry, EventDescriptor Descriptor)> Locator
	{
		get => _locator;
		set => _locator = value ?? Locate;
	}
	#endregion

	#region 公共方法
	/// <summary>通过调度器将事件交给远程投递执行器。</summary>
	/// <param name="context">待交换的事件上下文。</param>
	/// <param name="cancellation">取消本次事件等待及投递的令牌。</param>
	/// <returns>立即投递或延期接收的任务；交换器未运行时不执行。</returns>
	public ValueTask ExchangeAsync(EventContext context, CancellationToken cancellation)
	{
		if(this.State != WorkerState.Running)
			return ValueTask.CompletedTask;

		return _dispatcher.DispatchAsync(context, _executor, cancellation);
	}
	#endregion

	#region 显式实现
	/// <summary>定位事件注册表并执行本地处理程序，不再次向通道投递。</summary>
	/// <param name="name">事件的完整限定名称。</param>
	/// <param name="argumentType">发送方选择的已声明参数类型。</param>
	/// <param name="argument">事件实参。</param>
	/// <param name="parameters">事件参数集合。</param>
	/// <param name="cancellation">取消本地处理的令牌。</param>
	/// <returns>本地处理任务；事件不存在时不执行。</returns>
	ValueTask IEventReceiver.ReceiveAsync(string name, Type argumentType, object argument, Parameters parameters, CancellationToken cancellation)
	{
		(var registry, var descriptor) = _locator(name);

		if(registry != null && descriptor != null)
		{
			descriptor.Validate(argumentType, argument);
			return registry.RaiseAsync(descriptor, registry.GetContext(argumentType, descriptor.Name, argument, parameters), cancellation);
		}

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 重写方法
	/// <summary>打开事件通道，并启动支持工作器生命周期的调度器。</summary>
	/// <param name="args">传递给调度器的启动参数。</param>
	/// <param name="cancellation">启动操作的取消令牌。</param>
	/// <returns>通道与调度器就绪的任务。</returns>
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var tasks = new Task[_channels.Count];

		for(int i = 0; i < _channels.Count; i++)
			tasks[i] = _channels[i].OpenAsync(this, cancellation).AsTask();

		await Task.WhenAll(tasks).ConfigureAwait(false);
		if(_dispatcher is IWorker worker)
			await worker.StartAsync(args, cancellation).ConfigureAwait(false);
	}

	/// <summary>等待调度器停止后关闭事件通道。</summary>
	/// <param name="args">传递给调度器的停止参数。</param>
	/// <param name="cancellation">停止操作的取消令牌。</param>
	/// <returns>调度器与通道停止完成的任务。</returns>
	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		if(_dispatcher is IWorker worker)
			await worker.StopAsync(args, cancellation).ConfigureAwait(false);

		var tasks = new Task[_channels.Count];

		for(int i = 0; i < _channels.Count; i++)
			tasks[i] = _channels[i].CloseAsync(cancellation).AsTask();

		await Task.WhenAll(tasks).ConfigureAwait(false);
	}
	#endregion

	#region 私有方法
	/// <summary>将事件并发投递给已挂载的全部通道。</summary>
	/// <param name="context">待投递的事件上下文。</param>
	/// <param name="cancellation">投递操作的取消令牌。</param>
	/// <returns>全部通道完成投递的任务。</returns>
	private ValueTask SendAsync(EventContext context, CancellationToken cancellation)
	{
		var tasks = new Task[_channels.Count];

		for(var index = 0; index < tasks.Length; index++)
			tasks[index] = SendAsync(_channels[index], context, cancellation);

		return new(Task.WhenAll(tasks));

		static async Task SendAsync(IEventChannel channel, EventContext context, CancellationToken cancellation) =>
			await channel.SendAsync(context, cancellation).ConfigureAwait(false);
	}

	/// <summary>根据完整限定名查找事件及其注册表。</summary>
	/// <param name="qualifiedName">包含模块名的事件完整限定名。</param>
	/// <returns>事件所属的注册表及事件描述器。</returns>
	private static (EventRegistryBase Registry, EventDescriptor Descriptor) Locate(string qualifiedName)
	{
		var descriptor = Events.GetEvent(qualifiedName, out var registry);
		return (registry, descriptor);
	}
	#endregion
}
