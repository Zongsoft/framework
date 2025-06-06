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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Collections;

namespace Zongsoft.Components;

public static class Handler
{
	#region 同步处理
	public static IHandler<TArgument> Handle<TArgument>(Action<TArgument> handle) => new HandlerProxy<TArgument>(handle);
	public static IHandler<TArgument> Handle<TArgument>(Action<TArgument, Parameters> handle) => new HandlerProxy<TArgument>(handle);
	public static IHandler<TArgument, TResult> Handle<TArgument, TResult>(Func<TArgument, TResult> handle) => new HandlerProxy<TArgument, TResult>(handle);
	public static IHandler<TArgument, TResult> Handle<TArgument, TResult>(Func<TArgument, Parameters, TResult> handle) => new HandlerProxy<TArgument, TResult>(handle);
	#endregion

	#region 异步处理
	public static IHandler<TArgument> Handle<TArgument>(Func<TArgument, CancellationToken, ValueTask> handle) => new HandlerProxy<TArgument>(handle);
	public static IHandler<TArgument> Handle<TArgument>(Func<TArgument, Parameters, CancellationToken, ValueTask> handle) => new HandlerProxy<TArgument>(handle);
	public static IHandler<TArgument, TResult> Handle<TArgument, TResult>(Func<TArgument, CancellationToken, ValueTask<TResult>> handle) => new HandlerProxy<TArgument, TResult>(handle);
	public static IHandler<TArgument, TResult> Handle<TArgument, TResult>(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> handle) => new HandlerProxy<TArgument, TResult>(handle);
	#endregion

	#region 嵌套子类
	private sealed class HandlerProxy<TArgument> : HandlerBase<TArgument>
	{
		private readonly Func<TArgument, Parameters, CancellationToken, ValueTask> _handler;

		public HandlerProxy(Action<TArgument> handler)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			_handler = (argument, _, _) =>
			{
				handler(argument);
				return ValueTask.CompletedTask;
			};
		}

		public HandlerProxy(Action<TArgument, Parameters> handler)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			_handler = (argument, parameters, _) =>
			{
				handler(argument, parameters);
				return ValueTask.CompletedTask;
			};
		}

		public HandlerProxy(Func<TArgument, CancellationToken, ValueTask> handler)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			_handler = (argument, _, cancellation) => handler(argument, cancellation);
		}

		public HandlerProxy(Func<TArgument, Parameters, CancellationToken, ValueTask> handler)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		protected override ValueTask OnHandleAsync(TArgument argument, Parameters parameters, CancellationToken cancellation) => _handler(argument, parameters, cancellation);
	}

	private sealed class HandlerProxy<TArgument, TResult> : HandlerBase<TArgument, TResult>
	{
		private readonly Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> _handler;

		public HandlerProxy(Func<TArgument, TResult> handler)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			_handler = (argument, _, _) =>
			{
				var result = handler(argument);
				return ValueTask.FromResult(result);
			};
		}

		public HandlerProxy(Func<TArgument, Parameters, TResult> handler)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			_handler = (argument, parameters, _) =>
			{
				var result = handler(argument, parameters);
				return ValueTask.FromResult(result);
			};
		}

		public HandlerProxy(Func<TArgument, CancellationToken, ValueTask<TResult>> handler)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			_handler = (argument, _, cancellation) => handler(argument, cancellation);
		}

		public HandlerProxy(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> handler)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		protected override ValueTask<TResult> OnHandleAsync(TArgument argument, Parameters parameters, CancellationToken cancellation) => _handler(argument, parameters, cancellation);
	}
	#endregion
}
