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
	/// <summary>
	/// 表示数据服务可变性的结构。
	/// </summary>
	public struct DataServiceMutability : IEquatable<DataServiceMutability>
	{
		#region 常量定义
		private const byte DELETABLE_VALUE  = 0x01;
		private const byte UPDATABLE_VALUE  = 0x02;
		private const byte INSERTABLE_VALUE = 0x04;
		private const byte UPSERTABLE_VALUE = 0x08;
		private const byte UPSERTABLE_FLAG  = 0x80;
		#endregion

		#region 成员字段
		private byte _value;
		#endregion

		#region 构造函数
		private DataServiceMutability(byte value) => _value = value;
		#endregion

		#region 公共属性
		/// <summary>获取或设置一个值，指示是否可删除。</summary>
		public bool Deletable
		{
			get => (_value & DELETABLE_VALUE) == DELETABLE_VALUE;
			set => _value |= DELETABLE_VALUE;
		}

		/// <summary>获取或设置一个值，指示是否可更新。</summary>
		public bool Updatable
		{
			get => (_value & UPDATABLE_VALUE) == UPDATABLE_VALUE;
			set => _value |= UPDATABLE_VALUE;
		}

		/// <summary>获取或设置一个值，指示是否可新增。</summary>
		public bool Insertable
		{
			get => (_value & INSERTABLE_VALUE) == INSERTABLE_VALUE;
			set => _value |= INSERTABLE_VALUE;
		}

		/// <summary>获取或设置一个值，指示是否可增改。</summary>
		public bool Upsertable
		{
			get => (_value & UPSERTABLE_FLAG) == UPSERTABLE_FLAG ? (_value & UPSERTABLE_VALUE) == UPSERTABLE_VALUE : this.Insertable && this.Updatable;
			set => _value |= (UPSERTABLE_VALUE | UPSERTABLE_FLAG);
		}
		#endregion

		#region 重写方法
		public bool Equals(DataServiceMutability other) => _value == other._value;
		public override bool Equals(object obj) => obj is DataServiceMutability other && this.Equals(other);
		public override int GetHashCode() => _value;
		public override string ToString() => _value == 0 ?
			"None" :
			$"{(this.Deletable ? nameof(this.Deletable) : null)}," +
			$"{(this.Updatable ? nameof(this.Updatable) : null)}," +
			$"{(this.Insertable ? nameof(this.Insertable) : null)}," +
			$"{(this.Upsertable ? nameof(this.Upsertable) : null)}";
		#endregion

		#region 符号重写
		public static bool operator ==(DataServiceMutability left, DataServiceMutability right) => left._value == right._value;
		public static bool operator !=(DataServiceMutability left, DataServiceMutability right) => left._value != right._value;
		#endregion

		#region 静态属性
		/// <summary>获取一个空的可变性，即没有任何可变性。</summary>
		public static DataServiceMutability None => new (0);

		/// <summary>获取一个具有全部的可变性，支持“删除”、“新增”、“更新”、“增改”。</summary>
		public static DataServiceMutability All => new (DELETABLE_VALUE | UPDATABLE_VALUE | INSERTABLE_VALUE | UPSERTABLE_VALUE);

		/// <summary>获取一个默认的可变性，支持“新增”和“更新”。</summary>
		public static DataServiceMutability Default => new (UPDATABLE_VALUE | INSERTABLE_VALUE);
		#endregion
	}
}
