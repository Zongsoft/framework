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

namespace Zongsoft.Data.SQLite;

public class SQLiteStatementBuilder : StatementBuilderBase
{
	#region 单例字段
	public static readonly SQLiteStatementBuilder Default = new();
	#endregion

	#region 构造函数
	private SQLiteStatementBuilder() { }
	#endregion

	#region 重写方法
	protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder() => new SQLiteSelectStatementBuilder();
	protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder() => new SQLiteDeleteStatementBuilder();
	protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder() => new SQLiteInsertStatementBuilder();
	protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder() => new SQLiteUpdateStatementBuilder();
	protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder() => new SQLiteUpsertStatementBuilder();
	protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder() => new SQLiteAggregateStatementBuilder();
	protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder() => new SQLiteExistStatementBuilder();
	protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder() => new SQLiteExecutionStatementBuilder();
	#endregion
}
