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
using System.Collections.Generic;

using Zongsoft.Collections;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

public class DeleteStatement : MutateStatement
{
	#region 构造函数
	public DeleteStatement(IDataEntity entity, string alias = "T") : base(entity, null, alias)
	{
		this.Tables = [this.Table];
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个表标识的集合，表示要删除的表。</summary>
	public IList<TableIdentifier> Tables { get; }
	#endregion
}
