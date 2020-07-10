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
using System.Collections.Generic;

namespace Zongsoft.Flowing
{
	public abstract class StateDiagramBase<TState, T> : IStateDiagram<T> where T : struct where TState : State<T>
	{
		#region 构造函数
		protected StateDiagramBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public IServiceProvider ServiceProvider { get; }

		public StateVector<T>[] Vectors { get; protected set; }
		#endregion

		#region 公共方法
		public void Transfer(IStateContext<T> context, IStateHandler<T> handler)
		{
			this.OnTransfering(context);

			if(handler.Enabled)
				handler.Handle(context);

			this.OnTransferred(context);
		}
		#endregion

		#region 虚拟方法
		protected virtual bool CanTransfer(T origin, T destination)
		{
			var vectors = this.Vectors;

			if(vectors != null && vectors.Length > 0)
			{
				for(int i = 0; i < vectors.Length; i++)
				{
					var vector = vectors[i];

					if(vector.Origin.Equals(origin) && vector.Destination.Equals(destination))
						return true;
				}
			}

			return false;
		}

		protected virtual void OnTransfering(IStateContext<T> context)
		{
		}

		protected virtual void OnTransferred(IStateContext<T> context)
		{
		}
		#endregion

		#region 抽象方法
		protected abstract TState GetState(TState state);
		protected abstract bool SetState(TState state, IDictionary<object, object> parameters);
		#endregion

		#region 显式实现
		bool IStateDiagram<T>.CanTransfer(T origin, T destination)
		{
			return this.CanTransfer(origin, destination);
		}

		State<T> IStateDiagram<T>.GetState(State<T> state)
		{
			return this.GetState(state as TState ?? throw new ArgumentException($"The specified the State type is not ‘{typeof(TState).FullName}’ type."));
		}

		bool IStateDiagram<T>.SetState(State<T> state, IDictionary<object, object> parameters)
		{
			return this.SetState(state as TState ?? throw new ArgumentException($"The specified the State type is not ‘{typeof(TState).FullName}’ type."), parameters);
		}
		#endregion
	}
}
