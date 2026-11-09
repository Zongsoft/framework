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

public class SQLiteInsertStatementBuilder : InsertStatementBuilder
{
	#region 构造函数
	public SQLiteInsertStatementBuilder() { }
	#endregion

	#region 重写方法
	/// <summary>创建具有当前数据库批量写入限制的新增语句。</summary>
	/// <param name="entity">指定的数据实体元数据。</param>
	/// <param name="schema">指定的数据模式成员。</param>
	/// <returns>返回创建的新增语句。</returns>
	protected override InsertStatement CreateStatement(Metadata.IDataEntity entity, SchemaMember schema) => new(entity, schema) { ParameterLimit = 32766 };
	#endregion
}
