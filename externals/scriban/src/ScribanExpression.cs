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
using System.Collections.Generic;

using Scriban;
using Scriban.Runtime;

namespace Zongsoft.Externals.Scriban;

public class ScribanExpression(Template template) : Zongsoft.Expressions.IExpression
{
	private readonly Template _template = template ?? throw new ArgumentNullException(nameof(template));

	public object Evaluate(IDictionary<string, object> variables = null)
	{
		ScriptObject parameters = null;

		if(variables != null)
		{
			parameters = variables is Dictionary<string, object> dictionary ? new ScriptObject(variables.Count, dictionary.Comparer) : new ScriptObject(variables.Count);

			foreach(var variable in variables)
				parameters.SetValue(variable.Key, variable.Value, false);
		}

		return _template.Evaluate(new TemplateContext(parameters));
	}
}
