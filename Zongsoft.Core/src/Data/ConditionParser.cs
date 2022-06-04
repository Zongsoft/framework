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
using System.Collections.Generic;

namespace Zongsoft.Data
{
	internal static class ConditionParser
	{
		#region 公共方法
		public static bool TryParse(ReadOnlySpan<char> text, out ICondition result) => (result = Parse(text, 0, null)) != null;
		public static bool TryParse(string text, int start, int count, out ICondition result) => (result = Parse(text.AsSpan(start, count), 0, null)) != null;
		public static ICondition Parse(ReadOnlySpan<char> text) => Parse(text, 0, message => throw new InvalidOperationException(message));
		public static ICondition Parse(string text, int start, int count) => Parse(text.AsSpan(start, count), start, message => throw new InvalidOperationException(message));
		public static ICondition Parse(ReadOnlySpan<char> text, Action<string> onError) => Parse(text, 0, onError);
		private static ICondition Parse(ReadOnlySpan<char> text, int offset, Action<string> onError)
		{
			if(text.IsEmpty)
				return null;

			//创建解析上下文对象
			var context = new StateContext(text.Length, onError);

			//状态迁移驱动
			for(int i = 0; i < text.Length; i++)
			{
				context.Character = text[i];

			}

			//获取最终的解析结果
			return context.GetResult();
		}
		#endregion

		#region 嵌套结构
		private enum State
		{
			None,
			Gutter,
			Separator,
			Identifier,
			Indexer,
			Method,
			Parameter,
			String,
			Number,
		}

		private struct StateContext
		{
			#region 私有变量
			private readonly char[] _buffer;
			private readonly Action<string> _onError;
			#endregion

			#region 公共字段
			public State State;
			public char Character;
			public StateVector Flags;
			public ICondition Head;
			public readonly Stack<ICondition> Stack;
			#endregion

			#region 构造函数
			public StateContext(int length, Action<string> onError)
			{
				_onError = onError;
				_buffer = new char[length];

				this.Character = '\0';
				this.Head = null;
				this.Flags = new StateVector();
				this.State = State.None;
				this.Stack = new Stack<ICondition>();
			}
			#endregion

			public ICondition GetResult() => null;
		}

		private struct StateVector
		{
			#region 常量定义
			private const int STRING_ESCAPING_FLAG = 1; //字符串是否处于转移状态(0:普通态, 1:转移态)
			private const int STRING_QUOTATION_FLAG = 2; //字符串的引号是否为单引号(0:单引号, 1:双引号)

			private const int IDENTIFIER_ATTACHING_FLAG = 4; //标识表达式是否为附加到最后一个参数的标识表达式后面(0:新增参数, 1:附加参数)
			private const int IDENTIFIER_WHITESPACE_FLAG = 8; //标识表达式中间是否出现空白字符(0:没有, 1:有)

			private const int CONSTANT_TYPE_FLAG = 0x70; //常量的类型掩码范围
			private const int CONSTANT_TYPE_INT32_FLAG = 0x10; //32位整型数常量的类型值
			private const int CONSTANT_TYPE_INT64_FLAG = 0x20; //64位整型数常量的类型值
			private const int CONSTANT_TYPE_SINGLE_FLAG = 0x30; //单精度浮点数常量的类型值
			private const int CONSTANT_TYPE_DOUBLE_FLAG = 0x40; //双精度浮点数常量的类型值
			private const int CONSTANT_TYPE_DECIMAL_FLAG = 0x50; //Decimal 数常量的类型值
			#endregion

			#region 成员变量
			private int _data;
			#endregion

			#region 公共方法
			public bool IsEscaping()
			{
				return IsMarked(STRING_ESCAPING_FLAG);
			}

			public void IsEscaping(bool enabled)
			{
				Mark(STRING_ESCAPING_FLAG, enabled);
			}

			public bool IsAttaching()
			{
				return IsMarked(IDENTIFIER_ATTACHING_FLAG);
			}

			public void IsAttaching(bool enabled)
			{
				Mark(IDENTIFIER_ATTACHING_FLAG, enabled);
			}

			public bool HasWhitespace()
			{
				return IsMarked(IDENTIFIER_WHITESPACE_FLAG);
			}

			public void HasWhitespace(bool enabled)
			{
				Mark(IDENTIFIER_WHITESPACE_FLAG, enabled);
			}

			public TypeCode GetConstantType()
			{
				switch(_data & CONSTANT_TYPE_FLAG)
				{
					case CONSTANT_TYPE_INT32_FLAG:
						return TypeCode.Int32;
					case CONSTANT_TYPE_INT64_FLAG:
						return TypeCode.Int64;
					case CONSTANT_TYPE_SINGLE_FLAG:
						return TypeCode.Single;
					case CONSTANT_TYPE_DOUBLE_FLAG:
						return TypeCode.Double;
					case CONSTANT_TYPE_DECIMAL_FLAG:
						return TypeCode.Decimal;
					default:
						return TypeCode.String;
				}
			}

			public void SetConstantType(TypeCode type)
			{
				//首先，重置数字常量类型的比特区域
				Mark(CONSTANT_TYPE_FLAG, false);

				switch(type)
				{
					case TypeCode.Int32:
						Mark(CONSTANT_TYPE_INT32_FLAG, true);
						break;
					case TypeCode.Int64:
						Mark(CONSTANT_TYPE_INT64_FLAG, true);
						break;
					case TypeCode.Single:
						Mark(CONSTANT_TYPE_SINGLE_FLAG, true);
						break;
					case TypeCode.Double:
						Mark(CONSTANT_TYPE_DOUBLE_FLAG, true);
						break;
					case TypeCode.Decimal:
						Mark(CONSTANT_TYPE_DECIMAL_FLAG, true);
						break;
				}
			}

			public char GetStringQuote()
			{
				return IsMarked(STRING_QUOTATION_FLAG) ? '"' : '\'';
			}

			public void SetStringQuote(char chr)
			{
				Mark(STRING_QUOTATION_FLAG, chr == '"');
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private bool IsMarked(int bit)
			{
				return (_data & bit) == bit;
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private void Mark(int bit, bool value)
			{
				if(value)
					_data |= bit;
				else
					_data &= ~bit;
			}
			#endregion
		}
		#endregion
	}
}
