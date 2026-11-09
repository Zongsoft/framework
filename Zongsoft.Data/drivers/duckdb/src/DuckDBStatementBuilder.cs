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

namespace Zongsoft.Data.DuckDB;

public class DuckDBStatementBuilder : StatementBuilderBase
{
	#region 单例字段
	public static readonly DuckDBStatementBuilder Default = new();
	#endregion

	#region 构造函数
	private DuckDBStatementBuilder() { }
	#endregion

	#region 重写方法
	protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder() => new DuckDBSelectStatementBuilder();
	protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder() => new DuckDBDeleteStatementBuilder();
	protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder() => new DuckDBInsertStatementBuilder();
	protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder() => new DuckDBUpdateStatementBuilder();
	protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder() => new DuckDBUpsertStatementBuilder();
	protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder() => new DuckDBAggregateStatementBuilder();
	protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder() => new DuckDBExistStatementBuilder();
	protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder() => new DuckDBExecutionStatementBuilder();
	#endregion
}
