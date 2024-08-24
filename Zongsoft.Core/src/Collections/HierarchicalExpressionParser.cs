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

namespace Zongsoft.Collections
{
	public static class HierarchicalExpressionParser
	{
		#region 常量定义
		private const string EXCEPTION_ILLEGAL_CHARACTER_MESSAGE = "The '{0}' character at the {1} is an illegal character.";
		#endregion

		#region 公共方法
		public static bool TryParse(string text, out HierarchicalExpression expression)
		{
			return (expression = Parse(text, 0, 0, null)) != null;
		}

		public static bool TryParse(string text, int start, int count, out HierarchicalExpression expression)
		{
			return (expression = Parse(text, start, count, null)) != null;
		}

		public static HierarchicalExpression Parse(string text)
		{
			return Parse(text, 0, 0, message => throw new InvalidOperationException(message));
		}

		public static HierarchicalExpression Parse(string text, int start, int count)
		{
			return Parse(text, start, count, message => throw new InvalidOperationException(message));
		}

		public static HierarchicalExpression Parse(string text, Action<string> onError)
		{
			return Parse(text, 0, 0, onError);
		}

		public static HierarchicalExpression Parse(string text, int start, int count, Action<string> onError)
		{
			if(string.IsNullOrEmpty(text))
				return null;

			if(start < 0 || start >= text.Length)
				throw new ArgumentOutOfRangeException(nameof(start));

			if(count < 1)
				count = text.Length - start;
			else if(count > text.Length - start)
				throw new ArgumentOutOfRangeException(nameof(count));

			//创建解析上下文对象
			var context = new StateContext(text.AsSpan(start, count));
			Reflection.Expressions.IMemberExpression accessor = null;
			IList<string> segments = null;

			//状态迁移驱动
			for(int i = start; i < start + count; i++)
			{
				if(context.State == State.Exit)
				{
					var index = context.Character == '@' ? i : i - 1;

					if((accessor = Reflection.Expressions.MemberExpressionParser.Parse(text, index, -1, onError)) == null)
						return null;
					else
						break;
				}

				if(!context.Read())
					continue;

				switch(context.State)
				{
					case State.None:
						if(!DoNone(ref context, i, onError))
							return null;

						break;
					case State.Slash:
						if(!DoSlash(ref context, i, onError))
							return null;

						break;
					case State.AnchorCurrent:
						if(!DoAnchorCurrent(ref context, i, onError))
							return null;

						break;
					case State.AnchorParent:
						if(!DoAnchorParent(ref context, i, onError))
							return null;

						break;
					case State.Segment:
						if(!DoSegement(ref context, i, segment =>
						{
							if(segments == null)
								segments = new List<string>();

							segments.Add(segment);
						}, onError))
							return null;

						break;
				}
			}

			if(context.State == State.Segment)
			{
				//获取最终的解析结果
				var segment = context.Accept(0);

				if(segment != null && segment.Length > 0)
				{
					if(segments == null)
						segments = new string[] { segment };
					else
						segments.Add(segment);
				}
			}

			return new HierarchicalExpression(context.Anchor, segments?.ToArray(), accessor);
		}
		#endregion

		#region 状态处理
		private static bool DoNone(ref StateContext context, int position, Action<string> error)
		{
			switch(context.Character)
			{
				case '\0':
					return false;
				case '.':
					context.State = State.AnchorCurrent;
					context.Anchor = IO.PathAnchor.Current;
					break;
				case '/':
				case '\\':
					context.State = State.Slash;
					context.Anchor = IO.PathAnchor.Root;
					break;
				case '@':
				case '[':
					context.State = State.Exit;
					break;
				default:
					if(Validate(context.Character))
						context.State = State.Segment;
					else
					{
						error(GetIllegalCharacterExceptionMessage(context.Character, position));
						return false;
					}

					break;
			}

			return true;
		}

		private static bool DoSlash(ref StateContext context, int position, Action<string> error)
		{
			if(context.Character == '@' || context.Character == '[')
			{
				context.State = State.Exit;
				return true;
			}

			if(Validate(context.Character))
			{
				context.State = State.Segment;
				return true;
			}

			error(GetIllegalCharacterExceptionMessage(context.Character, position));
			return false;
		}

		private static bool DoAnchorCurrent(ref StateContext context, int position, Action<string> error)
		{
			if(char.IsWhiteSpace(context.Character))
				return true;

			switch(context.Character)
			{
				case '.':
					if(context.HasWhitespaces)
					{
						error("The path anchors cannot contain whitespace characters.");
						return false;
					}

					context.State = State.AnchorParent;
					context.Anchor = IO.PathAnchor.Parent;
					return true;
				case '/':
				case '\\':
					context.State = State.Slash;
					context.Accept(1);
					return true;
				default:
					error(GetIllegalCharacterExceptionMessage(context.Character, position));
					return false;
			}
		}

		private static bool DoAnchorParent(ref StateContext context, int position, Action<string> error)
		{
			if(char.IsWhiteSpace(context.Character))
				return true;

			if(context.Character == '/' || context.Character == '\\')
			{
				context.State = State.Slash;
				context.Accept(1);
				return true;
			}

			error(GetIllegalCharacterExceptionMessage(context.Character, position));
			return false;
		}

		private static bool DoSegement(ref StateContext context, int position, Action<string> complete, Action<string> error)
		{
			switch(context.Character)
			{
				case '@':
				case '[':
					context.State = State.Exit;
					complete(context.Accept(1));
					return true;
				case '/':
				case '\\':
					context.State = State.Slash;
					complete(context.Accept(1));
					return true;
			}

			if(Validate(context.Character))
				return true;

			error(GetIllegalCharacterExceptionMessage(context.Character, position));
			return false;
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool Validate(char chr)
		{
			var iterator = HierarchicalNode.IllegalCharacters.AsSpan().GetEnumerator();

			while(iterator.MoveNext())
			{
				if(iterator.Current == chr)
					return false;
			}

			return true;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetIllegalCharacterExceptionMessage(char chr, int position)
		{
			return string.Format(EXCEPTION_ILLEGAL_CHARACTER_MESSAGE, chr, position);
		}
		#endregion

		#region 嵌套结构
		private enum State
		{
			None,
			Exit,
			Slash,
			Segment,
			AnchorCurrent,
			AnchorParent,
		}

		private ref struct StateContext
		{
			#region 私有变量
			private int _anchorPosition;
			private int _cursorPosition;
			private int _whitespaces;
			private readonly ReadOnlySpan<char> _data;
			#endregion

			#region 公共字段
			public State State;
			public char Character;
			public IO.PathAnchor Anchor;
			#endregion

			#region 构造函数
			public StateContext(ReadOnlySpan<char> data)
			{
				_data = data;
				_anchorPosition = -1;
				_cursorPosition = -1;
				_whitespaces = 0;
				this.State = State.None;
				this.Character = '\0';
				this.Anchor = IO.PathAnchor.None;
			}
			#endregion

			#region 公共属性
			public bool HasWhitespaces
			{
				get => _whitespaces > 0;
			}
			#endregion

			#region 公共方法
			public bool Read()
			{
				if(_cursorPosition >= _data.Length)
					return false;

				if(char.IsWhiteSpace(this.Character = _data[++_cursorPosition]))
				{
					if(_cursorPosition == _anchorPosition + 1)
					{
						_anchorPosition++;
						return false;
					}

					_whitespaces++;
				}
				else
				{
					if(!IsDelimiter(this.Character))
						_whitespaces = 0;
					else
					{
						if(_cursorPosition == _anchorPosition + 1)
							_anchorPosition = _cursorPosition;
					}
				}

				return true;
			}

			public string Accept(int backspaces)
			{
				if(_anchorPosition < _cursorPosition - _whitespaces - backspaces)
				{
					var start = _anchorPosition;
					_anchorPosition = _cursorPosition;

					return _data.Slice(start + 1, _cursorPosition - start - _whitespaces - backspaces).ToString();
				}

				return null;
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private static bool IsDelimiter(char chr)
			{
				return chr == '/' || chr == '\\' || chr == '@' || chr == '[';
			}
			#endregion
		}
		#endregion
	}
}
