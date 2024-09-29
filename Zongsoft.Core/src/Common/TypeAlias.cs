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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Common
{
	public static class TypeAlias
	{
		private static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
		{
			{ nameof(Object), typeof(object) },
			{ nameof(DBNull), typeof(DBNull) },
			{ nameof(String), typeof(string) },
			{ nameof(Boolean), typeof(bool) },
			{ nameof(Guid), typeof(Guid) },
			{ nameof(Char), typeof(char) },
			{ nameof(Byte), typeof(byte) },
			{ nameof(SByte), typeof(sbyte) },
			{ nameof(Int16), typeof(short) },
			{ nameof(UInt16), typeof(ushort) },
			{ nameof(Int32), typeof(int) },
			{ nameof(UInt32), typeof(uint) },
			{ nameof(Int64), typeof(long) },
			{ nameof(UInt64), typeof(ulong) },
			{ nameof(Single), typeof(float) },
			{ nameof(Double), typeof(double) },
			{ nameof(Decimal), typeof(decimal) },
			{ nameof(TimeSpan), typeof(TimeSpan) },
			{ nameof(DateOnly), typeof(DateOnly) },
			{ nameof(TimeOnly), typeof(TimeOnly) },
			{ nameof(DateTime), typeof(DateTime) },
			{ nameof(DateTimeOffset), typeof(DateTimeOffset) },

			{ "void", typeof(void) },
			{ "bool", typeof(bool) },
			{ "uuid", typeof(Guid) },
			{ "float", typeof(float) },
			{ "short", typeof(short) },
			{ "ushort", typeof(ushort) },
			{ "int", typeof(int) },
			{ "integer", typeof(int) },
			{ "uint", typeof(uint) },
			{ "long", typeof(long) },
			{ "ulong", typeof(ulong) },
			{ "money", typeof(decimal) },
			{ "currency", typeof(decimal) },
			{ "date", typeof(DateOnly) },
			{ "time", typeof(TimeOnly) },
			{ "timestamp", typeof(DateTimeOffset) },
			{ "binary", typeof(byte[]) },
			{ "range", typeof(Zongsoft.Data.Range<>) },
			{ "mixture", typeof(Zongsoft.Data.Mixture<>) },

			{ nameof(IList), typeof(IList<>) },
			{ nameof(IEnumerable), typeof(IEnumerable<>) },
			{ nameof(ICollection), typeof(ICollection<>) },
			{ nameof(IDictionary), typeof(IDictionary<,>) },

			{ "List", typeof(List<>) },
			{ "ISet", typeof(ISet<>) },
			{ "Hashset", typeof(HashSet<>) },
			{ "Dictionary", typeof(Dictionary<,>) },

			{ "IReadOnlySet", typeof(IReadOnlySet<>) },
			{ "IReadOnlyList", typeof(IReadOnlyList<>) },
			{ "IReadOnlyCollection", typeof(IReadOnlyCollection<>) },
			{ "IReadOnlyDictionary", typeof(IReadOnlyDictionary<,>) },
		};

		public static Type Parse(string typeName, bool ignoreCase = true)
		{
			if(string.IsNullOrEmpty(typeName))
				return null;

			if(Types.TryGetValue(typeName, out var type) && !type.ContainsGenericParameters)
				return type;

			return TypeAliasParser.TryParse(typeName, out var token) ? token.ToType() : Type.GetType(typeName, false, true);
		}

		[Flags]
		private enum TypeAliasFlags
		{
			None,
			Nullable,
			Arrayable,
		}

		private readonly struct TypeAliasToken
		{
			#region 成员字段
			private readonly TypeAliasToken[] _genericArguments;
			#endregion

			#region 构造函数
			public TypeAliasToken(string name, TypeAliasFlags flags, params TypeAliasToken[] genericArguments) : this(name, null, flags, genericArguments) { }
			public TypeAliasToken(string name, string assembly, TypeAliasFlags flags, params TypeAliasToken[] genericArguments)
			{
				this.Name = name;
				this.Assembly = assembly;
				this.Flags = flags;
				_genericArguments = genericArguments;
			}
			#endregion

			#region 公共字段
			public readonly string Name;
			public readonly string Assembly;
			public readonly TypeAliasFlags Flags;
			#endregion

			#region 公共属性
			public readonly bool HasGenericArguments => _genericArguments != null && _genericArguments.Length > 0;
			public readonly TypeAliasToken[] GenericArguments => _genericArguments;
			public readonly bool Nullable => (this.Flags & TypeAliasFlags.Nullable) == TypeAliasFlags.Nullable;
			public readonly bool Arrayable => (this.Flags & TypeAliasFlags.Arrayable) == TypeAliasFlags.Arrayable;
			#endregion

			#region 公共方法
			public Type ToType() => GetType(this);
			#endregion

			#region 重写方法
			public override string ToString()
			{
				string result;

				if(this.HasGenericArguments)
				{
					if(this.Nullable)
						result = this.Arrayable ? $"{this.Name}<{GetGenericArguments(this.GenericArguments)}>?[]" : $"{this.Name}<{GetGenericArguments(this.GenericArguments)}>?";
					else
						result = this.Arrayable ? $"{this.Name}<{GetGenericArguments(this.GenericArguments)}>[]" : $"{this.Name}<{GetGenericArguments(this.GenericArguments)}>";
				}
				else
				{
					if(this.Nullable)
						result = this.Arrayable ? $"{this.Name}?[]" : $"{this.Name}?";
					else
						result = this.Arrayable ? $"{this.Name}[]" : this.Name;
				}

				return string.IsNullOrEmpty(this.Assembly) ? result : $"{result}@{this.Assembly}";
			}
			#endregion

			#region 私有方法
			private static string GetGenericArguments(TypeAliasToken[] aliases)
			{
				if(aliases == null || aliases.Length == 0)
					return null;

				if(aliases.Length == 1)
					return aliases[0].ToString();

				var text = new StringBuilder();

				for(int i = 0; i < aliases.Length; i++)
				{
					if(i > 0)
						text.Append(", ");

					text.Append(aliases[i].ToString());
				}

				return text.ToString();
			}

			private static Type GetType(TypeAliasToken alias)
			{
				if(string.IsNullOrEmpty(alias.Name))
					return null;

				if(!Types.TryGetValue(alias.Name, out var type))
				{
					var typeName = alias.Name.Contains('.') ? alias.Name : $"{nameof(System)}.{alias.Name}";
					if(alias.HasGenericArguments)
						typeName += $"`{alias.GenericArguments.Length}";
					if(!string.IsNullOrEmpty(alias.Assembly))
						typeName = $"{typeName}, {alias.Assembly}";

					type = Type.GetType(typeName, false, true);
				}

				if(type.ContainsGenericParameters && alias.HasGenericArguments)
				{
					var types = alias.GenericArguments.Select(GetType).ToArray();
					if(type.GenericTypeArguments.Length == types.Length)
						type = type.MakeGenericType(types);
				}

				if(alias.Nullable)
					type = typeof(Nullable<>).MakeGenericType(type);
				if(alias.Arrayable)
					type = type.MakeArrayType();

				return type;
			}
			#endregion
		}

		private static class TypeAliasParser
		{
			#region 解析方法
			public static bool TryParse(ReadOnlySpan<char> text, out TypeAliasToken result)
			{
				result = Parse(text);
				return !string.IsNullOrEmpty(result.Name);
			}

			public static TypeAliasToken Parse(ReadOnlySpan<char> text, Action<string> onError = null)
			{
				if(text.IsEmpty)
					return default;

				var context = new AliasContext(text);
				ReadOnlySpan<char> type, assembly;
				TypeAliasFlags flags = TypeAliasFlags.None;

				while(context.Move())
				{
					switch(context.State)
					{
						case AliasState.None:
							DoNone(ref context);
							break;
						case AliasState.Type:
							if(DoType(ref context))
								type = context.Reset();
							break;
						case AliasState.Assembly:
							if(DoAssembly(ref context))
								assembly = context.Reset();
							break;
						case AliasState.Nullable:
							if(DoNullable(ref context))
								flags |= TypeAliasFlags.Nullable;
							break;
						case AliasState.Arrayable:
							if(DoArrayable(ref context))
								flags |= TypeAliasFlags.Arrayable;
							break;
						case AliasState.Generic:
							DoGeneric(ref context);
							break;
						case AliasState.GenericFinal:
							DoGenericFinal(ref context);
							break;
						case AliasState.Ending:
							DoEnding(ref context);
							break;
					}

					if(context.HasError(out var message))
					{
						onError?.Invoke(message);
						return default;
					}
				}

				return default;
			}

			private static void DoNone(ref AliasContext context)
			{
				if(context.IsLetter || context.Character == '_')
					context.Accept(AliasState.Type);
				else if(context.IsWhitespace)
					context.Accept();
				else
					context.Error($"The type name or namespace must begin with a letter or an underscore.");
			}

			private static bool DoType(ref AliasContext context)
			{
				switch(context.Character)
				{
					case '.':
						if(context.Move())
						{
							if(context.IsLetter || context.Character == '_')
								context.Accept();
							else
								context.Error($"The type name or namespace must begin with a letter or an underscore.");
						}
						break;
					case '@':
						context.Accept(AliasState.Assembly);
						break;
					case ',':
						if(context.Depth > 0)
							context.Accept(AliasState.Generic);
						else
							context.Accept(AliasState.Assembly);
						break;
					case '?':
						context.Accept(AliasState.Nullable);
						break;
					case '<':
						context.Indent();
						context.Accept(AliasState.Generic);
						break;
					case '>':
						if(context.Depth > 0)
						{
							context.Dedent();
							context.Accept(AliasState.GenericFinal);
						}
						else
							context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
						break;
					case '[':
						context.Accept(AliasState.Arrayable);
						break;
					default:
						if(context.IsLetterOrDigit || context.Character == '_')
							context.Accept();
						else if(context.IsWhitespace)
							context.Accept(AliasState.Ending);
						else
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
						break;
				}

				return context.State != AliasState.Type;
			}

			private static bool DoAssembly(ref AliasContext context)
			{
				switch(context.Character)
				{
					case '.':
						if(context.Move())
						{
							if(context.IsLetterOrDigit || context.Character == '_' || context.Character == '-')
								context.Accept();
							else
								context.Error($"The assembly name contains the illegal character '{context.Character}', located at the {context.Position} character.");
						}
						break;
					case '>':
						if(context.Depth > 0)
						{
							context.Dedent();
							context.Accept(AliasState.GenericFinal);
						}
						else
							context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
						break;
					default:
						if(context.IsLetterOrDigit || context.Character == '_' || context.Character == '-')
							context.Accept();
						else if(context.IsWhitespace)
							context.Accept(AliasState.Ending);
						else
							context.Error($"The assembly name contains the illegal character '{context.Character}', located at the {context.Position} character.");
						break;
				}

				return context.State != AliasState.Assembly;
			}

			private static bool DoNullable(ref AliasContext context)
			{
				switch(context.Character)
				{
					case '@':
						context.Accept(AliasState.Assembly);
						break;
					case ',':
						if(context.Depth > 0)
							context.Accept(AliasState.Generic);
						else
							context.Accept(AliasState.Assembly);
						break;
					case '>':
						if(context.Depth > 0)
						{
							context.Dedent();
							context.Accept(AliasState.GenericFinal);
						}
						else
							context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
						break;
					case '[':
						context.Accept(AliasState.Arrayable);
						break;
					default:
						if(context.IsWhitespace)
							context.Accept();
						else
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
						break;
				}

				return context.State != AliasState.Nullable;
			}

			private static bool DoArrayable(ref AliasContext context)
			{
				while(context.Move())
				{
					if(context.IsWhitespace)
						context.Accept();
					else if(context.Character == ']')
						break;
					else
						context.Error($"Illegal character at the {context.Position}th character position.");
				}

				switch(context.Character)
				{
					case '@':
						context.Accept(AliasState.Assembly);
						break;
					case ',':
						if(context.Depth > 0)
							context.Accept(AliasState.Generic);
						else
							context.Accept(AliasState.Assembly);
						break;
					case '>':
						if(context.Depth > 0)
						{
							context.Dedent();
							context.Accept(AliasState.GenericFinal);
						}
						else
							context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
						break;
					default:
						if(context.IsWhitespace)
							context.Accept(AliasState.Ending);
						else
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
						break;
				}

				return context.State != AliasState.Arrayable;
			}

			private static void DoGeneric(ref AliasContext context)
			{
				if(context.IsWhitespace)
					context.Accept();

				switch(context.Character)
				{
					case '>':
						context.Dedent();
						context.Accept(AliasState.GenericFinal);
						break;
					default:
						if(context.IsLetter || context.Character == '_')
							context.Accept(AliasState.Type);
						else
							context.Error($"The type name or namespace must begin with a letter or an underscore.");
						break;
				}
			}

			private static void DoGenericFinal(ref AliasContext context)
			{
				switch(context.Character)
				{
					case '@':
						context.Accept(AliasState.Assembly);
						break;
					case ',':
						if(context.Depth > 0)
							context.Accept(AliasState.Generic);
						else
							context.Accept(AliasState.Assembly);
						break;
					case '>':
						if(context.Depth > 0)
						{
							context.Dedent();
							context.Accept(AliasState.GenericFinal);
						}
						else
							context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
						break;
					default:
						if(context.IsWhitespace)
							context.Accept();
						else
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
						break;
				}
			}

			private static void DoEnding(ref AliasContext context)
			{
				//循环跳过空白字符
				while(context.IsWhitespace && context.Move()) { }

				switch(context.Previous)
				{
					case AliasState.Type:
						DoTypeEnding(ref context);
						break;
					case AliasState.Nullable:
						DoNullableEnding(ref context);
						break;
					case AliasState.Arrayable:
						DoArrayableEnding(ref context);
						break;
					case AliasState.Assembly:
						DoAssembly(ref context);
						break;
				}

				static void DoTypeEnding(ref AliasContext context)
				{
					switch(context.Character)
					{
						case '@':
							context.Accept(AliasState.Assembly);
							break;
						case ',':
							if(context.Depth > 0)
								context.Accept(AliasState.Generic);
							else
								context.Accept(AliasState.Assembly);
							break;
						case '<':
							context.Indent();
							context.Accept(AliasState.Generic);
							break;
						case '>':
							if(context.Depth > 0)
							{
								context.Dedent();
								context.Accept(AliasState.GenericFinal);
							}
							else
								context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
							break;
						case '[':
							context.Accept(AliasState.Arrayable);
							break;
						default:
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
							break;
					}
				}

				static void DoNullableEnding(ref AliasContext context)
				{
					switch(context.Character)
					{
						case '@':
							context.Accept(AliasState.Assembly);
							break;
						case ',':
							if(context.Depth > 0)
								context.Accept(AliasState.Generic);
							else
								context.Accept(AliasState.Assembly);
							break;
						case '>':
							if(context.Depth > 0)
							{
								context.Dedent();
								context.Accept(AliasState.GenericFinal);
							}
							else
								context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
							break;
						case '[':
							context.Accept(AliasState.Arrayable);
							break;
						default:
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
							break;
					}
				}

				static void DoArrayableEnding(ref AliasContext context)
				{
					switch(context.Character)
					{
						case '@':
							context.Accept(AliasState.Assembly);
							break;
						case ',':
							if(context.Depth > 0)
								context.Accept(AliasState.Generic);
							else
								context.Accept(AliasState.Assembly);
							break;
						case '>':
							if(context.Depth > 0)
							{
								context.Dedent();
								context.Accept(AliasState.GenericFinal);
							}
							else
								context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
							break;
						default:
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
							break;
					}
				}

				static void DoAssembly(ref  AliasContext context)
				{
					switch(context.Character)
					{
						case ',':
							if(context.Depth > 0)
								context.Accept(AliasState.Generic);
							else
								context.Error($"The assembly name is followed by invalid comma-separated content.");
							break;
						case '>':
							if(context.Depth > 0)
							{
								context.Dedent();
								context.Accept(AliasState.GenericFinal);
							}
							else
								context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
							break;
						default:
							context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
							break;
					}
				}
			}
			#endregion

		}

		private enum AliasState
		{
			None,
			Type,
			Generic,
			GenericFinal,
			Nullable,
			Arrayable,
			Assembly,
			Ending,
		}

		private ref struct AliasContext
		{
			#region 私有字段
			private readonly ReadOnlySpan<char> _text;
			private AliasState _state;
			private AliasState _previous;
			private char _character;
			private int _position;
			private int _depth;
			private int _count;
			private int _whitespaces;
			private string _errorMessage;
			#endregion

			#region 构造函数
			public AliasContext(ReadOnlySpan<char> text)
			{
				_text = text;
				_state = AliasState.None;
				_previous = AliasState.None;
				_character = '\0';
				_position = 0;
				_depth = 0;
				_count = 0;
				_whitespaces = 0;
				_errorMessage = null;
			}
			#endregion

			#region 公共属性
			public AliasState State => _state;
			public AliasState Previous => _previous;
			public int Depth => _depth;
			public int Position => _position;
			public char Character => _character;
			public bool IsLetter => char.IsLetter(_character);
			public bool IsDigit => char.IsDigit(_character);
			public bool IsLetterOrDigit => char.IsLetterOrDigit(_character);
			public bool IsWhitespace => char.IsWhiteSpace(_character);
			#endregion

			#region 公共方法
			public bool Move()
			{
				if(_position < _text.Length)
				{
					_character = _text[_position++];
					return true;
				}

				_character = '\0';
				return false;
			}

			public void Error(string message) => _errorMessage = message;
			public bool HasError(out string message)
			{
				message = _errorMessage;
				return message != null;
			}

			public bool TryGet(out ReadOnlySpan<char> value)
			{
				if(_count > 0)
				{
					value = _text.Slice(_position - _count - _whitespaces - 1, _count);
					return true;
				}

				value = ReadOnlySpan<char>.Empty;
				return false;
			}

			public int Indent() => ++_depth;
			public int Dedent() => --_depth;

			public void Reset(AliasState state)
			{
				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public void Reset(AliasState state, out ReadOnlySpan<char> value)
			{
				value = _count > 0 ? _text.Slice(_position - _count - _whitespaces - 1, _count) : ReadOnlySpan<char>.Empty;

				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public ReadOnlySpan<char> Reset()
			{
				var result = _count > 0 ? _text.Slice(_position - _count - _whitespaces - 1, _count) : ReadOnlySpan<char>.Empty;
				_count = 0;
				_whitespaces = 0;
				return result;
			}

			public void Accept(AliasState? state = null) => this.Accept(state, out _);
			public void Accept(AliasState? state, out int count)
			{
				if(char.IsWhiteSpace(_character))
				{
					if(_count > 0)
						_whitespaces++;
				}
				else
				{
					_count += _whitespaces + 1;
					_whitespaces = 0;
				}

				if(state.HasValue)
				{
					_previous = _state;
					_state = state.Value;
				}

				count = _count;
			}
			#endregion
		}
	}
}
