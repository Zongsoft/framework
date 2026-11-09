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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components.States;

public abstract class StateHandlerBase<TKey, TValue> : IStateHandler<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : struct
{
	#region 构造函数
	protected StateHandlerBase(IServiceProvider serviceProvider)
	{
		this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}
	#endregion

	#region 公共属性
	public IServiceProvider ServiceProvider { get; }
	#endregion

	#region 抽象方法
	protected abstract ValueTask OnHandleAsync(StateContext<TKey, TValue> context, CancellationToken cancellation);
	#endregion

	#region 虚拟方法
	protected virtual async ValueTask OnFinishAsync(StateContext<TKey, TValue> context, CancellationToken cancellation) => await context.SetStateAsync(cancellation);
	#endregion

	#region 显式实现
	ValueTask IStateHandler<TKey, TValue>.HandleAsync(IStateContext<TKey, TValue> context, CancellationToken cancellation)
	{
		return this.OnHandleAsync(context as StateContext<TKey, TValue> ?? throw new InvalidOperationException(Properties.Resources.StateHandler_InvalidContext_Message), cancellation);
	}

	ValueTask IStateHandler<TKey, TValue>.FinishAsync(IStateContext<TKey, TValue> context, CancellationToken cancellation)
	{
		return this.OnFinishAsync(context as StateContext<TKey, TValue> ?? throw new InvalidOperationException(Properties.Resources.StateHandler_InvalidContext_Message), cancellation);
	}
	#endregion
}
