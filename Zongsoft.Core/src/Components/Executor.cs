/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Collections.Generic;

namespace Zongsoft.Components
{
	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class Executor : Executor<ExecutorContext>
	{
		#region 重写方法
		protected override IHandler GetHandler(ExecutorContext context)
		{
			const string KYE_NAME = "Name";
			const string KEY_HANDLER = "Handler";

			if(context.HasParameters && (context.Parameters.TryGetValue(KYE_NAME, out var value) || context.Parameters.TryGetValue(KEY_HANDLER, out value)))
			{
				if(value is string name)
					return this.Handlers.TryGetValue(name, out var handler) ? handler : null;
				else if(value is IHandler handler)
					return handler;
			}

			return null;
		}
		#endregion
	}

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class Executor<TContext> : ExecutorBase<TContext> where TContext : IExecutorContext
	{
		#region 成员字段
		private readonly Func<Executor<TContext>, TContext, IHandler> _locator;
		#endregion

		#region 构造函数
		protected Executor()
		{
			this.Handler = new ExecutorHandler(this, GetHandler);
			this.Handlers = new Dictionary<string, IHandler>(StringComparer.OrdinalIgnoreCase);
		}

		public Executor(Func<Executor<TContext>, TContext, IHandler> locator)
		{
			_locator = locator;
			this.Handler = new ExecutorHandler(this, GetHandler);
			this.Handlers = new Dictionary<string, IHandler>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public IDictionary<string, IHandler> Handlers { get; }
		#endregion

		#region 虚拟方法
		protected virtual IHandler GetHandler(TContext context) => _locator?.Invoke(this, context);
		#endregion

		#region 嵌套子类
		private class ExecutorHandler : HandlerBase<TContext>
		{
			private readonly Executor<TContext> _executor;
			private readonly Func<TContext, IHandler> _locator;

			public ExecutorHandler(Executor<TContext> executor, Func<TContext, IHandler> locator)
			{
				_executor = executor ?? throw new ArgumentNullException(nameof(executor));
				_locator = locator ?? throw new ArgumentNullException(nameof(locator));
			}

			public override bool CanHandle(TContext request, IEnumerable<KeyValuePair<string, object>> parameters)
			{
				return base.CanHandle(request) && _locator(request) != null;
			}

			protected override async ValueTask OnHandleAsync(object caller, TContext request, IDictionary<string, object> parameters, CancellationToken cancellation)
			{
				var handler = _locator(request) ?? throw Common.OperationException.Unfound($"Unable to locate the handler based on current request.");
				await _executor.ExecuteAsync(handler, request, cancellation);
			}
		}
		#endregion
	}
}