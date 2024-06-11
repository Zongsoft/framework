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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Text;

namespace Zongsoft.Data
{
	public readonly struct SchemaMemberMultiplex : IEquatable<SchemaMemberMultiplex>
	{
		#region 构造函数
		public SchemaMemberMultiplex(int count, params Sorting[] sortings)
		{
			this.Count = count;
			this.Sortings = sortings;
		}
		#endregion

		#region 公共字段
		/// <summary>获取成员对应的记录数量。</summary>
		public readonly int Count;

		/// <summary>获取成员对应的子集排序设置。</summary>
		public readonly Sorting[] Sortings;
		#endregion

		#region 重写方法
		public bool Equals(SchemaMemberMultiplex other) => this.Count == other.Count && Enumerable.SequenceEqual(this.Sortings, other.Sortings);
		public override bool Equals(object obj) => obj is SchemaMemberMultiplex other && this.Equals(other);

		public override int GetHashCode()
		{
			if(this.Sortings == null || this.Sortings.Length == 0)
				return this.Count.GetHashCode();

			var result = new HashCode();
			result.Add(this.Count);

			for(int i = 0; i < this.Sortings.Length; i++)
			{
				result.Add(this.Sortings[i]);
			}

			return result.ToHashCode();
		}

		public override string ToString()
		{
			static string GetSortings(Sorting[] sortings)
			{
				if(sortings == null || sortings.Length == 0)
					return string.Empty;

				var text = new StringBuilder();

				for(int i = 0; i < sortings.Length; i++)
				{
					if(text.Length > 0)
						text.Append(',');

					text.Append(sortings[i].ToString());
				}

				return text.ToString();
			}

			return this.Count == 0 ? GetSortings(this.Sortings) : this.Count.ToString() + GetSortings(this.Sortings);
		}
		#endregion

		#region 符号重写
		public static bool operator ==(SchemaMemberMultiplex left, SchemaMemberMultiplex right) => left.Equals(right);
		public static bool operator !=(SchemaMemberMultiplex left, SchemaMemberMultiplex right) => !(left == right);
		#endregion
	}
}