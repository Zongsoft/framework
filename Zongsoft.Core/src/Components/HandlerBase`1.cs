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

namespace Zongsoft.Components
{
	/// <summary>
	/// 表示处理程序的基类。
	/// </summary>
	/// <typeparam name="TArgument">处理程序的请求参数类型。</typeparam>
	public abstract class HandlerBase<TArgument> : IHandler<TArgument>, IHandler
	{
		#region 构造函数
		protected HandlerBase() { }
		#endregion

		#region 公共方法
		public virtual void Handle(TArgument argument, Collections.Parameters parameters = null)
		{
			var task = this.HandleAsync(argument, parameters, CancellationToken.None);

			if(task.IsCompletedSuccessfully)
				return;

			task.AsTask().GetAwaiter().GetResult();
		}

		public ValueTask HandleAsync(TArgument argument, CancellationToken cancellation = default) => this.OnHandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default)
		{
			if(parameters == null)
				return this.OnHandleAsync(argument, null, cancellation);
			else
				return this.OnHandleAsync(argument, parameters, cancellation);
		}
		#endregion

		#region 抽象方法
		protected abstract ValueTask OnHandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation);
		#endregion

		#region 显式实现
		ValueTask IHandler.HandleAsync(object argument, CancellationToken cancellation) => this.HandleAsync(this.Convert(argument), null, cancellation);
		ValueTask IHandler.HandleAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation) => this.HandleAsync(this.Convert(argument), parameters, cancellation);
		#endregion

		#region 参数转换
		protected virtual TArgument Convert(object argument)
		{
			if(argument == null)
				return default;

			return Common.Convert.TryConvertValue<TArgument>(argument, out var value) ? value : throw new ArgumentException($"The specified argument cannot be converted to '{typeof(TArgument).FullName}' type.", nameof(argument));
		}
		#endregion
	}
}
