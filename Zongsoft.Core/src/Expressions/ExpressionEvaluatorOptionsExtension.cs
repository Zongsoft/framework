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
using System.IO;

namespace Zongsoft.Expressions;

public static class ExpressionEvaluatorOptionsExtension
{
	public static IExpressionEvaluatorOptions In(this IExpressionEvaluatorOptions options, TextReader reader)
	{
		if(options != null)
			options.Input = reader;

		return options;
	}

	public static IExpressionEvaluatorOptions Out(this IExpressionEvaluatorOptions options, TextWriter writer)
	{
		if(options != null)
			options.Output = writer;

		return options;
	}

	public static IExpressionEvaluatorOptions Error(this IExpressionEvaluatorOptions options, TextWriter writer)
	{
		if(options != null)
			options.Error = writer;

		return options;
	}
}
