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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components
{
	public abstract class HandlerBase<TRequest> : IHandler<TRequest>, IHandler
	{
		#region 构造函数
		protected HandlerBase() { }
		#endregion

		#region 公共方法
		public virtual bool CanHandle(TRequest request) => request != null;
		public virtual object Handle(object caller, TRequest request) => this.HandleAsync(caller, request, CancellationToken.None).GetAwaiter().GetResult();
		public abstract ValueTask<object> HandleAsync(object caller, TRequest request, CancellationToken cancellation = default);
		#endregion

		#region 显式实现
		bool IHandler.CanHandle(object request) => request is TRequest model ? this.CanHandle(model) : false;
		ValueTask<object> IHandler.HandleAsync(object caller, object request, CancellationToken cancellation) => this.HandleAsync(caller, this.Convert(request), cancellation);
		#endregion

		#region 参数转换
		protected TRequest Convert(object request) => request is TRequest result ? result : throw new ArgumentException($"The specified request parameter cannot be converted to '{typeof(TRequest).FullName}' type.", nameof(request));
		#endregion
	}
}
