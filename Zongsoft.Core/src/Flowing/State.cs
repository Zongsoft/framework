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
	public abstract class State<T> : IEquatable<State<T>> where T : struct, IEquatable<T>
	{
		#region 构造函数
		protected State(IStateDiagram<State<T>, T> diagram, T value, DateTime? timestamp, string description = null)
		{
			this.Diagram = diagram ?? throw new ArgumentNullException(nameof(diagram));
			this.Value = value;
			this.Timestamp = timestamp;
			this.Description = description;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前状态所属的<see cref="IStateDiagram{TState, T}"/>状态图。
		/// </summary>
		public IStateDiagram<State<T>, T> Diagram { get; }

		/// <summary>
		/// 获取或设置当前状态值。
		/// </summary>
		public T Value { get; set; }

		/// <summary>
		/// 获取或设置状态变更时间。
		/// </summary>
		public DateTime? Timestamp { get; set; }

		/// <summary>
		/// 获取或设置状态描述或变更说明。
		/// </summary>
		public string Description { get; set; }
		#endregion

		#region 抽象成员
		internal protected abstract bool Match(State<T> state);
		#endregion

		#region 重写方法
		public virtual bool Equals(State<T> state)
		{
			if(state == null)
				return false;

			return this.Match(state) && this.Value.Equals(state.Value);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return this.Equals((State<T>)obj);
		}
		#endregion
	}
}
