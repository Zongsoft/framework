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

public interface IStateMachine : IAsyncDisposable
{
	bool HasParameters { get; }
	IDictionary<object, object> Parameters { get; }
	IStateHandlerProvider Handlers { get; set; }

	ValueTask RunAsync<TKey, TValue>(State<TKey, TValue> state, string description, CancellationToken cancellation = default) where TKey : struct, IEquatable<TKey> where TValue : struct;
	ValueTask RunAsync<TKey, TValue>(State<TKey, TValue> state, string description, IEnumerable<KeyValuePair<object, object>> parameters, CancellationToken cancellation = default) where TKey : struct, IEquatable<TKey> where TValue : struct;
}
