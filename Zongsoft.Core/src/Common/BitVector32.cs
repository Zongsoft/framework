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
using System.Threading;

namespace Zongsoft.Common
{
	public struct BitVector32 : IEquatable<BitVector32>
	{
		#region 成员字段
		private int _data;
		#endregion

		#region 构造函数
		public BitVector32(int data) => _data = data;
		#endregion

		#region 公共属性
		public int Data => _data;

		public bool this[int bit]
		{
			get
			{
				var data = _data;
				return (data & bit) == bit;
			}
			set
			{
				while(true)
				{
					var oldData = _data;
					int newData;

					if(value)
						newData = oldData | bit;
					else
						newData = oldData & ~bit;

					var result = Interlocked.CompareExchange(ref _data, newData, oldData);

					if(result == oldData)
						break;
				}
			}
		}
		#endregion

		#region 类型转换
		public static implicit operator int(BitVector32 vector) => vector._data;
		public static implicit operator BitVector32(int data) => new BitVector32(data);
		#endregion

		#region 符号重写
		public static bool operator ==(BitVector32 left, BitVector32 right) => left._data == right._data;
		public static bool operator !=(BitVector32 left, BitVector32 right) => left._data != right._data;
		#endregion

		#region 重写方法
		public bool Equals(BitVector32 other) => _data == other._data;
		public override bool Equals(object obj) => obj is BitVector32 other && this.Equals(other);
		public override int GetHashCode() => _data.GetHashCode();
		public override string ToString() => _data.ToString();
		#endregion
	}
}
