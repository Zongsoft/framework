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
	public abstract class StateDiagramBase<TKey, TValue> : IStateDiagram<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : struct
	{
		#region 构造函数
		protected StateDiagramBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public IServiceProvider ServiceProvider { get; }

		public StateVector<TValue>[] Vectors { get; protected set; }
		#endregion

		#region 公共方法
		public void Transfer(IStateContext<TKey, TValue> context, IStateHandler<TKey, TValue> handler)
		{
			this.OnTransfering(context);
			this.OnTransfer(context, handler);
			this.OnTransferred(context);
		}
		#endregion

		#region 虚拟方法
		protected virtual bool CanTransfer(TValue source, TValue destination)
		{
			var vectors = this.Vectors;

			if(vectors != null && vectors.Length > 0)
			{
				for(int i = 0; i < vectors.Length; i++)
				{
					var vector = vectors[i];

					if(vector.Source.Equals(source) && vector.Destination.Equals(destination))
						return true;
				}
			}

			return false;
		}

		protected virtual void OnTransfer(IStateContext<TKey, TValue> context, IStateHandler<TKey, TValue> handler)
		{
			handler.Handle(context);
		}

		protected virtual void OnTransfering(IStateContext<TKey, TValue> context)
		{
		}

		protected virtual void OnTransferred(IStateContext<TKey, TValue> context)
		{
		}
		#endregion

		#region 抽象方法
		protected abstract State<TKey, TValue> GetState(TKey key);
		protected abstract bool SetState(TKey key, TValue value, string description, IDictionary<object, object> parameters);
		protected virtual bool SetState(State<TKey, TValue> state, string description, IDictionary<object, object> parameters)
		{
			if(state == null)
				throw new ArgumentNullException(nameof(state));

			return this.SetState(state.Key, state.Value, description, parameters);
		}
		#endregion

		#region 显式实现
		bool IStateDiagram<TKey, TValue>.CanTransfer(TValue source, TValue destination)
		{
			return this.CanTransfer(source, destination);
		}

		State<TKey, TValue> IStateDiagram<TKey, TValue>.GetState(TKey key)
		{
			return this.GetState(key);
		}

		bool IStateDiagram<TKey, TValue>.SetState(TKey key, TValue value, string description, IDictionary<object, object> parameters)
		{
			return this.SetState(key, value, description, parameters);
		}

		bool IStateDiagram<TKey, TValue>.SetState(State<TKey, TValue> state, string description, IDictionary<object, object> parameters)
		{
			if(state == null)
				throw new ArgumentNullException(nameof(state));

			return this.SetState(state, description, parameters);
		}
		#endregion
	}
}
