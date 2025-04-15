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

namespace Zongsoft.Common;

/// <summary>
/// 表示层次向量的结构。
/// </summary>
public readonly struct HierarchyVector32 : IEquatable<HierarchyVector32>, IEquatable<uint>
{
	#region 静态字段
	/// <summary>获取为数值为零的层次向量。</summary>
	public static readonly HierarchyVector32 Zero = new(0);
	#endregion

	#region 公共字段
	/// <summary>获取层次向量的层级，基数为1。</summary>
	public readonly byte Depth;

	/// <summary>获取层次向量的数值。</summary>
	public readonly uint Value;

	/// <summary>获取层次向量的数据归属范围起始值。</summary>
	public readonly uint Minimum;

	/// <summary>获取层次向量的数据归属范围截止值。</summary>
	public readonly uint Maximum;
	#endregion

	#region 构造函数
	public HierarchyVector32(byte part1, byte part2, byte part3, byte part4)
	{
		this.Value = ((uint)part1 << 24) + ((uint)part2 << 16) + ((uint)part3 << 8) + part4;
		this.Depth = 0;
		this.Minimum = 0;
		this.Maximum = 0;

		if(part4 > 0)
		{
			this.Depth = 4;
			this.Minimum = this.Value;
			this.Maximum = this.Value;
			return;
		}

		if(part3 > 0)
		{
			this.Depth = 3;
			this.Minimum = ((uint)part1 << 24) + ((uint)part2 << 16) + ((uint)part3 << 8);
			this.Maximum = this.Minimum + 0xFF;
			return;
		}

		if(part2 > 0)
		{
			this.Depth = 2;
			this.Minimum = ((uint)part1 << 24) + ((uint)part2 << 16);
			this.Maximum = this.Minimum + 0xFF_FF;
			return;
		}

		if(part1 > 0)
		{
			this.Depth = 1;
			this.Minimum = (uint)part1 << 24;
			this.Maximum = this.Minimum + 0xFF_FF_FF;
			return;
		}
	}

	public HierarchyVector32(uint value)
	{
		this.Value = value;
		this.Depth = 0;
		this.Minimum = 0;
		this.Maximum = 0;

		if((value & 0x00_00_00_FF) > 0)
		{
			this.Depth = 4;
			this.Minimum = value;
			this.Maximum = value;
			return;
		}

		if((value & 0x00_00_FF_00) > 0)
		{
			this.Depth = 3;
			this.Minimum = value & 0xFF_FF_FF_00;
			this.Maximum = this.Minimum + 0xFF;
			return;
		}

		if((value & 0x00_FF_00_00) > 0)
		{
			this.Depth = 2;
			this.Minimum = value & 0xFF_FF_00_00;
			this.Maximum = this.Minimum + 0xFF_FF;
			return;
		}

		if((value & 0xFF_00_00_00) > 0)
		{
			this.Depth = 1;
			this.Minimum = value & 0xFF_00_00_00;
			this.Maximum = this.Minimum + 0xFF_FF_FF;
			return;
		}
	}
	#endregion

	#region 公共属性
	public bool IsZero => this.Value == 0;
	#endregion

	#region 静态方法
	public static bool Contains(uint owner, uint child)
	{
		if(owner == 0 || owner == child)
			return true;

		if(child == 0)
			return false;

		if((owner & 0xFF_00_00_00) == (child & 0xFF_00_00_00))
		{
			var part = (byte)(owner & 0xFF_00_00);

			if(part == 0)
				return true;

			if(part == (child & 0xFF_00_00))
			{
				part = (byte)(owner & 0xFF_00);

				if(part == 0)
					return true;

				if(part == (child & 0xFF_00))
				{
					part = (byte)(owner & 0xFF);

					if(part == 0)
						return true;

					return part == (child & 0xFF);
				}
			}
		}

		return false;
	}

	public static bool IsChild(uint parent, uint child)
	{
		if(parent == child)
			return false;

		var parentLevel = GetDepth(parent);
		var childLevel = GetDepth(child);

		if(parentLevel == childLevel - 1)
		{
			switch(parentLevel)
			{
				case 0:
					return true;
				case 1:
					return parent == (child & 0xFF_00_00_00);
				case 2:
					return parent == (child & 0xFF_FF_00_00);
				case 3:
					return parent == (child & 0XFF_FF_FF_00);
			}
		}

		return false;
	}

	public static int GetDepth(uint value)
	{
		if(value == 0)
			return 0;

		var level = 0;

		if((value & 0xFF_00_00_00) > 0)
		{
			level = 1;
		}

		if((value & 0x00_FF_00_00) > 0)
		{
			if(level < 1)
				return -1;

			level = 2;
		}

		if((value & 0x00_00_FF_00) > 0)
		{
			if(level < 2)
				return -1;

			level = 3;
		}

		if((value & 0x00_00_00_FF) > 0)
		{
			if(level < 3)
				return -1;

			level = 4;
		}

		return level;
	}

	public static uint GetParent(uint value, out int level)
	{
		if(value == 0)
		{
			level = 0;
			return 0;
		}

		level = -1;
		uint parent = 0;

		if((value & 0xFF_00_00_00) > 0)
		{
			level = 1;
		}

		if((value & 0x00_FF_00_00) > 0)
		{
			if(level < 1)
			{
				level = -1;
				return 0;
			}

			level = 2;
			parent = value & 0xFF_00_00_00;
		}

		if((value & 0x00_00_FF_00) > 0)
		{
			if(level < 2)
			{
				level = -1;
				return 0;
			}

			level = 3;
			parent = value & 0xFF_FF_00_00;
		}

		if((value & 0x00_00_00_FF) > 0)
		{
			if(level < 3)
			{
				level = -1;
				return 0;
			}

			level = 4;
			parent = value & 0xFF_FF_FF_00;
		}

		return parent;
	}

	public static uint[] GetAncestors(uint value, bool include = false)
	{
		if(value == 0)
			return include ? [0u] : [];

		int length = 0;
		uint ancestor1 = 0, ancestor2 = 0, ancestor3 = 0;

		if((value & 0x00_00_00_FF) > 0)
		{
			length = 3;
			ancestor3 = value & 0xFF_FF_FF_00;
		}

		if((value & 0x00_00_FF_00) > 0)
		{
			if(length < 2)
				length = 2;

			ancestor2 = value & 0xFF_FF_00_00;
		}
		else if(length > 2)
			return include ? [0u, value] : [0u];

		if((value & 0x00_FF_00_00) > 0)
		{
			if(length < 1)
				length = 1;

			ancestor1 = value & 0xFF_00_00_00;
		}
		else if(length > 1)
			return include ? [0u, value] : [0u];

		return length switch
		{
			1 => include ? [0u, ancestor1, value] : [0u, ancestor1],
			2 => include ? [0u, ancestor1, ancestor2, value] : [0u, ancestor1, ancestor2],
			3 => include ? [0u, ancestor1, ancestor2, ancestor3, value] : [0u, ancestor1, ancestor2, ancestor3],
			_ => include ? [0u, value] : [0u],
		};
	}
	#endregion

	#region 重写方法
	public bool Equals(uint value) => value == this.Value;
	public bool Equals(HierarchyVector32 vector) => vector.Value == this.Value;
	public override bool Equals(object obj) => (obj is HierarchyVector32 vector && vector.Value == this.Value) || (obj is uint number && this.Value == number);
	public override int GetHashCode() => HashCode.Combine(this.Value);
	public override string ToString() => this.Value == 0 ? "0" : $"{this.Depth}:{this.Value:X}({this.Minimum:X8}~{this.Maximum:X8})";
	#endregion

	#region 符号重写
	public static bool operator ==(HierarchyVector32 a, HierarchyVector32 b) => a.Value == b.Value;
	public static bool operator !=(HierarchyVector32 a, HierarchyVector32 b) => a.Value != b.Value;
	public static explicit operator HierarchyVector32(uint value) => value == 0 ? HierarchyVector32.Zero : new HierarchyVector32(value);
	public static explicit operator uint(HierarchyVector32 value) => value.Value;
	#endregion
}
