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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Buffers;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Components;

partial struct Identifier
{
	#region 私有字段
	private static readonly Type[] _stringKeyTypes = [typeof(string)];
	#endregion

	#region 解析入口
	internal static bool TryParseCore(ReadOnlySpan<char> text, Type[] expected, out Identifier result)
	{
		result = default;
		if(text.IsEmpty)
			return true;

		try
		{
			var context = new ParseContext(text, expected);

			while(context.Move())
			{
				switch(context.State)
				{
					case ParseState.Scope:
						DoScope(ref context);
						break;
					case ParseState.Types:
						DoTypes(ref context);
						break;
					case ParseState.ValueStart:
						DoValueStart(ref context);
						break;
					case ParseState.Value:
						DoValue(ref context);
						break;
					case ParseState.Code:
					case ParseState.Label:
					case ParseState.Alias:
						DoMetadata(ref context);
						break;
					case ParseState.Description:
						DoDescription(ref context);
						break;
					case ParseState.DescriptionEnd:
						DoDescriptionEnd(ref context);
						break;
					case ParseState.PropertyName:
						DoPropertyName(ref context);
						break;
					case ParseState.PropertyValue:
						DoPropertyValue(ref context);
						break;
					default:
						context.Fail();
						break;
				}

				if(context.State == ParseState.Error)
					return false;
			}

			return Complete(ref context, out result);
		}
		catch(Exception exception) when(exception is FormatException or ArgumentException or OverflowException or InvalidOperationException or NotSupportedException)
		{
			return false;
		}
	}
	#endregion

	#region 状态处理
	private static void DoScope(ref ParseContext context)
	{
		if(context.Character is '<' or '[')
			context.Depth++;
		if(context.Character is '>' or ']')
			context.Depth--;

		if(context.Depth < 0)
		{
			context.Fail();
			return;
		}

		if(context.Depth == 0 && context.Character == ':')
		{
			var scope = context.Take();

			if(scope.StartsWith('$'))
			{
				context.Type = ResolveType(scope[1..]);

				if(context.Type == null)
				{
					context.Fail();
					return;
				}
			}
			else
			{
				if(!IsName(scope))
				{
					context.Fail();
					return;
				}

				context.Name = scope;
			}

			context.State = ParseState.ValueStart;
		}
		else
			context.Accept();
	}

	private static void DoTypes(ref ParseContext context)
	{
		if(context.Depth == 0 && context.Character is ',' or '>')
		{
			if(context.DeclaredCount == 5)
			{
				context.Fail();
				return;
			}

			var type = ResolveType(context.Take());
			if(type == null || !CanConvert(type))
			{
				context.Fail();
				return;
			}

			context.Declared[context.DeclaredCount++] = type;
			if(context.Character == '>')
				BeginValue(ref context);
		}
		else
		{
			if(context.Character is '<' or '[')
				context.Depth++;
			if(context.Character is '>' or ']')
				context.Depth--;

			if(context.Depth < 0)
				context.Fail();
			else
				context.Accept();
		}
	}

	private static void DoValueStart(ref ParseContext context)
	{
		if(context.Character == '<')
			context.Reset(ParseState.Types);
		else
		{
			BeginValue(ref context);

			if(context.State == ParseState.Value)
				DoValue(ref context);
		}
	}

	private static void DoValue(ref ParseContext context)
	{
		if(context.Character == '\\')
		{
			context.AcceptEscape();
			return;
		}

		if(context.Character == context.Separator)
		{
			if(!SaveValue(ref context) || context.ValueCount >= context.Types.Length)
				context.Fail();
			return;
		}

		if(context.Character is '#' or '?' or '(' or '{')
		{
			if(!SaveValue(ref context) || context.ValueCount != context.Types.Length)
			{
				context.Fail();
				return;
			}

			context.State = context.Character switch { '#' => ParseState.Code, '?' => ParseState.Label, '(' => ParseState.Description, _ => ParseState.PropertyName };
		}
		else if(context.Character is '|' or '}' or ')' or '<' or '>')
			context.Fail();
		else
			context.Accept();
	}

	private static void DoMetadata(ref ParseContext context)
	{
		if(context.Character == '\\')
		{
			context.AcceptEscape();
			return;
		}

		if(context.Character is '(' or '{' || context.Character == '?' && context.State == ParseState.Code || context.Character == '|' && context.State == ParseState.Label)
		{
			SaveMetadata(ref context);
			context.State = context.Character switch { '?' => ParseState.Label, '|' => ParseState.Alias, '(' => ParseState.Description, _ => ParseState.PropertyName };
		}
		else if(context.Character is '#' or '?' or '|' or ')' or '}' or '<' or '>')
			context.Fail();
		else
			context.Accept();
	}

	private static void DoDescription(ref ParseContext context)
	{
		if(context.Character == '\\')
		{
			context.AcceptEscape();
			return;
		}

		if(context.Character == ')')
		{
			context.Description = context.Take();
			context.State = ParseState.DescriptionEnd;
		}
		else if(context.Character is '#' or '?' or '|' or '(' or '{' or '}' or '<' or '>')
			context.Fail();
		else
			context.Accept();
	}

	private static void DoDescriptionEnd(ref ParseContext context)
	{
		if(context.Character == '{')
			context.Reset(ParseState.PropertyName);
		else
			context.Fail();
	}

	private static void DoPropertyName(ref ParseContext context)
	{
		if(context.Character == '\\')
		{
			context.AcceptEscape();
			return;
		}

		if(context.Character == '=')
		{
			context.Property = context.Take();
			context.State = ParseState.PropertyValue;
		}
		else if(context.Character == '}' && context.IsEmpty && context.Properties == null)
			context.State = ParseState.Ending;
		else if(context.Character is '{' or '}' or ';')
			context.Fail();
		else
			context.Accept();
	}

	private static void DoPropertyValue(ref ParseContext context)
	{
		if(context.Character == '\\')
		{
			context.AcceptEscape();
			return;
		}

		if(context.Character is ';' or '}')
		{
			context.Properties ??= new(StringComparer.OrdinalIgnoreCase);

			if(!context.Properties.TryAdd(context.Property, context.Take()))
			{
				context.Fail();
				return;
			}

			context.State = context.Character == ';' ? ParseState.PropertyName : ParseState.Ending;
		}
		else if(context.Character is '{' or '=')
			context.Fail();
		else
			context.Accept();
	}
	#endregion

	#region 辅助方法
	private static void BeginValue(ref ParseContext context)
	{
		context.Types = GetTypes(ref context, out var separator);

		if(!Matches(context.Types, context.Expected))
		{
			context.Fail();
			return;
		}

		context.Separator = context.Types.Length == 1 ? '\0' : separator;

		//单键直接保存结果，复合键只分配创建元组所需的参数数组。
		if(context.Types.Length > 1)
			context.Values = new object[context.Types.Length];

		context.State = ParseState.Value;

		static Type[] GetTypes(ref ParseContext context, out char separator)
		{
			separator = ',';

			//类型选择依次使用显式声明、登记规则、泛型参数和默认字符串。
			if(context.DeclaredCount > 0)
				return context.Declared[..context.DeclaredCount].ToArray();

			var rule = Formatting.Find(context.Name, context.Type);
			if(rule != null)
			{
				separator = rule.Separator;
				return rule.Types;
			}

			var expected = context.Expected;
			if(expected == null)
				return _stringKeyTypes;

			if(expected.Length == 1 && expected[0].IsAssignableFrom(typeof(string)))
				return _stringKeyTypes;

			return expected;
		}

		static bool Matches(Type[] types, Type[] expected)
		{
			if(expected == null)
				return true;

			if(expected.Length == 1)
				return types.Length == 1 && expected[0].IsAssignableFrom(types[0]);

			return expected.AsSpan().SequenceEqual(types);
		}
	}

	private static bool SaveValue(ref ParseContext context)
	{
		if(context.ValueCount >= context.Types.Length)
			return false;

		var type = context.Types[context.ValueCount];
		var value = ParseValue(context.Take(), type);
		if(value == null || !type.IsInstanceOfType(value))
			return false;

		if(context.Values == null)
			context.Value = value;
		else
			context.Values[context.ValueCount] = value;

		context.ValueCount++;
		return true;
	}

	private static void SaveMetadata(ref ParseContext context)
	{
		switch(context.State)
		{
			case ParseState.Code:
				context.Code = context.Take();
				break;
			case ParseState.Label:
				context.Label = context.Take();
				break;
			case ParseState.Alias:
				context.Alias = context.Take();
				break;
		}
	}

	private static bool Complete(ref ParseContext context, out Identifier result)
	{
		result = default;

		if(context.State == ParseState.ValueStart)
			BeginValue(ref context);

		switch(context.State)
		{
			case ParseState.Value:
				if(!SaveValue(ref context))
					return false;
				break;
			case ParseState.Code:
			case ParseState.Label:
			case ParseState.Alias:
				SaveMetadata(ref context);
				break;
			case ParseState.DescriptionEnd:
			case ParseState.Ending:
				break;
			default:
				return false;
		}

		if(context.Types == null || context.ValueCount != context.Types.Length)
			return false;

		var value = context.Values == null ? context.Value : CreateValue(context.Types, context.Values);
		result = new Identifier(context.Name, context.Type, value, context.Label, context.Description, context.Alias, context.Code, context.Properties, false);
		return true;
	}

	private static string Unescape(ReadOnlySpan<char> text)
	{
		char[] rented = null;
		var buffer = text.Length <= 256 ? stackalloc char[text.Length] : (rented = ArrayPool<char>.Shared.Rent(text.Length));

		try
		{
			var count = 0;

			for(int i = 0; i < text.Length; i++)
			{
				var character = text[i];

				if(character == '\\')
				{
					character = text[++i] switch { 'n' => '\n', 'r' => '\r', 't' => '\t', var value => value };

					if(text[i] == 'u')
					{
						character = (char)ushort.Parse(text.Slice(i + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
						i += 4;
					}
				}

				buffer[count++] = character;
			}

			return new string(buffer[..count]);
		}
		finally
		{
			if(rented != null)
				ArrayPool<char>.Shared.Return(rented, true);
		}
	}
	#endregion

	#region 嵌套类型
	private enum ParseState
	{
		Scope,
		Types,
		ValueStart,
		Value,
		Code,
		Label,
		Alias,
		Description,
		DescriptionEnd,
		PropertyName,
		PropertyValue,
		Ending,
		Error,
	}

	[InlineArray(5)]
	private struct ParseTypes
	{
		private Type _element;
	}

	private ref struct ParseContext
	{
		#region 私有字段
		private readonly ReadOnlySpan<char> _text;
		private int _position;
		private int _start;
		private int _length;
		private bool _escaped;
		#endregion

		#region 公共字段
		public readonly Type[] Expected;
		public ParseState State;
		public ParseTypes Declared;
		public int DeclaredCount;
		public int ValueCount;
		public int Depth;
		public Type[] Types;
		public object Value;
		public object[] Values;
		public Dictionary<string, string> Properties;
		public string Property;
		public string Name;
		public Type Type;
		public string Code;
		public string Label;
		public string Alias;
		public string Description;
		public char Separator;
		#endregion

		#region 构造函数
		public ParseContext(ReadOnlySpan<char> text, Type[] expected)
		{
			this = default;
			_text = text;
			this.Expected = expected;
		}
		#endregion

		#region 公共属性
		public char Character { get; private set; }
		public readonly bool IsEmpty => _length == 0;
		#endregion

		#region 公共方法
		public bool Move()
		{
			if(_position >= _text.Length)
				return false;

			this.Character = _text[_position++];
			if(char.IsControl(this.Character))
				this.Fail();

			return true;
		}

		public void Fail() => this.State = ParseState.Error;
		public void Accept() => _length++;
		public void Reset(ParseState state)
		{
			this.State = state;
			_start = _position;
			_length = 0;
			_escaped = false;
		}

		public string Take()
		{
			var text = _text.Slice(_start, _length);
			var result = _escaped ? Unescape(text) : text.ToString();
			this.Reset(this.State);
			return result;
		}

		public void AcceptEscape()
		{
			if(!this.Move())
			{
				this.Fail();
				return;
			}

			if(this.Character == 'u')
			{
				if(_position + 4 > _text.Length || !ushort.TryParse(_text.Slice(_position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _))
				{
					this.Fail();
					return;
				}

				_position += 4;
				_length += 4;
			}
			else if(this.Character is not ('n' or 'r' or 't') && !"\\:,?|#{};=()$<>-/.".Contains(this.Character))
			{
				this.Fail();
				return;
			}

			_length += 2;
			_escaped = true;
		}
		#endregion
	}
	#endregion
}
