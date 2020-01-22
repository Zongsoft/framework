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
	public class StateContext<TState> : StateContextBase where TState : State
	{
		#region 构造函数
		public StateContext(StateMachine machine, bool isFirst, TState origin, TState destination, IDictionary<string, object> parameters = null) : base(machine, isFirst, origin, destination, parameters)
		{
		}
		#endregion

		#region 公共属性
		public TState Origin
		{
			get
			{
				return (TState)base.InnerOrigin;
			}
		}

		public TState Destination
		{
			get
			{
				return (TState)base.InnerDestination;
			}
		}
		#endregion

		#region 公共方法
		public TDiagram GetDiagram<TDiagram>() where TDiagram : StateDiagramBase<TState>
		{
			return this.Destination.Diagram as TDiagram;
		}

		public void OnStopped(Action<StateContext<TState>, StateStopReason> thunk)
		{
			this.StoppedThunk = thunk;
		}

		public void OnStopping(Func<StateContext<TState>, StateStopReason, bool> thunk)
		{
			this.StoppingThunk = thunk;
		}
		#endregion
	}
}
