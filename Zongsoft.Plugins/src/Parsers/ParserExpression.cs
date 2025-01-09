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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

namespace Zongsoft.Plugins.Parsers
{
	public class ParserExpression
	{
		#region 私有枚举
		private enum ParserExpressionState
		{
			None,
			Scheme,
			Content,
		}
		#endregion

		#region 成员字段
		private string _scheme;
		private string _content;
		private ParserExpression _next;
		#endregion

		#region 构造函数
		internal ParserExpression(string scheme, string content, ParserExpression next = null)
		{
			if(string.IsNullOrWhiteSpace(scheme))
				throw new ArgumentNullException(nameof(scheme));

			_scheme = scheme.ToLowerInvariant().Trim();
			_content = content?.Trim();
			_next = next;
		}
		#endregion

		#region 公共属性
		public string Scheme => _scheme;
		public string Content
		{
			get => _content;
			set => _content = value;
		}
		public ParserExpression Next
		{
			get => _next;
			set => _next = value;
		}
		#endregion

		#region 静态方法
		public static ParserExpression Parse(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return null;

			ParserExpression result = null;
			ParserExpression current = null;

			using(var reader = new StringReader(text))
			{
				while(reader.Peek() > 0)
				{
					current = ParseCore(reader, message =>
					{
						throw new ParserException(message);
					});

					if(result == null)
						result = current;
					else //线性查找命令表达式的管道链，并更新其指向
					{
						var item = result;

						while(item.Next != null)
						{
							item = item.Next;
						}

						item.Next = current;
					}
				}
			}

			return result;
		}

		public static bool TryParse(string text, out ParserExpression result)
		{
			result = null;

			if(string.IsNullOrWhiteSpace(text))
				return false;

			using(var reader = new StringReader(text))
			{
				while(reader.Peek() > 0)
				{
					var isFailed = false;
					var current = ParseCore(reader, _ => isFailed = true);

					//如果解析失败则重置输出参数为空并返回假
					if(isFailed)
					{
						result = null;
						return false;
					}

					if(current == null)
						continue;

					if(result == null)
						result = current;
					else
					{
						var item = result;

						while(item.Next != null)
						{
							item = item.Next;
						}

						item.Next = current;
					}
				}
			}

			return true;
		}

		private static ParserExpression ParseCore(TextReader reader, Action<string> onFailed)
		{
			if(reader == null)
				throw new ArgumentNullException(nameof(reader));

			if(onFailed == null)
				throw new ArgumentNullException(nameof(onFailed));

			var isEscaping = false;
			var state = ParserExpressionState.None;
			var valueRead = 0;

			var scheme = string.Empty;
			var content = string.Empty;

			while((valueRead = reader.Read()) > 0)
			{
				var chr = (char)valueRead;

				switch(chr)
				{
					case '{':
						if(state == ParserExpressionState.None)
							state = ParserExpressionState.Scheme;
						else if(state == ParserExpressionState.Scheme)
						{
							onFailed("The scheme of parser contains a '{' illegal character.");
							return null;
						}
						break;
					case '}':
						switch(state)
						{
							case ParserExpressionState.None:
								onFailed("Invalid parser expression.");
								return null;
							case ParserExpressionState.Scheme:
							case ParserExpressionState.Content:
								if(string.IsNullOrWhiteSpace(scheme))
								{
									onFailed("Missing scheme of parser.");
									return null;
								}

								return new ParserExpression(scheme, content);
						}
						break;
					case ':':
						if(state == ParserExpressionState.Scheme)
						{
							if(string.IsNullOrWhiteSpace(scheme))
							{
								onFailed("Missing scheme of parser.");
								return null;
							}

							state = ParserExpressionState.Content;
						}
						break;
					case '|':
						switch(state)
						{
							case ParserExpressionState.None:
								return null;
							case ParserExpressionState.Scheme:
								{
									onFailed("The scheme of parser contains a '|' illegal character.");
									return null;
								}
						}

						break;
					case '\\':
						//设置转义状态
						isEscaping = state == ParserExpressionState.Content && (!isEscaping);

						if(state != ParserExpressionState.Content)
						{
							onFailed("The parser expression contains illegal character.");
							return null;
						}

						break;
					default:
						break;
				}

				//设置转义状态：即当前字符为转义符并且当前状态不为转义状态
				isEscaping = chr == '\\' && (!isEscaping);

				if(isEscaping)
					continue;

				switch(state)
				{
					case ParserExpressionState.Scheme:
						scheme += chr;
						break;
					case ParserExpressionState.Content:
						content += chr;
						break;
				}
			}

			return new ParserExpression(scheme, content);
		}
		#endregion
	}
}
