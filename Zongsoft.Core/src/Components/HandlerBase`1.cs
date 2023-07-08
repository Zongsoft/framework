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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Components
{
	/// <summary>
	/// 表示处理程序的基类。
	/// </summary>
	/// <typeparam name="TRequest">处理程序的请求参数类型。</typeparam>
	public abstract class HandlerBase<TRequest> : IHandler<TRequest>, IHandler
	{
		#region 构造函数
		protected HandlerBase() { }
		#endregion

		#region 公共方法
		public virtual bool CanHandle(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters = null) => request != null;
		public virtual void Handle(object caller, TRequest request, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			var task = this.HandleAsync(caller, request, parameters, CancellationToken.None);

			if(task.IsCompletedSuccessfully)
				return;

			task.AsTask().GetAwaiter().GetResult();
		}

		public ValueTask HandleAsync(object caller, TRequest request, CancellationToken cancellation = default) => this.OnHandleAsync(caller, request, null, cancellation);
		public ValueTask HandleAsync(object caller, TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(parameters == null)
				return this.OnHandleAsync(caller, request, null, cancellation);
			else
				return this.OnHandleAsync(caller, request, parameters is IDictionary<string, object> dictionary ? dictionary : new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase), cancellation);
		}
		#endregion

		#region 抽象方法
		protected abstract ValueTask OnHandleAsync(object caller, TRequest request, IDictionary<string, object> parameters, CancellationToken cancellation);
		#endregion

		#region 显式实现
		bool IHandler.CanHandle(object request, IEnumerable<KeyValuePair<string, object>> parameters) => this.CanHandle(this.Convert(request), parameters);
		ValueTask IHandler.HandleAsync(object caller, object request, CancellationToken cancellation) => this.HandleAsync(caller, this.Convert(request), null, cancellation);
		ValueTask IHandler.HandleAsync(object caller, object request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => this.HandleAsync(caller, this.Convert(request), parameters, cancellation);
		#endregion

		#region 参数转换
		protected virtual TRequest Convert(object request)
		{
			if(request == null)
				return default;

			return request is TRequest result ? result : throw new ArgumentException($"The specified request parameter cannot be converted to '{typeof(TRequest).FullName}' type.", nameof(request));
		}
		#endregion
	}
}
