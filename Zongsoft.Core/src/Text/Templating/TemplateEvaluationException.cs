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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Text.Templating;

/// <summary>包含模板求值错误的位置、阶段及底层异常。</summary>
public class TemplateEvaluationException : Exception
{
	internal TemplateEvaluationException(
		string code,
		TemplateEvaluationStage stage,
		string template,
		string expression,
		int position,
		int length,
		int depth,
		Exception innerException = null) : base(string.Format(Properties.Resources.Template_EvaluationFailed_Message, code, position, stage), innerException)
	{
		this.Code = code;
		this.Stage = stage;
		this.Template = template;
		this.Expression = expression;
		this.Position = position;
		this.Length = length;
		this.Depth = depth;
	}

	public string Code { get; }
	public TemplateEvaluationStage Stage { get; }
	public string Template { get; }
	public string Expression { get; }
	public int Position { get; }
	public int Length { get; }
	public int Depth { get; }

	public override string ToString() => $"[{this.Code}]{this.Message}";
}
