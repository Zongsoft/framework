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
	partial class TypeAlias
	{
		#region 解析方法
		private static TypeAliasToken ParseCore(ReadOnlySpan<char> text, Action<string> onError = null)
		{
			if(text.IsEmpty)
				return default;

			var context = new AliasContext(text);
			var type = ReadOnlySpan<char>.Empty;
			var assembly = ReadOnlySpan<char>.Empty;
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

			if(context.State == AliasState.Nullable)
				flags |= TypeAliasFlags.Nullable;

			return new TypeAliasToken(type.ToString(), assembly.ToString(), flags);
		}

		private static void DoNone(ref AliasContext context)
		{
			if(context.IsLetter || context.Character == '_')
				context.Accept(AliasState.Type);
			else if(context.IsWhitespace)
				context.Skip();
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
						if(!context.IsLetter && context.Character != '_')
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
						;
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
							;
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
						;
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
						context.Skip();
					else
						context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
					break;
			}

			return context.State != AliasState.Nullable;
		}

		private static bool DoArrayable(ref AliasContext context)
		{
			switch(context.Character)
			{
				case ']':
					context.Accept(AliasState.Ending);
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
						context.Skip();
					else
						context.Error($"Illegal character at the {context.Position}th character position.");
					break;
			}

			return context.State != AliasState.Arrayable;
		}

		private static void DoGeneric(ref AliasContext context)
		{
			if(context.IsWhitespace)
				context.Skip();

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
						context.Skip();
					else
						context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
					break;
			}
		}

		private static void DoEnding(ref AliasContext context)
		{
			//循环跳过空白字符
			if(context.IsWhitespace)
			{
				context.Skip();
				return;
			}

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

			static void DoAssembly(ref AliasContext context)
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
			public TypeAliasToken(string type, TypeAliasFlags flags, params TypeAliasToken[] genericArguments) : this(type, null, flags, genericArguments) { }
			public TypeAliasToken(string type, string assembly, TypeAliasFlags flags, params TypeAliasToken[] genericArguments)
			{
				this.Type = type;
				this.Assembly = assembly;
				this.Flags = flags;
				_genericArguments = genericArguments;
			}
			#endregion

			#region 公共字段
			public readonly string Type;
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
						result = this.Arrayable ? $"{this.Type}<{GetGenericArguments(this.GenericArguments)}>?[]" : $"{this.Type}<{GetGenericArguments(this.GenericArguments)}>?";
					else
						result = this.Arrayable ? $"{this.Type}<{GetGenericArguments(this.GenericArguments)}>[]" : $"{this.Type}<{GetGenericArguments(this.GenericArguments)}>";
				}
				else
				{
					if(this.Nullable)
						result = this.Arrayable ? $"{this.Type}?[]" : $"{this.Type}?";
					else
						result = this.Arrayable ? $"{this.Type}[]" : this.Type;
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
				if(string.IsNullOrEmpty(alias.Type))
					return null;

				if(!Types.TryGetValue(alias.Type, out var type))
				{
					var typeName = alias.Type.Contains('.') ? alias.Type : $"{nameof(System)}.{alias.Type}";
					if(alias.HasGenericArguments)
						typeName += $"`{alias.GenericArguments.Length}";
					if(!string.IsNullOrEmpty(alias.Assembly))
						typeName = $"{typeName}, {alias.Assembly}";

					type = System.Type.GetType(typeName, false, true);
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
			private int _offset;
			private int _depth;
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
				_offset = 0;
				_depth = 0;
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
			public readonly bool HasError(out string message)
			{
				message = _errorMessage;
				return message != null;
			}

			public int Indent() => ++_depth;
			public int Dedent() => --_depth;

			public ReadOnlySpan<char> Reset()
			{
				var result = _text.Slice(_offset, _position - _offset - 1);
				_offset = _position;
				return result;
			}

			public void Skip() => _offset++;
			public void Accept(AliasState state)
			{
				if(_state == state)
					return;

				_previous = _state;
				_state = state;
			}
			#endregion
		}
	}
}
