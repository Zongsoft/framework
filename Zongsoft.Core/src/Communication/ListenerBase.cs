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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Communication;

/// <summary>提供工作器生命周期和泛型实参派发的监听器基类。</summary>
/// <typeparam name="T">处理器实参类型，与传输层的线协议包类型相互独立。</typeparam>
/// <remarks>注入的处理器由提供者拥有；未配置处理器时完成空处理，仍检查取消。</remarks>
public abstract class ListenerBase<T> : WorkerBase, IListener<T>, IHandleable<T>, IHandleable
{
	#region 成员字段
	private IHandler<T> _handler;
	#endregion

	#region 构造函数
	protected ListenerBase(string name) : base(name) { }
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示工作器处于 <see cref="WorkerState.Running" /> 状态。</summary>
	public virtual bool IsListening => this.State == WorkerState.Running;

	/// <summary>获取或设置共享处理器，不同连接可能并发调用该处理器。</summary>
	public IHandler<T> Handler
	{
		get => Volatile.Read(ref _handler);
		set => Volatile.Write(ref _handler, value);
	}

	/// <summary>获取或设置与监听实参类型匹配的共享处理器。</summary>
	IHandler IHandleable.Handler
	{
		get => this.Handler;
		set => this.Handler = value == null ? null : value as IHandler<T> ?? throw new ArgumentException(string.Format(Properties.Resources.Communication_ListenerHandlerMismatch_Message, value));
	}
	#endregion

	#region 处理方法
	/// <summary>检查取消并派发实参，不取得处理器的释放所有权。</summary>
	/// <param name="argument">处理器实参。</param>
	/// <param name="cancellation">处理取消令牌。</param>
	/// <returns>处理完成操作；调用方必须恰好消费一次。</returns>
	protected virtual ValueTask OnHandleAsync(T argument, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		return this.Handler?.HandleAsync(argument, cancellation) ?? ValueTask.CompletedTask;
	}

	/// <summary>通过泛型入口派发实参，不复制或额外分配参数容器。</summary>
	/// <param name="package">待派发或编解码的报文。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>恰好消费一次的处理操作。</returns>
	ValueTask IListener<T>.HandleAsync(T package, CancellationToken cancellation) => this.OnHandleAsync(package, cancellation);
	#endregion
}
