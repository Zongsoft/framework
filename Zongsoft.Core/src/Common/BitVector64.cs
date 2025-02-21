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
	public struct BitVector64 : IEquatable<BitVector64>
	{
		#region 成员字段
		private long _data;
		#endregion

		#region 构造函数
		public BitVector64(int data) => _data = data;
		public BitVector64(long data) => _data = data;
		#endregion

		#region 公共属性
		public readonly long Data => _data;

		public bool this[long bit]
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
					long oldData = _data;
					long newData;

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
		public static implicit operator long(BitVector64 vector) => vector._data;
		public static implicit operator BitVector64(long data) => new(data);
		public static implicit operator BitVector64(BitVector32 vector) => new(vector.Data);
		#endregion

		#region 符号重写
		public static bool operator ==(BitVector64 left, BitVector64 right) => left._data == right._data;
		public static bool operator !=(BitVector64 left, BitVector64 right) => left._data != right._data;
		#endregion

		#region 重写方法
		public readonly bool Equals(BitVector32 other) => _data == other.Data;
		public readonly bool Equals(BitVector64 other) => _data == other._data;
		public override readonly bool Equals(object obj) => obj is BitVector64 other && this.Equals(other);
		public override readonly int GetHashCode() => _data.GetHashCode();
		public override readonly string ToString() => _data.ToString();
		#endregion
	}
}
