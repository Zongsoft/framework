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

public class SQLiteUpdateStatementBuilder : UpdateStatementBuilder
{
	#region 构造函数
	public SQLiteUpdateStatementBuilder() { }
	#endregion

	#region 重写方法
	protected override ReturningKind GetLinkReturningKind() => ReturningKind.Newer;

	/// <summary>创建当前数据库的新增或更新语句构建器。</summary>
	/// <returns>返回当前数据库的新增或更新语句构建器。</returns>
	protected override UpsertStatementBuilder CreateUpsertStatementBuilder() => new SQLiteUpsertStatementBuilder();
	#endregion
}
