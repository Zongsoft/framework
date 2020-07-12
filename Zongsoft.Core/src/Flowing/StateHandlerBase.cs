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

namespace Zongsoft.Flowing
{
	public abstract class StateHandlerBase<TKey, TValue> : IStateHandler<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : struct
	{
		#region 构造函数
		protected StateHandlerBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public IServiceProvider ServiceProvider
		{
			get;
		}
		#endregion

		#region 抽象方法
		protected abstract void OnHandle(StateContext<TKey, TValue> context);
		protected abstract void OnFinish(StateContext<TKey, TValue> context);
		#endregion

		#region 显式实现
		void IStateHandler<TKey, TValue>.Handle(IStateContext<TKey, TValue> context)
		{
			this.OnHandle(context as StateContext<TKey, TValue> ?? throw new InvalidOperationException($"Invalid type of the state context."));
		}

		void IStateHandler<TKey, TValue>.Finish(IStateContext<TKey, TValue> context)
		{
			this.OnFinish(context as StateContext<TKey, TValue> ?? throw new InvalidOperationException($"Invalid type of the state context."));
		}
		#endregion
	}
}
