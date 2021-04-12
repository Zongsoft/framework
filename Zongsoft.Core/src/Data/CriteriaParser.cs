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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public static class CriteriaParser
	{
		#region 公共方法
		public static KeyValuePair<string, string>[] Parse(string text, int start = 0, int count = -1) => Parse(text.AsSpan(), start, count);
		public static KeyValuePair<string, string>[] Parse(ReadOnlySpan<char> span, int start = 0, int count = -1)
		{
			var result = ParseCore(span, start, count);

			if(result.IsFailed(out var message))
				throw new DataArgumentException("$criteria", message);

			return result.Members;
		}

		public static bool TryParse(string text, out KeyValuePair<string, string>[] result) => TryParse(text.AsSpan(), 0, -1, out result);
		public static bool TryParse(ReadOnlySpan<char> span, out KeyValuePair<string, string>[] result)
		{
			return TryParse(span, 0, -1, out result);
		}

		public static bool TryParse(string text, int start, out KeyValuePair<string, string>[] result) => TryParse(text.AsSpan(), start, -1, out result);
		public static bool TryParse(ReadOnlySpan<char> span, int start, out KeyValuePair<string, string>[] result)
		{
			return TryParse(span, start, -1, out result);
		}

		public static bool TryParse(string text, int start, int count, out KeyValuePair<string, string>[] result) => TryParse(text.AsSpan(), start, count, out result);
		public static bool TryParse(ReadOnlySpan<char> span, int start, int count, out KeyValuePair<string, string>[] result)
		{
			var criteria = ParseCore(span, start, count);
			result = criteria.Members;
			return criteria.Succeed && criteria.HasValue;
		}

		private static CriteriaParserResult ParseCore(ReadOnlySpan<char> span, int start = 0, int count = -1)
		{
			if(span.IsEmpty)
				return default;

			if(count < 0)
				count = span.Length - start;
			else if(count > span.Length - start)
				throw new ArgumentOutOfRangeException(nameof(count));

			string key = null;
			string value = null;
			ReadOnlySpan<char> part;
			var context = new CriteriaParserContext(span, span.Length - start);
			var members = new List<KeyValuePair<string, string>>();

			for(int i = start; i < start + count; i++)
			{
				context.Move(i, span[i]);
				var origin = context.State;

				switch(context.State)
				{
					case State.Error:
						return new CriteriaParserResult(context.ErrorMessage);
					case State.None:
						DoNone(ref context);
						break;
					case State.Suspense:
						DoSuspense(ref context);
						break;
					case State.Key:
						DoKey(ref context, out part);
						key = part.IsEmpty ? null : part.ToString();
						break;
					case State.Value:
						DoValue(ref context, out part);
						value = part.IsEmpty ? null : part.ToString();
						break;
				}

				if(context.State != origin)
				{
					switch(origin)
					{
						case State.Key:
							members.Add(new KeyValuePair<string, string>(key, null));
							break;
						case State.Value:
							members[^1] = new KeyValuePair<string, string>(key, value);
							break;
					}
				}
			}

			switch(context.State)
			{
				case State.Key:
					part = context.GetBuffer();
					members.Add(new KeyValuePair<string, string>(part.ToString(), null));
					break;
				case State.Value:
					part = context.GetBuffer();
					members[^1] = new KeyValuePair<string, string>(key, part.ToString());
					break;
			}

			return members.Count == 0 ? default : new CriteriaParserResult(members);
		}
		#endregion

		#region 状态处理
		private static void DoNone(ref CriteriaParserContext context)
		{
			if(context.Character == '+' || context.IsWhitespace)
				return;

			if(char.IsLetter(context.Character) || context.Character == '_')
			{
				context.Reset(State.Key, out _);
				context.Accept();
			}
			else
			{
				context.Error($"An illegal character ‘{context.Character}’ is at the {context.Index + 1} character.");
			}
		}

		private static void DoSuspense(ref CriteriaParserContext context)
		{
			if(context.IsWhitespace)
				return;

			switch(context.Character)
			{
				case ':':
					context.Reset(State.Value, out _);
					break;
				case '+':
					context.Reset(State.None, out _);
					break;
				default:
					context.Error($"An illegal character ‘{context.Character}’ is at the {context.Index + 1} character.");
					break;
			}
		}

		private static void DoKey(ref CriteriaParserContext context, out ReadOnlySpan<char> key)
		{
			switch(context.Character)
			{
				case ':':
					context.Reset(State.Value, out key);
					break;
				case '+':
					context.Reset(State.None, out key);
					break;
				default:
					key = default;

					if(context.IsWhitespace)
						context.Reset(State.Suspense, out key);
					else if(char.IsLetterOrDigit(context.Character) || context.Character == '_' || context.Character == '.')
						context.Accept();
					else
						context.Error($"An illegal character ‘{context.Character}’ is at the {context.Index + 1} character.");
					break;
			}
		}

		private static void DoValue(ref CriteriaParserContext context, out ReadOnlySpan<char> value)
		{
			value = default;

			switch(context.Character)
			{
				case '+':
					if(context.HasFlags(Flags.Escaping))
						context.Accept();
					else
						context.Reset(State.None, out value);

					break;
				case '\\':
					if(context.HasFlags(Flags.Escaping))
						context.Accept();
					else
						context.Mask(Flags.Escaping);

					break;
				default:
					context.Accept();
					break;
			}
		}
		#endregion

		#region 嵌套结构
		private ref struct CriteriaParserResult
		{
			#region 私有字段
			private readonly string _message;
			#endregion

			#region 公共字段
			public readonly KeyValuePair<string, string>[] Members;
			#endregion

			#region 构造函数
			public CriteriaParserResult(string message)
			{
				_message = message;
				this.Members = null;
			}

			public CriteriaParserResult(ICollection<KeyValuePair<string, string>> members)
			{
				_message = null;
				this.Members = members.ToArray();
			}
			#endregion

			#region 公共属性
			public bool IsEmpty { get => this.Members == null || this.Members.Length == 0; }
			public bool HasValue { get => this.Members != null && this.Members.Length > 0; }
			public bool Failed { get => !string.IsNullOrEmpty(_message); }
			public bool Succeed { get => string.IsNullOrEmpty(_message); }
			#endregion

			#region 公共方法
			public bool IsFailed(out string message)
			{
				message = _message;
				return message != null && message.Length > 0;
			}
			#endregion
		}

		private ref struct CriteriaParserContext
		{
			#region 私有字段
			private State _state;
			private char _character;
			private int _index;
			private Flags _flags;
			private string _errorMessage;
			private readonly ReadOnlySpan<char> _text;
			private readonly Span<char> _buffer;
			private int _bufferIndex;
			private int _whitespaceStart;
			private int _whitespaceCount;
			#endregion

			#region 构造函数
			public CriteriaParserContext(ReadOnlySpan<char> text, int count)
			{
				_text = text;
				_state = State.None;
				_index = 0;
				_character = '\0';
				_flags = Flags.None;
				_errorMessage = null;
				_whitespaceStart = 0;
				_whitespaceCount = 0;
				_bufferIndex = 0;
				_buffer = new Span<char>(new char[count]);
			}
			#endregion

			#region 公共属性
			public State State { get => _state; }
			public int Index { get => _index; }
			public char Character { get => _character; }
			public string ErrorMessage { get => _errorMessage; }
			public bool IsWhitespace { get => char.IsWhiteSpace(_character); }
			#endregion

			#region 公共方法
			public void Move(int index, char character)
			{
				_index = index;
				_character = character;
			}

			public void Error(string message)
			{
				_state = State.Error;
				_errorMessage = message;
			}

			public ReadOnlySpan<char> GetBuffer()
			{
				return _bufferIndex == 0 ? ReadOnlySpan<char>.Empty : _buffer.Slice(0, _bufferIndex);
			}

			public void Reset(State state, out ReadOnlySpan<char> value)
			{
				value = _bufferIndex == 0 ? ReadOnlySpan<char>.Empty : _buffer.Slice(0, _bufferIndex);

				_flags = Flags.None;
				_bufferIndex = 0;
				_whitespaceStart = 0;
				_whitespaceCount = 0;
				_state = state;
			}

			public void Accept()
			{
				this.Accept(this.Character);
			}

			public void Accept(char character = '\0')
			{
				if(character == '\0')
					return;

				if(char.IsWhiteSpace(character))
				{
					if(_bufferIndex > 0)
					{
						if(_whitespaceStart <= 0)
							_whitespaceStart = _index;

						_whitespaceCount++;
					}

					return;
				}

				if(_whitespaceCount > 0)
				{
					for(int i = 0; i < _whitespaceCount; i++)
						_buffer[_bufferIndex++] = _text[_whitespaceStart + i];

					_whitespaceStart = 0;
					_whitespaceCount = 0;
				}

				if(this.HasFlags(Flags.Escaping))
				{
					character = Escape(character);
					this.Unmask(Flags.Escaping);
				}

				_buffer[_bufferIndex++] = character;
			}

			public bool HasFlags(Flags flags) => (_flags & flags) == flags;
			public void Mask(Flags flags) => _flags |= flags;
			public void Unmask(Flags flags) => _flags &= ~flags;
			#endregion

			#region 私有方法
			private static char Escape(char chr) => chr switch
			{
				'\\' => '\\',
				'\'' => '\'',
				'\"' => '\"',
				't' => '\t',
				'b' => '\b',
				's' => ' ',
				'n' => '\n',
				'r' => '\r',
				_ => chr,
			};
			#endregion
		}
		#endregion

		#region 枚举定义
		[Flags]
		private enum Flags
		{
			/// <summary>无标记</summary>
			None = 0,
			/// <summary>转义中</summary>
			Escaping = 2,
		}

		private enum State
		{
			None,
			Error,
			Key,
			Value,
			Suspense,
		}
		#endregion
	}
}
