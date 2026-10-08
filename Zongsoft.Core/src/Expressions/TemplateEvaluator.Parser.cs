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
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Text;
using System.Collections.Generic;

using Zongsoft.Expressions.Tokenization;

namespace Zongsoft.Expressions;

public partial class TemplateEvaluator
{
	#region 语法节点
	private sealed class Part
	{
		public string Text;
		public Reference Reference;
		public string Format;
	}

	private sealed class Reference
	{
		public string Text;
		public string Namespace;
		public string Name;
		public int Position;
		public int Length;
		public readonly List<Accessor> Accessors = new();
	}

	private sealed class Accessor
	{
		public string Name;
		public List<Argument> Arguments;
	}

	private sealed class Argument
	{
		public object Value;
		public Reference Reference;
	}

	private readonly record struct Lexeme(Token Token, int Position, int Length);
	#endregion

	#region 模板解析
	private sealed class Parser(string template, int depth)
	{
		private static readonly Lexer _lexer = CreateLexer();
		private readonly string _template = template;
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
						parts.Add(new Part { Text = literal.ToString() });
						literal.Clear();
					}

					parts.Add(this.ParsePlaceholder(ref position));
				}
				else
				{
					literal.Append(character);
					position++;
				}
			}

			if(literal.Length > 0)
				parts.Add(new Part { Text = literal.ToString() });

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

			return new Part { Reference = reference, Format = format };
		}

		private string ParseFormat(int opening, ref int cursor)
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

					return _template[start..end];
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
	#endregion

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

			var reference = new Reference { Position = start };

			if(this.Match(":"))
			{
				reference.Namespace = string.Join('.', names);
				reference.Name = this.ReadName();
			}
			else
			{
				reference.Name = names[0];

				for(int i = 1; i < names.Count; i++)
					reference.Accessors.Add(new Accessor { Name = names[i] });
			}

			while(_index < _tokens.Count)
			{
				if(this.Match("."))
					reference.Accessors.Add(new Accessor { Name = this.ReadName() });
				else if(this.Match("["))
				{
					var arguments = new List<Argument> { this.ParseArgument(nesting + 1) };

					while(this.Match(","))
						arguments.Add(this.ParseArgument(nesting + 1));

					if(!this.Match("]"))
						throw this.Failure("InvalidSyntax", this.CurrentPosition, 1);

					reference.Accessors.Add(new Accessor { Arguments = arguments });
				}
				else
					break;
			}

			var last = _tokens[_index - 1];
			reference.Length = last.Position + last.Length - start;
			reference.Text = _template.Substring(start, reference.Length);
			return reference;
		}

		private Argument ParseArgument(int nesting)
		{
			if(_index >= _tokens.Count)
				throw this.Failure("InvalidSyntax", this.CurrentPosition, 0);

			var token = _tokens[_index].Token;

			if(token.Type == TokenType.Constant)
			{
				//布尔/null 只有在整个裸参数中才是常量，其它名称位置仍是标识符。
				if(token.Value is not bool && token.Value != null || this.IsSymbol(_index + 1, ",") || this.IsSymbol(_index + 1, "]"))
				{
					_index++;
					return new Argument { Value = token.Value };
				}
			}

			return new Argument { Reference = this.ParseReference(nesting) };
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
			return _template.Substring(lexeme.Position, lexeme.Length);
		}

		private bool Match(string symbol)
		{
			if(!this.IsSymbol(_index, symbol))
				return false;

			_index++;
			return true;
		}

		private bool IsSymbol(int index, string symbol) =>
			index < _tokens.Count && _tokens[index].Token.Type == TokenType.Symbol && (string)_tokens[index].Token.Value == symbol;

		private int CurrentPosition => _index < _tokens.Count ? _tokens[_index].Position :
			_tokens.Count == 0 ? 0 : _tokens[^1].Position + _tokens[^1].Length;

		private TemplateEvaluationException Failure(string code, int position, int length, Exception innerException = null) =>
			new(code, TemplateEvaluationStage.Parsing, _template, null, position, length, _depth, innerException);

		private static Lexer CreateLexer()
		{
			var lexer = new Lexer();
			lexer.Tokenizers[1] = new NumberTokenizer(true);
			return lexer;
		}
		#endregion
	}
}
