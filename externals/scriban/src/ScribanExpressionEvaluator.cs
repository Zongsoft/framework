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
using System.Collections;
using System.Collections.Generic;

using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

using Zongsoft.Services;
using Zongsoft.Expressions;

namespace Zongsoft.Externals.Scriban;

[Service<IExpressionEvaluator>(NAME)]
public class ScribanExpressionEvaluator : ExpressionEvaluatorBase
{
	#region 常量定义
	internal const string NAME = "Scriban";
	#endregion

	#region 静态常量
	private static readonly ParserOptions _parserOptions = new()
	{
		LiquidFunctionsToScriban = true,
	};

	private static readonly LexerOptions _lexerOptions = new()
	{
		Lang = ScriptLang.Default,
		Mode = ScriptMode.ScriptOnly,
	};
	#endregion

	#region 构造函数
	public ScribanExpressionEvaluator() : base(NAME) => this.Global = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共方法
	public override object Evaluate(string expression, IExpressionEvaluatorOptions options, IDictionary<string, object> variables = null)
	{
		if(string.IsNullOrEmpty(expression))
			return null;

		var template = Template.Parse(expression, null, _parserOptions, _lexerOptions);
		var parameters = new ScriptObject(variables.Count, StringComparer.OrdinalIgnoreCase);

		foreach(var variable in this.Global)
		{
			parameters.SetValue(variable.Key, variable.Value, false);
		}

		if(variables != null)
		{
			foreach(var variable in variables)
				parameters.SetValue(variable.Key, variable.Value, false);
		}

		return template.Evaluate(new TemplateContext(parameters));
	}
	#endregion
}
