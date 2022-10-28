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
		#region 常量定义
		private const string INVALID_FORMAT_MESSAGE = @"Invalid cell address format.";
		#endregion

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
			{
				if(throwOnError)
					throw new ArgumentNullException(nameof(text));

				return default;
			}

			int row = -1, column = -1;
			var context = new StateContext(text);

			while(context.Move())
			{
				switch(context.State)
				{
					case State.None:
						DoNone(ref context);
						break;
					case State.Column:
						if(DoColumn(ref context, out var value))
						{
							if(!TryGetColumn(value, out column))
							{
								if(throwOnError)
									throw new ArgumentException($"The specified '{value.ToString()}' is an invalid column name.");

								return (-1, -1);
							}
						}

						break;
					case State.Row:
						if(DoRow(ref context, out value))
						{
							if(!int.TryParse(value, out row))
							{
								if(throwOnError)
									throw new ArgumentException($"The specified '{value.ToString()}' is an invalid row number.");

								return (-1, -1);
							}
						}

						break;
					case State.Prefix:
						DoPrefix(ref context);
						break;
					case State.Suffix:
						DoSuffix(ref context);
						break;
				}

				if(context.HasError(out var message))
				{
					if(throwOnError)
						throw new ArgumentException(message);

					return (-1, -1);
				}
			}

			if(context.Final(out var span) == State.Row)
			{
				if(!int.TryParse(span, out row))
				{
					if(throwOnError)
						throw new ArgumentException($"The specified '{span.ToString()}' is an invalid row number.");

					return (-1, -1);
				}
			}

			return (row, column);

			#region 状态转换
			static bool DoNone(ref StateContext context)
			{
				if(context.IsLetter())
					return context.Accept(State.Column);
				if(context.IsWhitespace())
					return context.Accept(State.Prefix);

				return context.Error(INVALID_FORMAT_MESSAGE);
			}

			static bool DoColumn(ref StateContext context, out ReadOnlySpan<char> value)
			{
				if(context.IsLetter())
					return context.Accept(out value);
				if(context.IsDigit())
					return context.Accept(State.Row, out value);

				value = default;
				return context.Error(INVALID_FORMAT_MESSAGE);
			}

			static bool DoRow(ref StateContext context, out ReadOnlySpan<char> value)
			{
				if(context.IsDigit())
					return context.Accept(out value);
				if(context.IsWhitespace())
					return context.Accept(State.Suffix, out value);

				value = default;
				return context.Error(INVALID_FORMAT_MESSAGE);
			}

			static bool DoPrefix(ref StateContext context)
			{
				if(context.IsLetter())
					return context.Accept(State.Column);
				if(context.IsWhitespace())
					return context.Accept();

				return context.Error(INVALID_FORMAT_MESSAGE);
			}

			static bool DoSuffix(ref StateContext context)
			{
				return context.IsWhitespace() ? context.Accept() : context.Error(INVALID_FORMAT_MESSAGE);
			}
			#endregion
		}

		private static bool TryGetColumn(ReadOnlySpan<char> name, out int value)
		{
			value = 0;

			for(int i = 0; i < name.Length; i++)
			{
				var chr = name[^(i + 1)];

				if(chr >= 'A' && chr <= 'Z')
					value += (chr - 'A') + (int)Math.Pow(26, i);
				else if(chr >= 'a' && chr <= 'z')
					value += (chr - 'a') + (int)Math.Pow(26, i);
				else
					return false;
			}

			return true;
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

		#region 嵌套结构
		private enum State
		{
			None,
			Row,
			Column,
			Prefix,
			Suffix,
		}

		private ref struct StateContext
		{
			private int _index;
			private int _position;
			private string _error;
			private State _state;
			private ReadOnlySpan<char> _text;

			public StateContext(string text)
			{
				_text = text.AsSpan();
				_index = 0;
				_position = -1;
				_error = null;
				_state = State.None;
			}

			public State State => _state;
			public int Position => _position;

			public bool Accept() => false;
			public bool Accept(out ReadOnlySpan<char> value)
			{
				value = default;
				return false;
			}
			public bool Accept(State state)
			{
				if(_state == state)
					return false;

				_index = _position;
				_state = state;
				return true;
			}
			public bool Accept(State state, out ReadOnlySpan<char> value)
			{
				value = default;

				if(_state == state)
					return false;

				if(_index < _position)
					value = _text.Slice(_index, _position - _index);

				_index = _position;
				_state = state;
				return true;
			}

			public State Final(out ReadOnlySpan<char> value)
			{
				if(_index < _position)
					value = _text.Slice(_index, Math.Min(_position, _text.Length) - _index);
				else
					value = default;

				return _state;
			}

			public bool Error(string message)
			{
				var hasError = !string.IsNullOrEmpty(_error);
				_error = message;
				return hasError;
			}

			public bool HasError(out string message)
			{
				message = _error;
				return !string.IsNullOrEmpty(message);
			}

			public bool Move() => ++_position < _text.Length;
			public bool IsDigit() => char.IsDigit(_text[_position]);
			public bool IsLetter() => char.IsLetter(_text[_position]);
			public bool IsWhitespace() => char.IsWhiteSpace(_text[_position]);
		}
		#endregion
	}
}
