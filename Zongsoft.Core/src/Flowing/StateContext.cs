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
	public class StateContext<T> : IStateContext<T> where T : struct, IEquatable<T>
	{
		#region 构造函数
		public StateContext(IStateMachine machine, State<T> origin, State<T> destination)
		{
			this.StateMachine = machine ?? throw new ArgumentNullException(nameof(machine));
			this.Origin = origin;
			this.Destination = destination;
		}
		#endregion

		#region 公共属性
		public IStateMachine StateMachine { get; }
		public State<T> Origin { get; }
		public State<T> Destination { get; }

		public bool HasParameters => this.StateMachine.HasParameters;
		public IDictionary<object, object> Parameters => this.StateMachine.Parameters;
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return this.Origin.ToString() + "->" + this.Destination.ToString();
		}
		#endregion
	}
}
