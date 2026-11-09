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
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Zongsoft.Data.Common.Expressions;

public static class StatementSlotter
{
	#region 静态字段
	private static readonly Regex _regex = new(@"\$\{(?<placeholder>[a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
	#endregion

	#region 公共方法
	public static void Evaluate(IDataAccessContext context, IStatementBase statement, DbCommand command)
	{
		var evaluator = context?.Source?.Driver?.Slotter;

		if(evaluator == null)
			return;

		command.CommandText = _regex.Replace(command.CommandText, match =>
			evaluator.Evaluate(context, statement, GetSlot(statement, match.Groups["placeholder"].Value)));
	}
	#endregion

	#region 私有方法
	private static StatementSlot GetSlot(IStatementBase statement, string placeholder) => statement.Slots.TryGetValue(placeholder, out var slot) ? slot : null;
	#endregion
}
