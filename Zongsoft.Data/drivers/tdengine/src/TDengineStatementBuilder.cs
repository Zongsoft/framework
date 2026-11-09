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

namespace Zongsoft.Data.TDengine;

public class TDengineStatementBuilder : StatementBuilderBase
{
	#region 单例字段
	public static readonly TDengineStatementBuilder Default = new();
	#endregion

	#region 私有构造
	private TDengineStatementBuilder() { }
	#endregion

	#region 重写方法
	protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder() => new TDengineSelectStatementBuilder();
	protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder() => new TDengineDeleteStatementBuilder();
	protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder() => new TDengineInsertStatementBuilder();
	protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder() => throw new NotSupportedException(Properties.Resources.Statement_UpdateUnsupported_Message);
	protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder() => new TDengineUpsertStatementBuilder();
	protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder() => new TDengineAggregateStatementBuilder();
	protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder() => new TDengineExistStatementBuilder();
	protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder() => new TDengineExecutionStatementBuilder();
	#endregion
}
