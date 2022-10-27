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
 * This file is part of Zongsoft.Externals.OpenXml library.
 *
 * The Zongsoft.Externals.OpenXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.OpenXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.OpenXml library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Buffers;

namespace Zongsoft.Externals.OpenXml.Spreadsheet
{
	public readonly struct CellAddress : IEquatable<CellAddress>
	{
		#region 公共字段
		public readonly int Row;
		public readonly int Column;
		#endregion

		#region 构造函数
		public CellAddress(string address)
		{
			if(string.IsNullOrEmpty(address))
			{
				this.Row = 0;
				this.Column = 0;
			}
			else
			{
				(var row, var column) = ParseCore(address, true);
				this.Row = row;
				this.Column = column;
			}
		}

		public CellAddress(int row, int column)
		{
			this.Row = row > 0 ? row : 0;
			this.Column = column > 0 ? column : 0;
		}
		#endregion

		#region 解析方法
		public static bool TryParse(string text, out CellAddress value)
		{
			(var row, var column) = ParseCore(text, false);

			if(row < 0 || column < 0)
			{
				value = default;
				return false;
			}

			value = new(row, column);
			return true;
		}

		public static CellAddress Parse(string text)
		{
			(var row, var column) = ParseCore(text, true);
			return new(row, column);
		}

		private static (int row, int column) ParseCore(string text, bool throwOnError = true)
		{
			if(string.IsNullOrEmpty(text))
				return default;

			var index = 0;
			var span = text.AsSpan();

			for(int i = 0; i < span.Length; i++)
			{
				var chr = span[i];

				if((chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z'))
				{
				}
				else if(chr >= '0' && chr <= '9')
				{
					index = i;
					break;
				}
				else
				{
				}
			}

			if(index < 1)
				;

			return default;
		}
		#endregion

		#region 重写方法
		public bool Equals(CellAddress other) => this.Row == other.Row && this.Column == other.Column;
		public override bool Equals(object obj) => obj is CellAddress other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Row, this.Column);
		public override string ToString() => GetColumnName(this.Column) + this.Row.ToString();
		#endregion

		#region 符号重写
		public static bool operator ==(CellAddress left, CellAddress right) => left.Equals(right);
		public static bool operator !=(CellAddress left, CellAddress right) => !(left == right);
		#endregion

		#region 私有方法
		private static string GetColumnName(int column)
		{
			if(column < 1)
				return "A";

			var round = 0;
			var value = column;
			var result = ArrayPool<char>.Shared.Rent(5);

			try
			{
				while(value > 26)
				{
					if(round >= result.Length - 1)
						throw new ArgumentOutOfRangeException(nameof(column));

					result[^++round] = (char)('A' + (value % 27));
					value /= 27;
				}

				result[^(round + 1)] = (char)('A' + value);
				return new string(result, result.Length - (round + 1), round + 1);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(result);
			}
		}
		#endregion
	}
}
