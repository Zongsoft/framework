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
	/// <summary>
	/// 表示单元格地址的结构。
	/// </summary>
	public readonly struct CellAddress : IEquatable<CellAddress>
	{
		#region 公共字段
		private readonly int _row;
		private readonly int _column;
		#endregion

		#region 构造函数
		/// <summary>构建单元格地址结构。</summary>
		/// <param name="address">指定的单元格地址文本，譬如：<c>A1</c>、<c>AC100</c>。</param>
		public CellAddress(string address)
		{
			if(string.IsNullOrEmpty(address))
			{
				_row = 0;
				_column = 0;
			}
			else
			{
				(var row, var column) = ParseCore(address, true);
				_row = row;
				_column = column;
			}
		}

		/// <summary>构建单元格地址结构。</summary>
		/// <param name="row">指定的单元格行号(基于<c>1</c>)。</param>
		/// <param name="column">指定的单元格列号(基于<c>1</c>)。</param>
		public CellAddress(int row, int column)
		{
			_row = row > 0 ? row - 1 : 0;
			_column = column > 0 ? column - 1: 0;
		}
		#endregion

		#region 公共属性
		/// <summary>获取单元格的行号（基于<c>1</c>）。</summary>
		public int Row => _row + 1;

		/// <summary>获取单元格的列号（基于<c>1</c>）。</summary>
		public int Column => _column + 1;
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
		public bool Equals(CellAddress other) => _row == other._row && _column == other._column;
		public override bool Equals(object obj) => obj is CellAddress other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(_row, _column);
		public override string ToString() => $"{GetColumnName(_column)}{(_row + 1)}";
		#endregion

		#region 符号重写
		public static bool operator ==(CellAddress left, CellAddress right) => left.Equals(right);
		public static bool operator !=(CellAddress left, CellAddress right) => !(left == right);

		public static implicit operator string (CellAddress address) => $"{GetColumnName(address._column)}{(address._row + 1)}";
		public static implicit operator CellAddress(string address) => Parse(address);
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
				while(value > 25)
				{
					if(round >= result.Length - 1)
						throw new ArgumentOutOfRangeException(nameof(column));

					result[^++round] = (char)('A' + (value % 26));
					value = (value / 26) - 1;
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
