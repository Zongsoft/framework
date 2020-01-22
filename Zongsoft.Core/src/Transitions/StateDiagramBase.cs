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

namespace Zongsoft.Transitions
{
	public abstract class StateDiagramBase<TState> : IStateDiagram where TState : State
	{
		#region 成员字段
		private ICollection<IStateTrigger> _triggers;
		#endregion

		#region 构造函数
		protected StateDiagramBase()
		{
			_triggers = new List<IStateTrigger>();
		}
		#endregion

		#region 公共属性
		public ICollection<IStateTrigger> Triggers
		{
			get
			{
				return _triggers;
			}
		}
		#endregion

		#region 公共方法
		public bool Transfer(StateContextBase context)
		{
			return this.OnTransfer(context as StateContext<TState>);
		}
		#endregion

		#region 显式实现
		void IStateDiagram.Failed(StateContextBase context)
		{
			this.OnFailed(context as StateContext<TState>);
		}

		bool IStateDiagram.CanVectoring(State origin, State destination)
		{
			return this.CanVectoring(origin as TState, destination as TState);
		}

		State IStateDiagram.GetState(State state)
		{
			return this.GetState(state as TState);
		}

		bool IStateDiagram.SetState(State state, IDictionary<string, object> parameters)
		{
			return this.SetState(state as TState, parameters);
		}
		#endregion

		#region 虚拟方法
		protected virtual bool OnTransfer(StateContext<TState> context)
		{
			return true;
		}

		protected virtual void OnFailed(StateContext<TState> context)
		{
			throw new InvalidOperationException($"Not supported state transfer from '{context.Origin}' to '{context.Destination}'.");
		}
		#endregion

		#region 抽象方法
		protected abstract bool CanVectoring(TState origin, TState destination);

		protected abstract TState GetState(TState state);
		protected abstract bool SetState(TState state, IDictionary<string, object> parameters);
		#endregion
	}
}
