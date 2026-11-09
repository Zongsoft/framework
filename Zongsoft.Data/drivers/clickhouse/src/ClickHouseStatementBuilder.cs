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

namespace Zongsoft.Data.ClickHouse;

public class ClickHouseStatementBuilder : StatementBuilderBase
{
	#region 单例字段
	public static readonly ClickHouseStatementBuilder Default = new();
	#endregion

	#region 构造函数
	private ClickHouseStatementBuilder() { }
	#endregion

	#region 重写方法
	protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder() => new ClickHouseSelectStatementBuilder();
	protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder() => new ClickHouseDeleteStatementBuilder();
	protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder() => new ClickHouseInsertStatementBuilder();
	protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder() => new ClickHouseUpdateStatementBuilder();
	protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder() => new ClickHouseUpsertStatementBuilder();
	protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder() => new ClickHouseAggregateStatementBuilder();
	protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder() => new ClickHouseExistStatementBuilder();
	protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder() => new ClickHouseExecutionStatementBuilder();
	#endregion
}
