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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

/// <summary>表示 TDengine 数据语句的参数绑定器。</summary>
/// <remarks>
///		<para>TDengine 的 DELETE 命令不支持以普通预备语句参数的方式绑定删除条件，因此
///		<see cref="TDengineExpressionVisitor"/> 会在生成 DELETE 命令文本时，将经过转义的参数值直接写入 SQL。</para>
///		<para>但是这些参数表达式仍然保留在语句的参数集中，通用命令创建过程也会据此创建对应的
///		<see cref="DbParameter"/> 对象。如果将这些已不再被 SQL 引用的参数传给 TDengine 驱动，驱动仍会尝试绑定它们，从而导致命令参数与 SQL 占位符不匹配。</para>
///		<para>参数不能在命令创建阶段提前清除，因为通用绑定过程仍需通过参数名称查找并完成绑定；因此必须等绑定完成后，再移除 DELETE 命令中已经内联的参数。</para>
/// </remarks>
public sealed class TDengineStatementBinder : StatementBinder
{
	public static readonly TDengineStatementBinder Instance = new();

	private TDengineStatementBinder() { }

	protected override ValueTask OnBoundAsync(IDataMutateContextBase context, IStatementBase statement, DbCommand command, CancellationToken cancellation)
	{
		//DELETE 的参数值已由 TDengineExpressionVisitor 内联到命令文本中，此处仅移除不再被 SQL 引用的参数对象。
		if(statement is DeleteStatement)
			command.Parameters.Clear();

		return ValueTask.CompletedTask;
	}
}
