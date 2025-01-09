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
using System.Text.RegularExpressions;

namespace Zongsoft.Plugins.Parsers
{
	public abstract class Parser : IParser
	{
		#region 静态成员
		private static readonly Regex _regex = new Regex(@"(?<prefix>[^\{]*)?{\s*(?<scheme>\w+)\s*:\s*(?<value>[^\}]+)\s*}(?<suffix>.*)?", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		#endregion

		#region 构造函数
		protected Parser() { }
		#endregion

		#region 静态方法
		public static bool CanParse(string text)
		{
			return !string.IsNullOrWhiteSpace(text) && _regex.IsMatch(text);
		}

		public static object Parse(string text, PluginTreeNode node, string memberName, Type memberType)
		{
			if(node == null)
				throw new ArgumentNullException(nameof(node));

			return ParseCore(text, node, GetParser, (scheme, expression, element) => new ParserContext(scheme, expression, element, memberName, memberType));
		}

		public static object Parse(string text, Builtin builtin, string memberName, Type memberType)
		{
			if(builtin == null)
				throw new ArgumentNullException(nameof(builtin));

			return ParseCore(text, builtin, (scheme, element) => GetParser(scheme, element.Node), (scheam, expression, element) => new ParserContext(scheam, expression, element, memberName, memberType));
		}

		public static Type GetValueType(string text, Builtin builtin)
		{
			if(string.IsNullOrWhiteSpace(text) || builtin == null)
				return null;

			//解析输入的文本
			ResolveText(text, out var scheme, out var value, out _, out _);

			if(!string.IsNullOrEmpty(scheme))
			{
				//根据当前构件获取一个对应的解析器
				var parser = GetParser(scheme, builtin.Node);

				if(parser != null)
					return parser.GetValueType(new ParserContext(scheme, value, builtin, null, null));
			}

			//返回空(失败)
			return null;
		}
		#endregion

		#region 私有方法
		private static IParser GetParser(string scheme, PluginTreeNode node)
		{
			if(node.Plugin != null)
				return node.Plugin.GetParser(scheme);

			foreach(var plugin in node.Tree.Plugins)
			{
				if(plugin.IsHidden)
					continue;

				//通过插件向上查找指定的解析器
				var parser = plugin.GetParser(scheme);

				if(parser != null)
					return parser;
			}

			return null;
		}

		private static object ParseCore<TElement>(string text, TElement element, Func<string, TElement, IParser> parserFactory, Func<string, string, TElement, ParserContext> createContext) where TElement : PluginElement
		{
			if(string.IsNullOrWhiteSpace(text))
				return text;

			//解析输入的文本
			ResolveText(text, out var scheme, out var value, out var prefix, out var suffix);

			if(string.IsNullOrWhiteSpace(scheme))
				return value;

			//根据当前构件获取一个对应的解析器
			var parser = parserFactory(scheme, element);

			if(parser == null)
				throw new PluginException($"The specified '{scheme}' parser was not found.");

			//创建解析器上下文对象
			var context = createContext(scheme, value, element);
			//调用解析器的解析方法，获取解析结果
			var result = parser.Parse(context);

			//如果表达式文本中无前缀和后缀则直接返回解析结果
			if(string.IsNullOrWhiteSpace(prefix) && string.IsNullOrWhiteSpace(suffix))
				return result;

			//注意：否则将对解析结果与前缀和后缀做文本连接并返回该文本
			return string.Format("{1}{0}{2}", result, prefix, suffix);
		}

		internal static void ResolveText(string text, out string scheme, out string value, out string prefix, out string suffix)
		{
			//设置输出参数的默认值
			scheme = null;
			value = text;
			prefix = null;
			suffix = null;

			if(string.IsNullOrWhiteSpace(text))
				return;

			Match match = _regex.Match(text);

			if(!match.Success)
				throw new PluginException(string.Format("Invalid format of parser, this expression is '{0}'.", text));

			//设置解析器模式为匹配成功的模式值
			scheme = match.Groups["scheme"].Value;
			//返回解析器原始文本中匹配成功的文本值
			value = match.Groups["value"].Value;

			prefix = match.Groups["prefix"].Value;
			suffix = match.Groups["suffix"].Value;
		}
		#endregion

		#region 获取类型
		public virtual Type GetValueType(ParserContext context) => this.Parse(context)?.GetType();
		#endregion

		#region 抽象方法
		/// <summary>解析目标对象。</summary>
		/// <returns>返回解析后的对象。</returns>
		public abstract object Parse(ParserContext context);
		#endregion
	}
}
