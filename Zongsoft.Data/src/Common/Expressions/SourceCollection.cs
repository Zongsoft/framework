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
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Common.Expressions;

public class SourceCollection : KeyedCollection<string, ISource>
{
	#region 构造函数
	public SourceCollection(params ISource[] items) : base(StringComparer.OrdinalIgnoreCase)
	{
		if(items != null && items.Length > 0)
		{
			foreach(var item in items)
				this.Add(item);
		}
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(ISource item)
	{
		if(item is JoinClause join)
			return join.Name;
		else
			return item.Alias ?? item.ToString();
	}
	#endregion
}
