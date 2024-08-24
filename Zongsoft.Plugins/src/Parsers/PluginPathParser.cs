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
using System.Collections.Generic;

namespace Zongsoft.Plugins.Parsers
{
	public class PluginPathParser : Parser
	{
		#region 公共方法
		public override Type GetValueType(ParserContext context)
		{
			if(string.IsNullOrWhiteSpace(context.Text))
				return null;

			//处理特殊路径表达式，即获取插件文件路径或目录
			if(context.Text.StartsWith("~"))
				return typeof(string);

			var expression = Collections.HierarchicalExpression.Parse(PluginPath.PreparePathText(context.Text));
			var node = context.Node.Find(expression.Path);

			if(node != null && node.ValueType != null)
				return PluginUtility.GetMemberType(expression.Accessor, node.ValueType);

			return null;
		}

		public override object Parse(ParserContext context)
		{
			if(string.IsNullOrWhiteSpace(context.Text))
				return null;

			//处理特殊路径表达式，即获取插件文件路径或目录
			if(context.Text == "~")
				return context.Plugin.FilePath;
			else if(context.Text == "~/")
				return System.IO.Path.GetDirectoryName(context.Plugin.FilePath);

			var text = PluginPath.PreparePathText(context.Text, out var mode);

			if(string.IsNullOrWhiteSpace(text))
				throw new PluginException($"Missing argument of the path parser.");

			return context.Node.Resolve(text, mode, context.MemberType);
		}
		#endregion
	}
}
