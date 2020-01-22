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

namespace Zongsoft.Collections
{
	public static class HierarchicalExpressionParser
	{
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
			var anchor = IO.PathAnchor.None;
			Reflection.Expressions.IMemberExpression members = null;
			IList<string> segments = null;

			//状态迁移驱动
			for(int i = start; i < start + count; i++)
			{
				if(context.State == State.Exit)
				{
					if((members = Reflection.Expressions.MemberExpressionParser.Parse(text, i, -1, onError)) == null)
						return null;
					else
						break;
				}

				context.Accept();

				switch(context.State)
				{
					case State.None:
						if(!DoNone(ref context, onError))
							return null;

						break;
					case State.Slash:
						if(!DoSlash(ref context, onError))
							return null;

						if(anchor == IO.PathAnchor.None)
							anchor = IO.PathAnchor.Root;

						break;
					case State.Anchor_Current:
						if(!DoAnchorCurrent(ref context, onError))
							return null;

						anchor = IO.PathAnchor.Current;

						break;
					case State.Anchor_Parent:
						if(!DoAnchorParent(ref context, onError))
							return null;

						anchor = IO.PathAnchor.Parent;

						break;
					case State.Segment:
						if(!DoSegement(ref context, segment => { }, onError))
							return null;

						break;
				}
			}

			//获取最终的解析结果
			var segment = context.GetResult();

			if(segment != null && segment.Length > 0)
			{
				if(segments == null)
					segments = new string[] { segment };
				else
					segments.Add(segment);
			}

			return new HierarchicalExpression(anchor, segments.ToArray(), members);
		}
		#endregion

		#region 状态处理
		private static bool DoNone(ref StateContext context, Action<string> error)
		{
			switch(context.Character)
			{
				case '\0':
					return false;
				case '.':
					context.State = State.Anchor_Current;
					break;
				case '/':
				case '\\':
					context.State = State.Slash;
					break;
				case '@':
				case '[':
					context.State = State.Exit;
					break;
				default:
					if(context.IsWhitespace())
						return true;

					if(Validate(context.Character))
						context.State = State.Segment;
					else
					{
						error($"");
						return false;
					}

					break;
			}

			return true;
		}

		private static bool DoSlash(ref StateContext context, Action<string> error)
		{
			if(context.IsWhitespace())
				return true;

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

			error($"");
			return false;
		}

		private static bool DoAnchorCurrent(ref StateContext context, Action<string> error)
		{
			switch(context.Character)
			{
				case '.':
					context.State = State.Anchor_Parent;
					return true;
				case '/':
				case '\\':
					context.State = State.Slash;
					return true;
				default:
					error($"");
					return false;
			}
		}

		private static bool DoAnchorParent(ref StateContext context, Action<string> error)
		{
			if(context.Character == '/' || context.Character == '\\')
			{
				context.State = State.Slash;
				return true;
			}

			error($"");
			return false;
		}

		private static bool DoSegement(ref StateContext context, Action<string> complete, Action<string> error)
		{
			switch(context.Character)
			{
				case '@':
				case '[':
					context.State = State.Exit;
					complete(context.GetResult());
					return true;
				case '/':
				case '\\':
					context.State = State.Slash;
					complete(context.GetResult());
					return true;
			}

			if(Validate(context.Character))
				return true;

			error($"");
			return false;
		}
		#endregion

		private static bool Validate(char chr)
		{
			return !Array.Exists(HierarchicalNode.IllegalCharacters, c => chr == c);
		}

		#region 嵌套结构
		private enum State
		{
			None,
			Exit,
			Slash,
			Segment,
			Anchor_Current,
			Anchor_Parent,
		}

		private ref struct StateContext
		{
			#region 私有变量
			private int _last;
			private int _position;
			private int _whitespaces;
			private readonly ReadOnlySpan<char> _data;
			#endregion

			#region 公共字段
			public State State;
			public char Character;
			#endregion

			#region 构造函数
			public StateContext(ReadOnlySpan<char> data)
			{
				_data = data;
				_last = -1;
				_position = -1;
				_whitespaces = 0;
				this.State = State.None;
				this.Character = '\0';
			}
			#endregion

			#region 公共方法
			public char Accept()
			{
				if(_position < _data.Length)
				{
					if(char.IsWhiteSpace(this.Character = _data[++_position]))
						_whitespaces++;

					return this.Character;
				}

				return this.Character = '\0';
			}

			public bool IsWhitespace()
			{
				if(_position < 0)
					throw new InvalidOperationException();

				return char.IsWhiteSpace(Character);
			}

			public string GetResult()
			{
				if(_last < _position - _whitespaces)
				{
					var start = _last;
					_last = _position;

					return _data.Slice(start + 1, _position - start - _whitespaces + 1).ToString();
				}

				return null;
			}
			#endregion
		}
		#endregion
	}
}
