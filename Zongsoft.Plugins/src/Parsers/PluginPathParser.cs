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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Plugins.Parsers;

public class PluginPathParser : Parser
{
	#region 公共方法
	public override Type GetValueType(ParserContext context)
	{
		if(string.IsNullOrWhiteSpace(context.Text))
			return null;

		//处理特殊路径表达式，即获取插件文件路径或目录
		if(context.Text.StartsWith('~'))
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
			throw new PluginException(Properties.Resources.Parser_PathArgumentRequired_Message);

		return context.Node.Resolve(text, mode, context.MemberType);
	}
	#endregion
}
