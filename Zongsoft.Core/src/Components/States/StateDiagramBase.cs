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
using System.Collections.Generic;

namespace Zongsoft.Components.States;

public abstract class StateDiagramBase<TKey, TValue> : IStateDiagram<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : struct
{
	#region 构造函数
	protected StateDiagramBase(IServiceProvider serviceProvider)
	{
		this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}
	#endregion

	#region 公共属性
	public IServiceProvider ServiceProvider { get; }
	public StateVector<TValue>[] Vectors { get; protected set; }
	#endregion

	#region 公共方法
	public async ValueTask TransferAsync(IStateContext<TKey, TValue> context, IStateHandler<TKey, TValue> handler, CancellationToken cancellation = default)
	{
		await this.OnTransferingAsync(context, cancellation);
		await this.OnTransferAsync(context, handler, cancellation);
		await this.OnTransferredAsync(context, cancellation);
	}
	#endregion

	#region 虚拟方法
	protected virtual bool CanTransfer(TValue source, TValue destination)
	{
		var vectors = this.Vectors;

		if(vectors != null && vectors.Length > 0)
		{
			for(int i = 0; i < vectors.Length; i++)
			{
				var vector = vectors[i];

				if(vector.Source.Equals(source) && vector.Destination.Equals(destination))
					return true;
			}
		}

		return false;
	}

	protected virtual ValueTask OnTransferAsync(IStateContext<TKey, TValue> context, IStateHandler<TKey, TValue> handler, CancellationToken cancellation) => handler.HandleAsync(context, cancellation);
	protected virtual ValueTask OnTransferingAsync(IStateContext<TKey, TValue> context, CancellationToken cancellation) => ValueTask.CompletedTask;
	protected virtual ValueTask OnTransferredAsync(IStateContext<TKey, TValue> context, CancellationToken cancellation) => ValueTask.CompletedTask;
	#endregion

	#region 抽象方法
	protected abstract ValueTask<State<TKey, TValue>> GetStateAsync(TKey key, CancellationToken cancellation);
	protected abstract ValueTask<bool> SetStateAsync(TKey key, TValue value, string description, IDictionary<object, object> parameters, CancellationToken cancellation);
	protected virtual ValueTask<bool> SetStateAsync(State<TKey, TValue> state, string description, IDictionary<object, object> parameters, CancellationToken cancellation)
	{
		ArgumentNullException.ThrowIfNull(state);
		return this.SetStateAsync(state.Key, state.Value, description, parameters, cancellation);
	}
	#endregion

	#region 显式实现
	bool IStateDiagram<TKey, TValue>.CanTransfer(TValue source, TValue destination) => this.CanTransfer(source, destination);
	ValueTask<State<TKey, TValue>> IStateDiagram<TKey, TValue>.GetStateAsync(TKey key, CancellationToken cancellation) => this.GetStateAsync(key, cancellation);
	ValueTask<bool> IStateDiagram<TKey, TValue>.SetStateAsync(TKey key, TValue value, string description, IDictionary<object, object> parameters, CancellationToken cancellation) => this.SetStateAsync(key, value, description, parameters, cancellation);
	ValueTask<bool> IStateDiagram<TKey, TValue>.SetStateAsync(State<TKey, TValue> state, string description, IDictionary<object, object> parameters, CancellationToken cancellation)
	{
		ArgumentNullException.ThrowIfNull(state);
		return this.SetStateAsync(state, description, parameters, cancellation);
	}
	#endregion
}
