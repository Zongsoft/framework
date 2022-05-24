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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data
{
	public struct DataServiceWritability : IEquatable<DataServiceWritability>
	{
		#region 常量定义
		private const byte INSERTABLE_VALUE = 0x1;
		private const byte UPSERTABLE_VALUE = 0x2;
		private const byte UPDATABLE_VALUE = 0x4;
		private const byte DELETABLE_VALUE = 0x8;
		private const byte UPSERTABLE_FLAG = 0x80;
		#endregion

		#region 成员字段
		private byte _value;
		#endregion

		#region 公共属性
		public bool Deletable { get => (_value & DELETABLE_VALUE) == DELETABLE_VALUE; set => _value |= DELETABLE_VALUE; }
		public bool Updatable { get => (_value & UPDATABLE_VALUE) == UPDATABLE_VALUE; set => _value |= UPDATABLE_VALUE; }
		public bool Insertable { get => (_value & INSERTABLE_VALUE) == INSERTABLE_VALUE; set => _value |= INSERTABLE_VALUE; }
		public bool Upsertable
		{
			get => (_value & UPSERTABLE_FLAG) == UPSERTABLE_FLAG ? (_value & UPSERTABLE_VALUE) == UPSERTABLE_VALUE : this.Insertable && this.Updatable;
			set => _value |= (UPSERTABLE_VALUE | UPSERTABLE_FLAG);
		}
		#endregion

		#region 重写方法
		public bool Equals(DataServiceWritability other) => _value == other._value;
		public override bool Equals(object obj) => obj is DataServiceWritability other && this.Equals(other);
		public override int GetHashCode() => _value;
		public override string ToString() => _value == 0 ?
			"None" :
			$"{(this.Deletable ? nameof(this.Deletable) : null)}," +
			$"{(this.Updatable ? nameof(this.Updatable) : null)}," +
			$"{(this.Insertable ? nameof(this.Insertable) : null)}," +
			$"{(this.Upsertable ? nameof(this.Upsertable) : null)}";
		#endregion

		#region 符号重写
		public static bool operator ==(DataServiceWritability left, DataServiceWritability right) => left.Equals(right);
		public static bool operator !=(DataServiceWritability left, DataServiceWritability right) => !(left == right);
		#endregion

		#region 静态属性
		public static DataServiceWritability None => new();

		public static DataServiceWritability All => new ()
		{
			Deletable = true,
			Updatable = true,
			Upsertable = true,
			Insertable = true,
		};

		public static DataServiceWritability Default => new()
		{
			Insertable = true,
			Updatable = true,
			Deletable = false,
		};

		public static DataServiceWritability InsertOnly => new()
		{
			Insertable = true,
		};
		#endregion
	}
}
