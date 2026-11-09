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
