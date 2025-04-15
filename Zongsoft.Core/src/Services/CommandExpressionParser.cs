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
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Services;

public class CommandExpressionParser : ICommandExpressionParser
{
	#region 单例字段
	public static readonly CommandExpressionParser Instance = new();
	#endregion

	#region 私有枚举
	private enum CommandPathState
	{
		None,
		Dot,
		DoubleDot,
		Slash,
		Part,
	}

	private enum CommandPairState
	{
		None,
		Slash,
		Assign,
		Part,
	}
	#endregion

	#region 私有构造
	private CommandExpressionParser() { }
	#endregion

	#region 解析方法
	public CommandExpression Parse(string text)
	{
		if(string.IsNullOrWhiteSpace(text))
			return null;

		CommandExpression result = null;
		CommandExpression current = null;

		using(var reader = new StringReader(text))
		{
			while(reader.Peek() > 0)
			{
				current = Parse(reader);

				if(result == null)
					result = current;
				else //线性查找命令表达式的管道链，并更新其指向
				{
					var previous = result;

					while(previous.Next != null)
					{
						previous = previous.Next;
					}

					previous.Next = current;
				}
			}
		}

		return result;
	}

	private CommandExpression Parse(TextReader reader)
	{
		IO.PathAnchor anchor;
		string name, path;
		var arguments = new List<string>();
		var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		//解析命令表达式中的路径部分，如果表达式有误则该解析方法内部抛出异常
		ParsePath(reader, out anchor, out name, out path);

		KeyValuePair<string, string>? pair;

		//依次解析命令表达式中的选项和参数
		while((pair = ParsePair(reader)) != null)
		{
			if(pair != null)
			{
				if(string.IsNullOrEmpty(pair.Value.Key))
					arguments.Add(pair.Value.Value);
				else
					options.Add(pair.Value.Key, pair.Value.Value);
			}
		}

		//返回一个命令表达式
		return new CommandExpression(anchor, name, path, options, arguments.ToArray());
	}
	#endregion

	#region 私有方法
	private static void ParsePath(TextReader reader, out IO.PathAnchor anchor, out string name, out string path)
	{
		if(reader == null)
			throw new ArgumentNullException(nameof(reader));

		var state = CommandPathState.None;
		var parts = new List<string>();
		var valueRead = 0;

		//设置输出参数的默认值
		anchor = IO.PathAnchor.None;
		name = string.Empty;
		path = string.Empty;

		while((valueRead = reader.Read()) > 0)
		{
			var chr = (char)valueRead;

			//首先对位于路径中间的点号进行转换，以方便后续的处理
			if(chr == '.' && state == CommandPathState.Part)
				chr = '/';

			if(chr == '.')
			{
				switch(state)
				{
					case CommandPathState.None:
						state = CommandPathState.Dot;
						anchor = IO.PathAnchor.Current;
						break;
					case CommandPathState.Dot:
						state = CommandPathState.DoubleDot;
						anchor = IO.PathAnchor.Parent;
						break;
					default:
						throw new CommandExpressionException("Invalid anchor of command path.");
				}
			}
			else if(chr == '/')
			{
				if(state == CommandPathState.Slash)
					throw new CommandExpressionException("Duplicate '/' slash characters.");

				if(state == CommandPathState.None)
					anchor = IO.PathAnchor.Root;
				else if(state == CommandPathState.Part)
				{
					parts.Add(name);
					name = string.Empty;
				}

				state = CommandPathState.Slash;
			}
			else if(Char.IsLetterOrDigit(chr) || chr == '_')
			{
				if(state == CommandPathState.Dot || state == CommandPathState.DoubleDot)
					throw new CommandExpressionException("Missing '/' slash character between dot and letter or digit.");

				name += chr;
				state = CommandPathState.Part;
			}
			else if(Char.IsWhiteSpace(chr))
			{
				if(state == CommandPathState.None)
					continue;
				else
					break;
			}
			else
			{
				throw new CommandExpressionException($"Contains '{chr}' illegal character(s) in the command path.");
			}
		}

		//如果路径以斜杠符结尾，即为非法路径格式
		if(state == CommandPathState.Slash && ((parts != null && parts.Count > 0) || anchor != IO.PathAnchor.Root))
			throw new CommandExpressionException("The command path can not at the end of '/' character.");

		if(parts != null && parts.Count > 0)
		{
			path = string.Join(".", parts);
		}
		else if(string.IsNullOrEmpty(name))
		{
			switch(anchor)
			{
				case IO.PathAnchor.Root:
					name = "/";
					break;
				case IO.PathAnchor.Current:
					name = ".";
					break;
				case IO.PathAnchor.Parent:
					name = "..";
					break;
			}

			anchor = IO.PathAnchor.None;
		}
	}

	private static IEnumerable<KeyValuePair<string, string>?> ParsePairs(string text)
	{
		if(text == null)
			yield break;

		using(var reader = new StringReader(text))
		{
			do
			{
				yield return ParsePair(reader);
			} while(reader.Peek() > 0);
		}
	}

	private static KeyValuePair<string, string>? ParsePair(TextReader reader)
	{
		if(reader == null)
			throw new ArgumentNullException(nameof(reader));

		var quote = '\0';
		var isEscaping = false;
		var key = string.Empty;
		var value = string.Empty;
		var state = CommandPairState.None;
		int valueRead;

		while((valueRead = reader.Read()) > 0)
		{
			var chr = (char)valueRead;

			if(chr == '-' || chr == '/')
			{
				if(state == CommandPairState.Slash)
					throw new CommandExpressionException($"Duplicate '{chr}' option indicator of command expression.");

				if(state == CommandPairState.None && quote == '\0')
				{
					state = CommandPairState.Slash;
					continue;
				}
			}
			else if(chr == ':' || chr == '=')
			{
				if(state == CommandPairState.Part && !string.IsNullOrEmpty(key) && (quote == '\0' && !isEscaping))
				{
					state = CommandPairState.Assign;
					continue;
				}
			}
			else if(chr == '|')
			{
				if(quote == '\0')
				{
					if(string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value))
						return null;

					return new KeyValuePair<string, string>(key, value);
				}
			}
			else if(Char.IsWhiteSpace(chr))
			{
				if(state == CommandPairState.Slash)
					throw new CommandExpressionException("A white-space character at the back of the option indicator.");

				if(state == CommandPairState.None)
					continue;
				else if(quote == '\0')
					return new KeyValuePair<string, string>(key, value);
			}
			else if(IsQuote(chr) && !isEscaping)
			{
				if(quote != '\0')
				{
					quote = '\0';
					continue;
				}
				else if(state != CommandPairState.Part)
				{
					quote = chr;
					continue;
				}
			}

			//设置转义状态：即当前字符为转义符并且当前状态不为转义状态
			isEscaping = chr == '\\' && (!isEscaping);

			if(isEscaping)
				continue;

			switch(state)
			{
				case CommandPairState.Slash:
					key += chr;
					break;
				case CommandPairState.None:
				case CommandPairState.Assign:
					value += chr;
					break;
				default:
					if(string.IsNullOrEmpty(value))
						key += chr;
					else
						value += chr;
					break;
			}

			state = CommandPairState.Part;
		}

		if(string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value))
			return null;

		return new KeyValuePair<string, string>(key, value);
	}

	private static bool IsQuote(char chr) => (chr == '"' || chr == '\'');
	#endregion
}
