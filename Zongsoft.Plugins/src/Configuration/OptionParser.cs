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

using Zongsoft.Services;
using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Configuration;

public class OptionParser : Parser
{
	#region 解析方法
	public override object Parse(ParserContext context)
	{
		if(string.IsNullOrWhiteSpace(context.Text))
			return null;

		var expression = Collections.HierarchicalExpression.Parse(context.Text);

		if(expression != null)
		{
			object target = ApplicationContext.Current.Configuration.GetOption(context.MemberType, expression.Path);

			if(target != null && expression.Accessor != null)
				return Reflection.Expressions.MemberExpressionEvaluator.Default.GetValue(expression.Accessor, target);
			else
				return target;
		}

		return null;
	}
	#endregion
}
