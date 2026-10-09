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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Collections.Generic;

using Zongsoft.Expressions;
using Zongsoft.Expressions.Tokenization;

namespace Zongsoft.Text.Templating;

public partial class TemplateEvaluator
{
	#region 语法节点
	private sealed class Reference(string text, string @namespace, string name, int position, int length, List<Accessor> accessors)
	{
		public readonly string Text = text;
		public readonly string Name = name;
		public readonly string Namespace = @namespace;
		public readonly int Position = position;
		public readonly int Length = length;
		public readonly List<Accessor> Accessors = accessors;
	}

	private readonly struct Accessor(string name, List<Argument> arguments = null)
	{
		public readonly string Name = name;
		public readonly List<Argument> Arguments = arguments;
	}

	private readonly struct Argument(object value, Reference reference = null)
	{
		public readonly object Value = value;
		public readonly Reference Reference = reference;
	}

	private readonly struct Part(string text, Reference reference = null, string format = null)
	{
		public readonly Reference Reference = reference;
		public readonly string Text = text;
		public readonly string Format = format;
	}

	private readonly struct Lexeme(Token token, int position, int length)
	{
		public readonly Token Token = token;
		public readonly int Position = position;
		public readonly int Length = length;
	}
	#endregion

	#region 嵌套子类
	private ref struct Parser(ReadOnlySpan<char> template, int depth)
	{
		private static readonly Lexer _lexer = CreateLexer();
		private static Lexer CreateLexer()
		{
			var lexer = new Lexer();
			lexer.Tokenizers[1] = new NumberTokenizer(true);
			return lexer;
		}

		private readonly ReadOnlySpan<char> _template = template;
		private readonly int _depth = depth;
		private List<Lexeme> _tokens;
		private int _index;

		public List<Part> Parse()
		{
			var parts = new List<Part>();
			var literal = new StringBuilder();
			var position = 0;

			while(position < _template.Length)
			{
				var character = _template[position];

				if(character == '\\')
				{
					if(position + 1 >= _template.Length || !StringTokenizer.TryEscape(_template[position + 1], out var escaped))
						throw this.Failure("InvalidEscape", position, Math.Min(2, _template.Length - position));

					literal.Append(escaped);
					position += 2;
				}
				else if(character == '$' && position + 1 < _template.Length && _template[position + 1] == '{')
				{
					if(literal.Length > 0)
					{
						parts.Add(new Part(literal.ToString()));
						literal.Clear();
					}

					var part = this.ParsePlaceholder(ref position);

					//只有一个片段时精确分配，避免结构节点占用多余的数组槽位。
					if(parts.Count == 0 && position == _template.Length)
						parts.Capacity = 1;

					parts.Add(part);
				}
				else
				{
					literal.Append(character);
					position++;
				}
			}

			if(literal.Length > 0)
			{
				if(parts.Count == 0)
					parts.Capacity = 1;

				parts.Add(new Part(literal.ToString()));
			}

			return parts;
		}

		private Part ParsePlaceholder(ref int position)
		{
			var opening = position;
			var start = position + 2;
			var cursor = start;
			var brackets = 0;
			var quote = '\0';

			for(; cursor < _template.Length; cursor++)
			{
				var character = _template[cursor];

				if(quote != '\0')
				{
					if(character == '\\')
						cursor++;
					else if(character == quote)
						quote = '\0';

					continue;
				}

				if(character is '\'' or '"')
					quote = character;
				else if(character == '[')
					brackets++;
				else if(character == ']')
					brackets--;
				else if(character == '}' || character == '#' && brackets == 0)
					break;
			}

			if(cursor >= _template.Length)
				throw this.Failure("UnclosedPlaceholder", opening, _template.Length - opening);

			var end = cursor;
			string format = null;

			if(_template[cursor] == '#')
				format = this.ParseFormat(opening, ref cursor);

			position = cursor + 1;

			if(start < end && _template[start] == '=')
				throw this.Failure("UnsupportedExpression", opening, position - opening);

			this.Tokenize(start, end);

			_index = 0;
			var reference = this.ParseReference(0);

			if(_index != _tokens.Count)
				throw this.Failure("InvalidSyntax", _tokens[_index].Position, _tokens[_index].Length);

			return new Part(null, reference, format);
		}

		private readonly string ParseFormat(int opening, ref int cursor)
		{
			var start = -1;
			var end = -1;
			var quote = '\0';

			for(cursor++; cursor < _template.Length; cursor++)
			{
				var character = _template[cursor];

				if(quote == '\0' && character == '}')
				{
					if(start < 0)
						throw this.Failure("EmptyFormat", cursor, 1);

					return _template[start..end].ToString();
				}

				var significant = quote != '\0' || !char.IsWhiteSpace(character);

				if(character == '\\')
				{
					if(start < 0)
						start = cursor;

					cursor++;
					end = cursor + 1;
					continue;
				}

				if(character == quote)
					quote = '\0';
				else if(quote == '\0' && character is '\'' or '"')
					quote = character;

				if(significant)
				{
					if(start < 0)
						start = cursor;

					end = cursor + 1;
				}
			}

			throw this.Failure("UnclosedPlaceholder", opening, _template.Length - opening);
		}

		#region 引用解析
		private void Tokenize(int start, int end)
		{
			_tokens = new List<Lexeme>();
			var text = _template[start..end];

			if(text.Length == 0)
				throw this.Failure("InvalidSyntax", start, 0);

			using var scanner = _lexer.GetScanner(text);
			var previous = 0;

			while(true)
			{
				Token token;
				var offset = previous;
				var length = 0;

				try
				{
					token = scanner.Scan(out offset, out length);
				}
				catch(Exception exception)
				{
					throw this.Failure("InvalidSyntax", start + offset, Math.Max(1, length), exception);
				}

				if(offset != previous)
					throw this.Failure("InvalidWhitespace", start + previous, offset - previous);

				if(token == null)
					break;

				_tokens.Add(new Lexeme(token, start + offset, length));
				previous = offset + length;
			}
		}

		private Reference ParseReference(int nesting)
		{
			//索引语法深度与可配置的字符串模板递归深度分别保护。
			if(nesting >= 256)
				throw this.Failure("SyntaxDepthExceeded", this.CurrentPosition, 1);

			var start = this.CurrentPosition;
			var names = new List<string> { this.ReadName() };

			while(this.Match("."))
				names.Add(this.ReadName());

			string name;
			string @namespace = null;
			List<Accessor> accessors = null;

			if(this.Match(":"))
			{
				@namespace = string.Join('.', names);
				name = this.ReadName();
			}
			else
			{
				name = names[0];

				for(int i = 1; i < names.Count; i++)
					(accessors ??= new()).Add(new Accessor(names[i]));
			}

			while(_index < _tokens.Count)
			{
				if(this.Match("."))
					(accessors ??= new()).Add(new Accessor(this.ReadName()));
				else if(this.Match("["))
				{
					var arguments = new List<Argument> { this.ParseArgument(nesting + 1) };

					while(this.Match(","))
						arguments.Add(this.ParseArgument(nesting + 1));

					if(!this.Match("]"))
						throw this.Failure("InvalidSyntax", this.CurrentPosition, 1);

					(accessors ??= new()).Add(new Accessor(null, arguments));
				}
				else
					break;
			}

			var last = _tokens[_index - 1];
			var length = last.Position + last.Length - start;

			return new Reference(_template.Slice(start, length).ToString(), @namespace, name, start, length, accessors);
		}

		private Argument ParseArgument(int nesting)
		{
			if(_index >= _tokens.Count)
				throw this.Failure("InvalidSyntax", this.CurrentPosition, 0);

			var token = _tokens[_index].Token;

			if(token.Type == TokenType.Constant)
			{
				//布尔/空 只有在整个裸参数中才是常量，其它名称位置仍是标识符
				if(token.Value is not bool && token.Value != null || this.IsSymbol(_index + 1, ",") || this.IsSymbol(_index + 1, "]"))
				{
					_index++;
					return new Argument(token.Value);
				}
			}

			return new Argument(null, this.ParseReference(nesting));
		}

		private string ReadName()
		{
			if(_index >= _tokens.Count)
				throw this.Failure("InvalidSyntax", this.CurrentPosition, 0);

			var lexeme = _tokens[_index];
			var token = lexeme.Token;

			if(token.Type != TokenType.Identifier && !(token.Type == TokenType.Constant && (token.Value is bool || token.Value == null)))
				throw this.Failure("InvalidSyntax", lexeme.Position, lexeme.Length);

			_index++;
			return token.Type == TokenType.Identifier ? (string)token.Value : _template.Slice(lexeme.Position, lexeme.Length).ToString();
		}

		private bool Match(string symbol)
		{
			if(!this.IsSymbol(_index, symbol))
				return false;

			_index++;
			return true;
		}

		private readonly bool IsSymbol(int index, string symbol) =>
			index < _tokens.Count && _tokens[index].Token.Type == TokenType.Symbol && (string)_tokens[index].Token.Value == symbol;

		private readonly int CurrentPosition => _index < _tokens.Count ? _tokens[_index].Position :
			_tokens.Count == 0 ? 0 : _tokens[^1].Position + _tokens[^1].Length;

		private readonly TemplateEvaluationException Failure(string code, int position, int length, Exception innerException = null) =>
			new(code, TemplateEvaluationStage.Parsing, _template.ToString(), null, position, length, _depth, innerException);
		#endregion
	}
	#endregion
}
