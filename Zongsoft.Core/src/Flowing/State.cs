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

namespace Zongsoft.Flowing
{
	public abstract class State<TKey, TValue> : IEquatable<State<TKey, TValue>> where TKey : struct, IEquatable<TKey> where TValue : struct
	{
		#region 构造函数
		protected State(IStateDiagram<TKey, TValue> diagram, TKey key, TValue value)
		{
			this.Diagram = diagram ?? throw new ArgumentNullException(nameof(diagram));
			this.Key = key;
			this.Value = value;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前状态所属的<see cref="IStateDiagram{TKey, TValue}"/>状态图。
		/// </summary>
		public IStateDiagram<TKey, TValue> Diagram { get; }

		/// <summary>
		/// 获取当前状态的键值。
		/// </summary>
		public TKey Key { get; }

		/// <summary>
		/// 获取或设置当前状态值。
		/// </summary>
		public TValue Value { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(State<TKey, TValue> state)
		{
			return state != null && this.Key.Equals(state.Key) && this.Value.Equals(state.Value);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return this.Equals((State<TKey, TValue>)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.Key, this.Value);
		}

		public override string ToString()
		{
			return this.Key.ToString() + ":" + this.Value.ToString();
		}
		#endregion
	}
}
