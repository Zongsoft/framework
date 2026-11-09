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
using System.Collections.Generic;

namespace Zongsoft.Components.States;

public interface IStateDiagram<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : struct
{
	#region 属性定义
	StateVector<TValue>[] Vectors { get; }
	#endregion

	#region 方法定义
	bool CanTransfer(StateVector<TValue> state) => this.CanTransfer(state.Source, state.Destination);
	bool CanTransfer(TValue source, TValue destination);
	ValueTask TransferAsync(IStateContext<TKey, TValue> context, IStateHandler<TKey, TValue> handler, CancellationToken cancellation = default);

	ValueTask<State<TKey, TValue>> GetStateAsync(TKey key, CancellationToken cancellation = default);
	ValueTask<bool> SetStateAsync(TKey key, TValue value, string description, CancellationToken cancellation = default) => this.SetStateAsync(key, value, description, null, cancellation);
	ValueTask<bool> SetStateAsync(TKey key, TValue value, string description, IDictionary<object, object> parameters, CancellationToken cancellation = default);
	ValueTask<bool> SetStateAsync(State<TKey, TValue> state, string description, CancellationToken cancellation = default) => this.SetStateAsync(state, description, null, cancellation);
	ValueTask<bool> SetStateAsync(State<TKey, TValue> state, string description, IDictionary<object, object> parameters, CancellationToken cancellation = default);
	#endregion
}
