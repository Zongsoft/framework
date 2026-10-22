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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Runtime.CompilerServices;

namespace Zongsoft.Data;

internal sealed class DataAsyncEnumerable<T> : IAsyncEnumerable<T>, IPageable
{
	#region 事件定义
	public event Func<object, PagingEventArgs, CancellationToken, ValueTask> Paginated
	{
		add
		{
			if(value == null)
				return;

			lock(_sync)
			{
				if(_paginated == null && _pageable != null)
					_pageable.Paginated += this.OnPaginatedAsync;

				_paginated += value;
			}
		}
		remove
		{
			lock(_sync)
			{
				_paginated -= value;

				if(_paginated == null && _pageable != null)
					_pageable.Paginated -= this.OnPaginatedAsync;
			}
		}
	}
	#endregion

	#region 成员字段
	private readonly object _sync = new();
	private int _prepared;
	private IPageable _pageable;
	private readonly bool _suppressed;
	private readonly CancellationToken _cancellation;
	private readonly Task<IAsyncEnumerable<T>> _preparation;
	private Func<object, PagingEventArgs, CancellationToken, ValueTask> _paginated;
	#endregion

	#region 构造函数
	public DataAsyncEnumerable(ValueTask<IAsyncEnumerable<T>> preparation, CancellationToken cancellation, bool suppressed)
	{
		_cancellation = cancellation;
		_suppressed = suppressed;
		_preparation = this.PrepareAsync(preparation);

		//即使调用方从未枚举结果，也观察准备阶段的失败。
		_ = _preparation.ContinueWith(
			static task => _ = task.Exception,
			CancellationToken.None,
			TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
			TaskScheduler.Default);
	}
	#endregion

	#region 公共属性
	public bool Suppressed => Volatile.Read(ref _pageable)?.Suppressed ?? (Volatile.Read(ref _prepared) != 0 || _suppressed);
	#endregion

	#region 公共方法
	public static IAsyncEnumerable<T> From(ValueTask<IAsyncEnumerable<T>> preparation, CancellationToken cancellation, Paging paging)
	{
		//仅提取已经完成的结果；未完成的操作由结果包装器异步等待。
		if(preparation.IsCompleted)
			return preparation.Result;

		return new DataAsyncEnumerable<T>(preparation, cancellation, paging == null || !paging.IsPaged());
	}

	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => this.EnumerateAsync(cancellation).GetAsyncEnumerator();
	#endregion

	#region 私有方法
	private async ValueTask OnPaginatedAsync(object sender, PagingEventArgs args, CancellationToken cancellation)
	{
		Func<object, PagingEventArgs, CancellationToken, ValueTask> handlers;

		lock(_sync)
			handlers = _paginated;

		if(handlers != null)
		{
			foreach(Func<object, PagingEventArgs, CancellationToken, ValueTask> handler in handlers.GetInvocationList())
				await handler(this, args, cancellation);
		}
	}

	private async Task<IAsyncEnumerable<T>> PrepareAsync(ValueTask<IAsyncEnumerable<T>> preparation)
	{
		var result = await preparation.ConfigureAwait(false);

		if(result is IPageable pageable)
		{
			lock(_sync)
			{
				Volatile.Write(ref _pageable, pageable);

				if(_paginated != null)
					pageable.Paginated += this.OnPaginatedAsync;
			}
		}

		Volatile.Write(ref _prepared, 1);
		return result;
	}

	private async IAsyncEnumerable<T> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellation)
	{
		using var source = _cancellation.CanBeCanceled && cancellation.CanBeCanceled && _cancellation != cancellation ?
			CancellationTokenSource.CreateLinkedTokenSource(_cancellation, cancellation) : null;
		var token = source?.Token ?? (_cancellation.CanBeCanceled ? _cancellation : cancellation);

		token.ThrowIfCancellationRequested();
		var result = await _preparation.WaitAsync(token).ConfigureAwait(false);

		await foreach(var item in result.WithCancellation(token).ConfigureAwait(false))
			yield return item;
	}
	#endregion
}
