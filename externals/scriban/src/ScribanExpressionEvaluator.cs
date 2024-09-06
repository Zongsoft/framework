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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Scriban library.
 *
 * The Zongsoft.Externals.Scriban is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Scriban is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Scriban library. If not, see <http://www.gnu.org/licenses/>.
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
public class ScribanExpressionEvaluator : IExpressionEvaluator, IMatchable, IMatchable<string>
{
	#region 常量定义
	private const string NAME = "Scriban";
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
	public ScribanExpressionEvaluator() => this.Global = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public IDictionary<string, object> Global { get; }
	#endregion

	#region 公共方法
	public object Evaluate(string expression, IDictionary<string, object> variables = null)
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

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is string name && this.Match(name);
	public bool Match(string name) => string.Equals(name, NAME, StringComparison.OrdinalIgnoreCase);
	#endregion
}
