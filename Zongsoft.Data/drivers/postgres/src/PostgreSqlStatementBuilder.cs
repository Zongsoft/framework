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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.PostgreSql;

public class PostgreSqlStatementBuilder : StatementBuilderBase
{
	#region 单例字段
	public static readonly PostgreSqlStatementBuilder Default = new();
	#endregion

	#region 构造函数
	private PostgreSqlStatementBuilder() { }
	#endregion

	#region 重写方法
	protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder() => new PostgreSqlSelectStatementBuilder();
	protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder() => new PostgreSqlDeleteStatementBuilder();
	protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder() => new PostgreSqlInsertStatementBuilder();
	protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder() => new PostgreSqlUpdateStatementBuilder();
	protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder() => new PostgreSqlUpsertStatementBuilder();
	protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder() => new PostgreSqlAggregateStatementBuilder();
	protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder() => new PostgreSqlExistStatementBuilder();
	protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder() => new PostgreSqlExecutionStatementBuilder();
	#endregion
}
