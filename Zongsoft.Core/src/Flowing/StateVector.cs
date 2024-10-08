﻿/*
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
	public readonly struct StateVector<T> : IEquatable<StateVector<T>> where T : struct
	{
		#region 构造函数
		public StateVector(T source, T destination)
		{
			this.Source = source;
			this.Destination = destination;
		}
		#endregion

		#region 公共字段
		public readonly T Source;
		public readonly T Destination;
		#endregion

		#region 公共方法
		public bool Contains(T value)
		{
			return this.Source.Equals(value) || this.Destination.Equals(value);
		}
		#endregion

		#region 重写方法
		public bool Equals(StateVector<T> other)
		{
			return this.Source.Equals(other.Source) &&
			       this.Destination.Equals(other.Destination);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return this.Equals((StateVector<T>)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.Source, this.Destination);
		}

		public override string ToString()
		{
			return this.Source.ToString() + "->" + this.Destination.ToString();
		}
		#endregion
	}
}
