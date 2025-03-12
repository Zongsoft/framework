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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Common;

partial class TypeAlias
{
	#region 解析方法
	private static TypeAliasToken ParseCore(ReadOnlySpan<char> text, Action<string> onError = null)
	{
		if(text.IsEmpty)
			return default;

		var context = new AliasContext(text);

		while(context.Move())
		{
			switch(context.State)
			{
				case AliasState.None:
					DoNone(ref context);
					break;
				case AliasState.Type:
					DoType(ref context);
					break;
				case AliasState.Assembly:
					DoAssembly(ref context);
					break;
				case AliasState.Nullable:
					DoNullable(ref context);
					break;
				case AliasState.Arrayable:
					DoArrayable(ref context);
					break;
				case AliasState.Generic:
					DoGeneric(ref context);
					break;
				case AliasState.GenericStart:
					DoGenericStart(ref context);
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

		return context.GetResult();
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
				context.Accept();

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
			case '<':
				context.Accept(AliasState.GenericStart);
				break;
			case '>':
				if(context.Depth > 0)
					context.Accept(AliasState.GenericFinal);
				else
					context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
				break;
			case '?':
				context.Accept(AliasState.Nullable);
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
				context.Accept();

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
					context.Accept(AliasState.GenericFinal);
				else
					context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
				break;
			default:
				if(context.IsLetterOrDigit || context.Character == '_' || context.Character == '-')
					context.Accept();
				else if(context.IsWhitespace)
				{
					if(context.Count > 0)
						context.Accept(AliasState.Ending);
					else
						context.Skip();
				}
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
					context.Accept(AliasState.GenericFinal);
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
					context.Accept(AliasState.GenericFinal);
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
		{
			context.Skip();
			return;
		}

		if(context.IsLetter || context.Character == '_')
			context.Accept(AliasState.Type);
		else
			context.Error($"The type name or namespace must begin with a letter or an underscore.");
	}

	private static void DoGenericStart(ref AliasContext context)
	{
		if(context.IsWhitespace)
		{
			context.Skip();
			return;
		}

		switch(context.Character)
		{
			case '>':
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
					context.Accept(AliasState.GenericFinal);
				else
					context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
				break;
			case '?':
				context.Accept(AliasState.Nullable);
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
				DoAssemblyEnding(ref context);
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
					context.Accept(AliasState.GenericStart);
					break;
				case '>':
					if(context.Depth > 0)
						context.Accept(AliasState.GenericFinal);
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
						context.Accept(AliasState.GenericFinal);
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
						context.Accept(AliasState.GenericFinal);
					else
						context.Error($"Syntax error: The '>' symbol at {context.Position} character missing matching generic argument starter.");
					break;
				default:
					context.Error($"The type name contains the illegal character '{context.Character}', located at the {context.Position} character.");
					break;
			}
		}

		static void DoAssemblyEnding(ref AliasContext context)
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
						context.Accept(AliasState.GenericFinal);
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

	#region 嵌套结构
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
		private readonly IReadOnlyList<TypeAliasToken> _genericArguments;
		#endregion

		#region 构造函数
		public TypeAliasToken(string type, TypeAliasFlags flags, IReadOnlyList<TypeAliasToken> genericArguments = null) : this(type, null, flags, genericArguments) { }
		public TypeAliasToken(string type, string assembly, TypeAliasFlags flags, IReadOnlyList<TypeAliasToken> genericArguments = null)
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
		public readonly bool HasGenericArguments => _genericArguments != null && _genericArguments.Count > 0;
		public readonly IReadOnlyList<TypeAliasToken> GenericArguments => _genericArguments;
		public readonly bool Nullable => (this.Flags & TypeAliasFlags.Nullable) == TypeAliasFlags.Nullable;
		public readonly bool Arrayable => (this.Flags & TypeAliasFlags.Arrayable) == TypeAliasFlags.Arrayable;
		#endregion

		#region 公共方法
		public Type ToType(bool throwException) => GetType(this, null, null, throwException);
		public Type ToType(Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwException) => GetType(this, assemblyResolver, typeResolver, throwException);
		public TypeAliasToken Clone(string assembly) => new(this.Type, assembly, this.Flags, _genericArguments);
		public TypeAliasToken Clone(TypeAliasFlags flags) => new(this.Type, this.Assembly, flags, _genericArguments);
		public TypeAliasToken Clone(IReadOnlyList<TypeAliasToken> genericArguments) => new(this.Type, this.Assembly, this.Flags, genericArguments);
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
		private static string GetGenericArguments(IReadOnlyList<TypeAliasToken> aliases)
		{
			if(aliases == null || aliases.Count == 0)
				return null;

			if(aliases.Count == 1)
				return aliases[0].ToString();

			var text = new StringBuilder();

			for(int i = 0; i < aliases.Count; i++)
			{
				if(i > 0)
					text.Append(", ");

				text.Append(aliases[i].ToString());
			}

			return text.ToString();
		}

		private static TypeInfo GetType(TypeAliasToken alias, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwException)
		{
			if(string.IsNullOrEmpty(alias.Type))
				return null;

			if(!_types.TryGetValue(alias.Type, out var type))
			{
				var typeName = alias.Type.Contains('.') ? alias.Type : $"{nameof(System)}.{alias.Type}";
				if(alias.HasGenericArguments)
					typeName += $"`{alias.GenericArguments.Count}";
				if(!string.IsNullOrEmpty(alias.Assembly))
					typeName = $"{typeName}, {alias.Assembly}";

				if(assemblyResolver == null && typeResolver == null)
					type = System.Type.GetType(typeName, throwException, true)?.GetTypeInfo();
				else
					type = System.Type.GetType(typeName, assemblyResolver, typeResolver, throwException, true)?.GetTypeInfo();

				if(type == null)
					return null;
			}

			if(type.ContainsGenericParameters && alias.HasGenericArguments)
			{
				var types = alias.GenericArguments.Select(token => GetType(token, assemblyResolver, typeResolver, throwException)).ToArray();
				if(type.GenericTypeParameters.Length == types.Length)
					type = type.MakeGenericType(types).GetTypeInfo();
			}

			if(alias.Nullable)
				type = typeof(Nullable<>).MakeGenericType(type).GetTypeInfo();
			if(alias.Arrayable)
				type = type.MakeArrayType().GetTypeInfo();

			return type;
		}
		#endregion
	}

	private enum AliasState
	{
		None,
		Type,
		Generic,
		GenericStart,
		GenericFinal,
		Nullable,
		Arrayable,
		Assembly,
		Ending,
	}

	private readonly struct StackToken
	{
		public StackToken(int depth, ReadOnlySpan<char> type, TypeAliasFlags flags)
		{
			this.Depth = depth;
			this.Token = new TypeAliasToken(type.ToString(), flags);
		}
		public StackToken(int depth, TypeAliasToken token)
		{
			this.Depth = depth;
			this.Token = token;
		}

		public readonly TypeAliasToken Token;
		public readonly int Depth;

		public override string ToString() => $"{this.Depth}#{this.Token}";
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

		private TypeAliasFlags _flags;
		private ReadOnlySpan<char> _type;
		private ReadOnlySpan<char> _assembly;
		private readonly Stack<StackToken> _stack;
		#endregion

		#region 构造函数
		public AliasContext(ReadOnlySpan<char> text)
		{
			_text = text;
			_state = AliasState.None;
			_previous = AliasState.None;
			_character = '\0';
			_position = 0;
			_count = 0;
			_depth = 0;
			_errorMessage = null;
			_flags = TypeAliasFlags.None;
			_stack = new();
		}
		#endregion

		#region 公共属性
		public TypeAliasFlags Flags => _flags;
		public AliasState State => _state;
		public AliasState Previous => _previous;
		public int Depth => _depth;
		public int Count => _count;
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

			_position = _text.Length + 1;
			_character = '\0';
			return false;
		}

		public void Error(string message) => _errorMessage = message;
		public readonly bool HasError(out string message)
		{
			message = _errorMessage;
			return message != null;
		}

		public void Skip()
		{
			if(_count > 0)
				_whitespaces++;
		}

		public void Accept() => _count++;
		public void Accept(AliasState state)
		{
			switch(_state)
			{
				case AliasState.None:
					_count++;
					break;
				case AliasState.Generic:
					if(!_type.IsEmpty)
						_stack.Push(new StackToken(_depth, _type, _flags));
					_flags = TypeAliasFlags.None;
					_count++;
					break;
				case AliasState.GenericStart:
					_stack.Push(new StackToken(_depth++, _type, _flags));
					_flags = TypeAliasFlags.None;
					_count++;
					break;
				case AliasState.Type:
					_type = this.Reset();
					break;
				case AliasState.Assembly:
					_assembly = this.Reset();
					break;
				case AliasState.Arrayable:
					if(_type.IsEmpty && _stack.TryPop(out var token))
						_stack.Push(new StackToken(token.Depth, token.Token.Clone(token.Token.Flags | TypeAliasFlags.Arrayable)));
					else
						this.Mark(TypeAliasFlags.Arrayable);
					break;
			}

			switch(state)
			{
				case AliasState.Nullable:
					if(_state == AliasState.GenericFinal && _stack.TryPop(out var token))
						_stack.Push(new StackToken(token.Depth, token.Token.Clone(token.Token.Flags | TypeAliasFlags.Nullable)));
					else
						this.Mark(TypeAliasFlags.Nullable);
					break;
				case AliasState.GenericFinal:
					var arguments = this.Dedent();

					if(_stack.TryPop(out token))
					{
						token = new StackToken(token.Depth, token.Token.Clone(arguments));
						_stack.Push(token);

						_type = default;
						_assembly = default;
						_flags = TypeAliasFlags.None;
					}

					break;
			}

			_previous = _state;
			_state = state;
		}

		public TypeAliasToken GetResult()
		{
			if(_stack.TryPop(out var token))
				return token.Token;

			switch(_state)
			{
				case AliasState.Type:
					_type = this.Reset();
					break;
				case AliasState.Assembly:
					_assembly = this.Reset();
					break;
			}

			return new TypeAliasToken(_type.ToString(), _assembly.ToString(), _flags);
		}
		#endregion

		#region 私有方法
		private IReadOnlyList<TypeAliasToken> Dedent()
		{
			var tokens = GetStackTokens(_stack, _depth--);

			if(!_type.IsEmpty)
				tokens.Add(new TypeAliasToken(_type.ToString(), _assembly.ToString(), _flags));

			return tokens;

			static List<TypeAliasToken> GetStackTokens(Stack<StackToken> stack, int depth)
			{
				if(stack.Count == 0 || depth < 0)
					return [];

				var result = new List<TypeAliasToken>();
				while(stack.TryPeek(out var token) && token.Depth == depth)
				{
					result.Insert(0, stack.Pop().Token);
				}
				return result;
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private TypeAliasFlags Mark(TypeAliasFlags flags) => _flags |= flags;
		private ReadOnlySpan<char> Reset()
		{
			var result = _text.Slice(_position - _count - _whitespaces - 1, _count);
			_count = 0;
			return result;
		}
		#endregion
	}
	#endregion
}
